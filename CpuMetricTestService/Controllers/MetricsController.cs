using CpuMetricTestService.Cpu;
using CpuMetricTestService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    [Route("/.metrics")]
    public class MetricsController : Controller
    {
        private IServiceProvider _serviceProvider;

        public MetricsController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
