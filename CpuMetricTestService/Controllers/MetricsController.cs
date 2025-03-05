using CpuMetricTestService.Cpu;
using CpuMetricTestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    [Route("/.metrics")]
    public class MetricsController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ClusterMetricProvider _clusterMetricProvider;

        public MetricsController(IServiceProvider serviceProvider, ClusterMetricProvider clusterMetricProvider)
        {
            _serviceProvider = serviceProvider;
            _clusterMetricProvider = clusterMetricProvider;
        }

        [HttpGet("clusterHealth")]
        public IActionResult GetClusterHealth()
        {
            return Ok(_clusterMetricProvider.GetClusterMetrics());
        }

        [HttpGet("debug")]
        public async Task<IActionResult> GetDebug()
        {
            var evaluators = _serviceProvider.GetServices<ICpuUsageEvaluator>();

            var result = new Dictionary<string, object?>();

            foreach (var evaluator in evaluators)
            {
                try
                {
                    var value = await evaluator.EvaluateAsync();
                    result.Add(evaluator.GetType().Name, value);
                }
                catch (Exception ex)
                {
                    result.Add(evaluator.GetType().Name, ex.ToString());
                }
            }

            return Ok(new CpuUsageStatistics
            {
                Sources = result
            });
        }

        [HttpGet("cpu")]
        public async Task<IActionResult> GetCpu()
        {
            var evaluator = _serviceProvider.GetServices<ICpuUsageEvaluator>().FirstOrDefault(s => s.GetType() == typeof(ResourceMonitoringCpuUsageEvaluator));
            return Ok(await evaluator.EvaluateAsync());
        }
    }
}
