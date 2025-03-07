using CpuMetricTestService.Cpu;
using CpuMetricTestService.Middlewares;
using Prometheus;

namespace CpuMetricTestService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddResourceMonitoring();
            builder.Services.AddCpuEvaluators();
            builder.Services.AddHttpClient();

            builder.Services.AddTransient<CpuProxyMiddleware>();
            builder.Services.AddTransient<HeaderEnrichmentMiddleware>();

            builder.Services.AddSingleton<ClusterMetricProvider>();

            builder.Services.AddHostedService<ClusterMetricCollectorHostedService>();

            var app = builder.Build();

            app.UseMetricServer();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseHttpMetrics(o =>
            {
                o.AddCustomLabel("pod_name", _ => Environment.GetEnvironmentVariable("POD_NAME") ?? string.Empty);
            });

            app.UseAuthorization();

            app.MapControllers();

            app.UseMiddleware<HeaderEnrichmentMiddleware>();

            app.Run();
        }
    }
}
