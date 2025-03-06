
using System.Diagnostics;
using System.Text;
using CpuMetricTestService.Cpu;

namespace CpuMetricTestService.Middlewares
{
    public class CpuProxyMiddleware : IMiddleware
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CpuProxyMiddleware> _logger;
        private readonly IResourceMonitoringCpuUsageEvaluator _cpuUsageEvaluator;

        public CpuProxyMiddleware(IResourceMonitoringCpuUsageEvaluator cpuUsageEvaluator, HttpClient httpClient, ILogger<CpuProxyMiddleware> logger)
        {
            _cpuUsageEvaluator = cpuUsageEvaluator;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _logger.LogInformation("Invoking CpuProxyMiddleware");

            if (context.Request.Headers.TryGetValue("WasAlreadyCpuProxied", out var wasAlreadyCpuProxied) && wasAlreadyCpuProxied == "true")
            {
                _logger.LogInformation("Request was already CPU proxied. Will not be proxied again");
                await next(context);
                return;
            }
           
            var cpuUsage = (ResourceMonitoringCpuUsageResult?)await _cpuUsageEvaluator.EvaluateAsync();
            _logger.LogInformation($"CPU usage current pod {cpuUsage?.CpuUsagePercentage}");
            if (cpuUsage == null || cpuUsage.CpuUsagePercentage <= 5)
            {
                _logger.LogInformation($"CPU usage too low to proxy");
                await next(context);
                return;
            }

            _logger.LogWarning("CPU usage is above 5%");

            var watch = new Stopwatch();
            watch.Start();

            var currentPodIp = Environment.GetEnvironmentVariable("POD_IP");

            var podsResponse = await _httpClient.GetAsync($"http://{currentPodIp}:8080/pods");
            podsResponse.EnsureSuccessStatusCode();
            var pods = await podsResponse.Content.ReadFromJsonAsync<PodCpuMetricsResponse>();
            _logger.LogInformation($"Pods response: {podsResponse.Content}");
            _logger.LogInformation($"Current pod IP: {currentPodIp}");

            var podWithLowestCpu = pods?.CpuMetrics
                .OrderBy(p => p.CpuMetric.CpuUsagePercentage)
                .Where(p => p.PodIP != currentPodIp)
                .FirstOrDefault();

            if (podWithLowestCpu != null)
            {
                _logger.LogInformation($"Proxying request to pod with IP: {podWithLowestCpu.PodIP} at http://{podWithLowestCpu.PodIP}:8080{context.Request.Path}{context.Request.QueryString}");

                var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"http://{podWithLowestCpu.PodIP}:8080{context.Request.Path}{context.Request.QueryString}")
                {
                    Content = new StreamContent(context.Request.Body)
                };
                proxyRequest.Headers.TryAddWithoutValidation("WasAlreadyCpuProxied", "true");

                foreach (var header in context.Request.Headers)
                {
                    proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                var proxyResponse = await _httpClient.SendAsync(proxyRequest);

                context.Response.StatusCode = (int)proxyResponse.StatusCode;
                foreach (var header in proxyResponse.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                context.Response.Headers["x-proxied-to"] = podWithLowestCpu.PodName;
                context.Response.Headers["x-proxied-by"] = Environment.GetEnvironmentVariable("POD_NAME");
                

                var responseContent = await proxyResponse.Content.ReadAsByteArrayAsync();

                _logger.LogInformation($"Proxy response: {Encoding.UTF8.GetString(responseContent)}");

                await context.Response.Body.WriteAsync(responseContent, 0, responseContent.Length);

                watch.Stop();
                context.Response.Headers["x-proxy-duration"] = watch.ElapsedMilliseconds.ToString();

                return;
                
            }
            
            await next(context);
        }
    }

    public class CpuMetric
    {
        public double CpuUsagePercentage { get; set; }
    }

    public class PodCpuMetricsResponse
    {
        public List<PodCpuMetric> CpuMetrics { get; set; }
    }

    public class PodCpuMetric
    {
        public string PodName { get; set; }
        public string PodIP { get; set; }
        public CpuMetric CpuMetric { get; set; }
    }
}
