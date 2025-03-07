using Prometheus;

namespace CpuMetricTestService
{
    public static class PrometheusMetrics
    {
        public static readonly Counter RequestsProxied = Metrics.CreateCounter("requests_proxied",
            "Number of requests proxied from this pod");
        public static readonly Counter ProxyRequestsReceived = Metrics.CreateCounter("proxy_requests_received",
            "Number of proxied requests received by this pod");
        public static readonly Counter RequestsRedirected =
            Metrics.CreateCounter("redirected_requests", "Number of requests redirected from this pod");
        public static readonly Gauge PodCpuUsagePercentage = Metrics.CreateGauge("pod_cpu_usage_percentage",
            "Current CPU usage percentage of the pod");
        public static readonly Gauge ClusterCpuUsagePercentage = Metrics.CreateGauge("cluster_cpu_usage_percentage",
            "Current CPU usage percentage of the cluster");
    }
}
