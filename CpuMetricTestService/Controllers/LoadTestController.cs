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
            return Ok(Fibonacci.Calculate(n));
        }
    }
}
