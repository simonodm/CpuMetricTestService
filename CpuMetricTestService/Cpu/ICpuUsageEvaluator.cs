namespace CpuMetricTestService.Cpu
{
    public interface ICpuUsageEvaluator
    {
        public Task<object?> EvaluateAsync();
    }
}
