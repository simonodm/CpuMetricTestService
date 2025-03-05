namespace CpuMetricTestService.Cpu
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCpuEvaluators(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ICpuUsageEvaluator, ResourceMonitoringCpuUsageEvaluator>();
            serviceCollection.AddSingleton<ICpuUsageEvaluator, K8sMetricsApiCpuUsageEvaluator>();
            serviceCollection.AddSingleton<IResourceMonitoringCpuUsageEvaluator, ResourceMonitoringCpuUsageEvaluator>();

            return serviceCollection;
        }
    }
}
