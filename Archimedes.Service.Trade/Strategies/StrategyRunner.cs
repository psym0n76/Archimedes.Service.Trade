using System;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Service.Price;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade
{
    public class StrategyRunner : IStrategyRunner
    {
        private readonly ILogger<StrategyRunner> _logger;
        private readonly IHttpPriceLevelRepository _priceLevel;
        private readonly IHttpCandleRepository _candle;
        private readonly IHttpPriceRepository _price;

        private readonly IBasicPriceLevelStrategy _basicPriceLevelStrategy;
        private readonly IBasicCandleStrategy _basicCandleStrategy;
        private readonly IBasicPriceStrategy _basicPriceStrategy;

        public StrategyRunner(IHttpPriceLevelRepository priceLevel,
            IHttpCandleRepository candle, IHttpPriceRepository price, ILogger<StrategyRunner> logger,
            IBasicPriceLevelStrategy basicPriceLevelStrategy, IBasicCandleStrategy basicCandleStrategy,
            IBasicPriceStrategy basicPriceStrategy)
        {
            _priceLevel = priceLevel;
            _candle = candle;
            _price = price;
            _logger = logger;
            _basicPriceLevelStrategy = basicPriceLevelStrategy;
            _basicCandleStrategy = basicCandleStrategy;
            _basicPriceStrategy = basicPriceStrategy;
        }

        public async void Run(string market, string granularity, CancellationToken token)
        {
            _logger.LogInformation("Starting Strategy");
            var priceLevels = await _priceLevel.GetPriceLevelCurrentAndPreviousDay(market, granularity);

            _logger.LogInformation($"Returned {priceLevels.Count} PriceLevels");

            var candlesDto = await _candle.GetCandlesByMarketByFromDate(market, DateTime.Today.AddDays(-1));
            _logger.LogInformation($"Returned {candlesDto.Count} Candles");

            var lastPrice = await _price.GetLastPriceByMarket(market);
            _logger.LogInformation($"Returned Bid: {lastPrice.Bid} Ask: {lastPrice.Ask} LastPrice");

             var task1 = Task.Run(() => { _basicPriceLevelStrategy.Consume(priceLevels, token); }, token);

             var task2 = Task.Run(() => { _basicCandleStrategy.Consume("TradeProfileRiskThreeTimesEqual", token); }, token);

             var task3 = Task.Run(() => { _basicPriceStrategy.Consume("TradeProfileRiskThreeTimesEqual", lastPrice, new CancellationToken()); }, token);

             await Task.WhenAny(task1, task2, task3);

            _logger.LogInformation("Finished");

        }
    }
}