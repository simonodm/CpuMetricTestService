using k8s;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PodDiscoveryController : ControllerBase
    {
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
;
                return Ok(new
                {
                    Pods = pods.Items.Select(a => JsonSerializer.Serialize(a, new JsonSerializerOptions { WriteIndented = true })),
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
