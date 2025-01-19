using TradingApp.API.Services;

namespace TradingApp.API.BackgroundServices
{
    public class TradingWorker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TradingWorker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tradeService = scope.ServiceProvider.GetRequiredService<TradeService>();
                        await tradeService.CheckOrders();
                    }
                    _logger.LogInformation("Orders checked successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError("An error occurred while checking orders: {0}", ex.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
