using CpuMetricTestService.Cpu;
using k8s;

namespace CpuMetricTestService
{
    public class ClusterMetrics
    {
        public DateTime Timestamp { get; set; }
        public double ClusterCpuUsage { get; set; }
        public Dictionary<string, double> PodCpuUsage { get; set; } = new Dictionary<string, double>();
    }

    public class ClusterMetricProvider
    {
        private readonly ILogger<ClusterMetricProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private ClusterMetrics? _clusterMetrics;

        public ClusterMetricProvider(ILogger<ClusterMetricProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public ClusterMetrics? GetClusterMetrics() => _clusterMetrics;

        public async Task UpdateClusterMetrics()
        {
            _logger.LogInformation("Updating cluster metrics");
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            var client = new Kubernetes(config);

            var pods = await client.CoreV1.ListNamespacedPodAsync("rapi-cpu-experiments");

            var result = new ClusterMetrics();

            using var httpClient = _httpClientFactory.CreateClient();

            foreach (var pod in pods)
            {
                try
                {
                    var cpuUsage =
                        await httpClient.GetFromJsonAsync<ResourceMonitoringCpuUsageResult?>(
                            $"http://{pod.Status.PodIP}:8080/.metrics/cpu");
                    if (cpuUsage != null)
                    {
                        result.PodCpuUsage.Add(pod.Metadata.Name, cpuUsage.CpuUsagePercentage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to retrieve CPU usage of {pod.Metadata.Name}");
                }
            }

            result.Timestamp = DateTime.UtcNow;
            result.ClusterCpuUsage = CalculateClusterCpuUsage(result.PodCpuUsage.Values.ToList());

            _clusterMetrics = result;
        }

        public double CalculateClusterCpuUsage(List<double> podCpuUsage)
        {
            return podCpuUsage.Average();
        }
    }
}
