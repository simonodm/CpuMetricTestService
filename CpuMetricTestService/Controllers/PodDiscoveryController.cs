using CpuMetricTestService.Middlewares;
using k8s;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PodDiscoveryController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        // midleware
        // check current cpu for pod
        // check request metadata - count
        // 

        public PodDiscoveryController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        [Route("/pods")]
        public async Task<IActionResult> GetPods()
        {
            try
            {
                var config = KubernetesClientConfiguration.BuildDefaultConfig();
                var client = new Kubernetes(config);

                // List all pods in the default namespace
                var pods = await client.CoreV1.ListNamespacedPodAsync("rapi-cpu-experiments");

                var currentPodName = Environment.GetEnvironmentVariable("POD_NAME");
                var hostName = Environment.GetEnvironmentVariable("HOST_NAME");

                if (pods == null || pods.Items.Count == 0)
                {
                    return NoContent();
                }

                var podCpuMetrics = new List<object>();
                foreach (var pod in pods.Items)
                {
                    var podIp = pod.Status.PodIP;
                    var cpuMetric = await _httpClient.GetFromJsonAsync<object>($"http://{podIp}:8080/.metrics/cpu");
                    podCpuMetrics.Add(new { PodName = pod.Metadata.Name, PodIP = podIp, CpuMetric = cpuMetric });
                }

                return Ok(new
                {
                    Pods = pods.Items.Select(a => JsonSerializer.Serialize(a, new JsonSerializerOptions { WriteIndented = true })),
                    IPs = pods.Items.Select(p => p.Status.PodIPs),
                    CpuMetrics = podCpuMetrics,
                    Info = $"There are {pods.Items.Count} pods available. This request hit pod {currentPodName} {hostName}. All available pods are: {string.Join(", ", pods.Items.Select(a => a.Metadata.Name))}"
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message + e.StackTrace);
            }
        }
    }
}
