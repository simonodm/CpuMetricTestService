using k8s;
using Microsoft.AspNetCore.Mvc;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    [Route("/.metrics")]
    public class MetricsController : Controller
    {
        [HttpGet("cpu")]
        public async Task<IActionResult> GetCpu()
        {
            var config = KubernetesClientConfiguration.InClusterConfig();
            var client = new Kubernetes(config);

            var metrics = await client.GetKubernetesPodsMetricsAsync();

            var podCpus = metrics.Items
                .Where(metric => metric.Metadata.Labels.ContainsKey("app"))
                .ToDictionary(metric => metric.Metadata.Name,
                    metric => metric.Containers.FirstOrDefault()?.Usage["cpu"]);

            return Ok(podCpus);
        }
    }
}
