namespace CpuMetricTestService.Models
{
    public class CpuUsageStatistics
    {
        public string PodName { get; set; } = Environment.GetEnvironmentVariable("POD_NAME") ?? string.Empty;
        public Dictionary<string, object?> Sources { get; set; } = new();
    }
}
