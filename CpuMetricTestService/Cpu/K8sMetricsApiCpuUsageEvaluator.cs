using k8s;

namespace CpuMetricTestService.Cpu
{
    public class K8sMetricsApiCpuUsageEvaluator : ICpuUsageEvaluator
    {
        public async Task<object?> EvaluateAsync()
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            var client = new Kubernetes(config);

            var metrics = await client.GetKubernetesPodsMetricsAsync();

            var currentPodName = Environment.GetEnvironmentVariable("POD_NAME");

            var cpuUsage = metrics.Items
                .FirstOrDefault(metric => metric.Metadata.Labels.ContainsKey("app")
                                          && metric.Metadata.Name == currentPodName)?
                .Containers
                .FirstOrDefault()?
                .Usage["cpu"];

            return cpuUsage?.CanonicalizeString();
        }
    }
}
