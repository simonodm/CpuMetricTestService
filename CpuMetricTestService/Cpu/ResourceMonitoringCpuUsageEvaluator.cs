using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace CpuMetricTestService.Cpu
{
    public class ResourceMonitoringCpuUsageEvaluator : IResourceMonitoringCpuUsageEvaluator
    {
        private IResourceMonitor _resourceMonitor;

        public ResourceMonitoringCpuUsageEvaluator(IResourceMonitor resourceMonitor)
        {
            _resourceMonitor = resourceMonitor;
        }

        public Task<object?> EvaluateAsync()
        {
            var utilization = _resourceMonitor.GetUtilization(TimeSpan.FromSeconds(5));
            return Task.FromResult<object?>(utilization.CpuUsedPercentage);
        }
    }
}
