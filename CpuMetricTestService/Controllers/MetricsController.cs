using CpuMetricTestService.Model;
using k8s;
using Microsoft.AspNetCore.Http.HttpResults;
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
            return Ok(metrics);
        }
    }
}
