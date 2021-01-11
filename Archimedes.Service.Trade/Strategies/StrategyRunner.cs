using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class StrategyRunner : IStrategyRunner
    {
        private const string TradeProfile = "TradeProfileRiskThreeTimesEqual";
        private readonly ILogger<StrategyRunner> _logger;
        private readonly IBasicPriceLevelStrategy _priceLevelStrategy;
        private readonly IEngulfingCandleStrategy _candleStrategy;
        private readonly IBasicPriceStrategy _priceStrategy;
        private readonly BatchLog _batchLog = new();
        private string _logId;


        public StrategyRunner(ILogger<StrategyRunner> logger,
            IBasicPriceLevelStrategy priceLevelStrategy, IEngulfingCandleStrategy candleStrategy,
            IBasicPriceStrategy priceStrategy)
        {
            _logger = logger;
            _priceLevelStrategy = priceLevelStrategy;
            _candleStrategy = candleStrategy;
            _priceStrategy = priceStrategy;
        }

        public async Task Run(string market, string granularity, string engulfGranularity, CancellationToken token)
        {
            _logId = _batchLog.Start();

            var tasks = new List<Task>
            {
                Task.Run(() => { _priceLevelStrategy.Consume(market, granularity, token); }, token),
                Task.Run(() => { _candleStrategy.Consume(market, engulfGranularity, TradeProfile, token); }, token),
                Task.Run(() => { _priceStrategy.Consume(TradeProfile, market, token); }, token)
            };

            var complete = Task.WhenAny(tasks.ToArray());

            try
            {
                await complete;
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId, $"Error returned from TaskRun", e));
                return;
            }

            _logger.LogInformation(_batchLog.Print(_logId));
        }
    }
}