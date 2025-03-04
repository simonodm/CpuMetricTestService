using Microsoft.AspNetCore.Mvc;

namespace CpuMetricTestService.Controllers
{
    [ApiController]
    public class LoadTestController : Controller
    {
        [HttpGet]
        [Route("/api/loadtest")]
        public IActionResult Get([FromQuery] int n = 10000)
        {
            Parallel.ForEach(Enumerable.Range(0, 10), _ => Fibonacci.Calculate(n));
            return Ok();
        }
    }
}
