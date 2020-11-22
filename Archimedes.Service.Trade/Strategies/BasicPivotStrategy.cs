using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;
using Archimedes.Service.Trade.Strategies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Phema.Caching;

namespace Archimedes.Service.Price
{
    public class BasicPivotStrategy : IBasicPivotStrategy
    {
        private decimal _lastBidPrice = 0m;
        private decimal _lastAskPrice = 0m;

        private readonly ILogger<BasicPivotStrategy> _logger;

        private readonly IPriceSubscriber _priceSubscriber;
        private readonly IPriceLevelSubscriber _priceLevelSubscriber;
        private readonly ICandleSubscriber _candleSubscriber;

        private readonly List<PriceLevel> _priceLevels = new List<PriceLevel>();
        private readonly List<CandleDto> _candles = new List<CandleDto>();
        private readonly List<Transaction> _transactions = new List<Transaction>();

        private readonly ITradeExecutorPrice _tradeExecutorPrice;

        private readonly IDistributedCache<List<PriceLevel>> _cache;
        private const string CacheName = "price-levels";

        public BasicPivotStrategy(ILogger<BasicPivotStrategy> log,
            IPriceSubscriber priceSubscriber, ICandleSubscriber candleSubscriber,
            IPriceLevelSubscriber priceLevelSubscriber, ITradeExecutorPrice tradeExecutorPrice, IDistributedCache<List<PriceLevel>> cache)
        {
            _logger = log;

            _priceSubscriber = priceSubscriber;
            _candleSubscriber = candleSubscriber;
            _priceLevelSubscriber = priceLevelSubscriber;
            _tradeExecutorPrice = tradeExecutorPrice;
            _cache = cache;

            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler;
            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler_Cache;

            _candleSubscriber.CandleMessageEventHandler += CandleSubscriber_CandleMessageEventHandler;
            _priceSubscriber.PriceMessageEventHandler += PriceSubscriber_PriceMessageEventHandler;
        }

        private void PriceLevelSubscriber_PriceLevelMessageEventHandler_Cache(object sender, MessageHandlerEventArgs e)
        {
            var priceLevel = JsonConvert.DeserializeObject<List<PriceLevel>>(e.Message);
            UpdatePriceLevelCache(priceLevel);
        }

        public void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var priceLevel = JsonConvert.DeserializeObject<List<PriceLevel>>(e.Message);
            UpdatePriceLevel(priceLevel);
        }

        public void CandleSubscriber_CandleMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var candleDto = JsonConvert.DeserializeObject<List<CandleDto>>(e.Message);
            UpdateCandles(candleDto);
        }

        public void PriceSubscriber_PriceMessageEventHandler(object sender, MessageHandlerEventArgs e)
        {
            var price = JsonConvert.DeserializeObject<PriceDto>(e.Message);

            _tradeExecutorPrice.Execute(price, _lastBidPrice, _lastAskPrice);

            if (price.Ask > 0 )
            {
                _lastAskPrice = price.Ask;
            }

            if (price.Bid > 0)
            {
                _lastBidPrice = price.Bid;
            }

            UpdateTrade(price);
        }

        public async void Consume(List<PriceLevel> priceLevels, List<CandleDto> candles)
        {
            await _cache.SetAsync(CacheName, priceLevels);
            _priceLevels.AddRange(priceLevels);
            _candles.AddRange(candles);
        }

        public void UpdatePriceLevel(List<PriceLevel> priceLevel)
        {
            foreach (var level in priceLevel.Where(level =>
                !_priceLevels.Select(a => a.TimeStamp).Contains(level.TimeStamp)))
            {
                _priceLevels.Add(level);
            }
        }

        public async void UpdatePriceLevelCache(List<PriceLevel> priceLevel)
        {
            var cachePriceLevels = await _cache.GetAsync(CacheName);

            foreach (var level in priceLevel.Where(level =>
                !cachePriceLevels.Select(a => a.TimeStamp).Contains(level.TimeStamp)))
            {
                cachePriceLevels.Add(level);
            }

            await _cache.SetAsync(CacheName, cachePriceLevels);
        }

        public void UpdateCandles(List<CandleDto> candleDto)
        {
            foreach (var level in candleDto)
            {
                if (!_candles.Select(a => a.TimeStamp).Contains(level.TimeStamp))
                {
                    _candles.Add(level);
                }
            }
        }

        public void UpdateTrade(PriceDto price)
        {
            foreach (var target in _transactions.SelectMany(transaction => transaction.ProfitTargets))
            {
                target.UpdateTrade(price);
            }

            foreach (var target in _transactions.SelectMany(transaction => transaction.StopTargets))
            {
                target.UpdateTrade(price);
            }
        }
    }
}