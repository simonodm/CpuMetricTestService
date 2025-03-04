namespace CpuMetricTestService.Model;

public record CpuMetricResponse(decimal CpuFromMetricsService, decimal CpuFromPerformanceCounter);
