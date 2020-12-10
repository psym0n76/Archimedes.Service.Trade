using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class StrategyRunnerService : BackgroundService
    {
        private readonly IStrategyRunner _strategyRunner;
        private readonly ILogger<StrategyRunnerService> _logger;

        public StrategyRunnerService(IStrategyRunner strategyRunner, ILogger<StrategyRunnerService> logger)
        {
            _strategyRunner = strategyRunner;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Running Strategy Runner");
                    _strategyRunner.Run("GBP/USD", "15Min", stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unknown error found in StrategyBackgroundService {e.Message} {e.StackTrace}");
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }
    }
}