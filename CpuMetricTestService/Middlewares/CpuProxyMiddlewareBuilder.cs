namespace CpuMetricTestService.Middlewares
{
    public class CpuProxyMiddlewareBuilder
    {
        public static void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<CpuProxyMiddleware>();
        }
    }
}
