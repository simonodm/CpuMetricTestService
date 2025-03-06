namespace CpuMetricTestService.Middlewares
{
    public class HeaderEnrichmentMiddleware : IMiddleware
    {
        private ILogger<HeaderEnrichmentMiddleware> _logger;

        public HeaderEnrichmentMiddleware(ILogger<HeaderEnrichmentMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd("x-pod-name", Environment.GetEnvironmentVariable("POD_NAME"));
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
