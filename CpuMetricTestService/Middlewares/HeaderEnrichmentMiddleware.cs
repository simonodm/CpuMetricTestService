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
            _logger.LogInformation("Invoking HeaderEnrichmentMiddleware");

            context.Response.OnStarting(() =>
            {
                context.Request.Headers.TryGetValue("WasAlreadyForwarded", out var wasProxied);

                context.Response.Headers.Append("x-pod-name", Environment.GetEnvironmentVariable("POD_NAME"));
                context.Response.Headers.Append("x-was-proxied", wasProxied.Count == 0 ? "false" : wasProxied);

                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
