using Microsoft.Extensions.Diagnostics.ResourceMonitoring;

namespace CpuMetricTestService.Cpu
{
    public class ResourceMonitoringCpuUsageResult
    {
        public double CpuUsagePercentage { get; set; }
        public double GuaranteedCpuUnits { get; set; }
    }

    public class ResourceMonitoringCpuUsageEvaluator : ICpuUsageEvaluator
    {
        private IResourceMonitor _resourceMonitor;

        public ResourceMonitoringCpuUsageEvaluator(IResourceMonitor resourceMonitor)
        {
            _resourceMonitor = resourceMonitor;
        }

        public Task<object?> EvaluateAsync()
        {
            var utilization = _resourceMonitor.GetUtilization(TimeSpan.FromSeconds(5));
            return Task.FromResult<object?>(new ResourceMonitoringCpuUsageResult
            {
                CpuUsagePercentage = utilization.CpuUsedPercentage,
                GuaranteedCpuUnits = utilization.SystemResources.GuaranteedCpuUnits
            });
        }
    }
}
