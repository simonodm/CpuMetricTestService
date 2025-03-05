using CpuMetricTestService.Middlewares;
using Microsoft.AspNetCore.Mvc;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    public class LoadTestController : Controller
    {
        [HttpGet]
        [Route("/api/loadtest")]
        [MiddlewareFilter(typeof(CpuProxyMiddlewareBuilder))]
        public IActionResult Get([FromQuery] int n = 10000)
        {
            return Ok(Fibonacci.Calculate(n));
        }
    }
}
