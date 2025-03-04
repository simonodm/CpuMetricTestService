using CpuMetricTestService.Cpu;
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

        [HttpGet("cpu")]
        public async Task<IActionResult> GetCpu()
        {
            var evaluators = _serviceProvider.GetServices<ICpuUsageEvaluator>();

            var result = new Dictionary<string, object?>();

            foreach (var evaluator in evaluators)
            {
                var value = await evaluator.EvaluateAsync();
                result.Add(evaluator.GetType().Name, value);
            }

            return Ok(result);
        }
    }
}
