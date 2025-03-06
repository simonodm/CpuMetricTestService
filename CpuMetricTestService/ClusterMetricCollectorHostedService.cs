namespace CpuMetricTestService
{
    public class ClusterMetricCollectorHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<ClusterMetricCollectorHostedService> _logger;
        private readonly ClusterMetricProvider _clusterMetricProvider;
        private Timer? _timer = null;

        public ClusterMetricCollectorHostedService(ILogger<ClusterMetricCollectorHostedService> logger, ClusterMetricProvider clusterMetricProvider)
        {
            _logger = logger;
            _clusterMetricProvider = clusterMetricProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ClusterMetricCollectorHostedService)} is starting.");

            _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(ClusterMetricCollectorHostedService)} is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void Execute(object? _)
        {
            _clusterMetricProvider.UpdateClusterMetrics().Wait();
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
