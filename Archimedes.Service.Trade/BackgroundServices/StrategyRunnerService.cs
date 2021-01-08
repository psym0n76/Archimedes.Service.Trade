using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class StrategyRunnerService : BackgroundService
    {
        private readonly ICandleLoader _candle;
        private readonly IStrategyRunner _strategyRunner;
        private readonly IBasicPriceStrategyHistoryUpdater _basicPriceStrategyHistoryUpdater;
        private readonly ILogger<StrategyRunnerService> _logger;
        private readonly BatchLog _batchLog = new();
        private string _logId;
        private const string Market = "GBP/USD";
        private const string Granularity = "15Min";
        private const string EngulfGranularity = "5Min";

        public StrategyRunnerService(IStrategyRunner strategyRunner, ILogger<StrategyRunnerService> logger, IBasicPriceStrategyHistoryUpdater basicPriceStrategyHistoryUpdater, ICandleLoader candle)
        {
            _strategyRunner = strategyRunner;
            _logger = logger;
            _basicPriceStrategyHistoryUpdater = basicPriceStrategyHistoryUpdater;
            _candle = candle;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logId = _batchLog.Start();

            Task.Run(() =>
            {
                try
                {
                    _batchLog.Update(_logId, $"Validating Candles {Market} {Granularity}");
                    _candle.ValidateRecentCandle(Market, Granularity,10);
                    
                    _batchLog.Update(_logId, $"Running StrategyHistory {Market} {Granularity}");
                    _basicPriceStrategyHistoryUpdater.UpdateHistory(Market, Granularity);

                    _batchLog.Update(_logId, $"Running Strategy {Market} {Granularity}");
                    _strategyRunner.Run(Market, Granularity, EngulfGranularity, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(_batchLog.Print(_logId, $"Unknown error found in StrategyRunnerService",e));
                }
                
            }, stoppingToken);

            _logger.LogInformation(_batchLog.Print(_logId, "Running still..."));

            return Task.CompletedTask;
        }
    }
}