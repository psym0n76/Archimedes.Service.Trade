using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeExecutor : ITradeExecutor
    {
        private readonly ILogger<TradeExecutor> _logger;

        private readonly object _locker = new object();
        private readonly ICacheManager _cache;
        private const string CacheName = "price-levels";
        private const string LastPriceCache = "price";
        private readonly IHttpPriceLevelRepository _priceLevel;

        private readonly ITradeProfileFactory _tradeProfileFactory;

        public TradeExecutor(ILogger<TradeExecutor> logger, IHttpPriceLevelRepository priceLevel, ITradeProfileFactory tradeProfileFactory, ICacheManager cache)
        {
            _logger = logger;
            _priceLevel = priceLevel;
            _tradeProfileFactory = tradeProfileFactory;
            _cache = cache;
        }

        public event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;


        public void ExecuteLocked(PriceDto price, string tradeProfile)
        {
            lock (_locker)
            {
                Execute(price, tradeProfile);
            }
        }

        public async void Execute(PriceDto price, string tradeProfile)
        {
            var lastPrice = await _cache.GetAsync<PriceDto>(LastPriceCache);

            if (lastPrice.Ask == 0 || lastPrice.Bid == 0)
            {
                await UpdateLastPriceCache(price);
                return;
            }

            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(CacheName);

            var tradeLog = new TimedLogger($"UPDATE PRICE {price}");

            foreach (var priceLevel in cachePriceLevels)
            {
                switch (priceLevel.BuySell)
                {
                    case "BUY":

                    {
                        if (ValidateBuy(price, lastPrice.Ask, priceLevel))
                        {
                            RaiseTradeEvent(priceLevel, tradeLog, tradeProfile, price);
                            await UpdatePriceLevelCache(cachePriceLevels);
                            await UpdatePriceLevelTable(priceLevel);
                        }

                        break;
                    }
                    case "SELL":
                    {
                        if (ValidateSell(price, lastPrice.Bid, priceLevel))
                        {
                            RaiseTradeEvent(priceLevel, tradeLog, tradeProfile, price);
                            await UpdatePriceLevelCache(cachePriceLevels);
                            await UpdatePriceLevelTable(priceLevel);
                        }

                        break;
                    }
                }
            }

            await UpdateLastPriceCache(price);

            tradeLog.EndLog();
            _logger.LogInformation(tradeLog.GetLog());

        }

        private async Task UpdatePriceLevelTable(PriceLevelDto priceLevel)
        {
            await _priceLevel.UpdatePriceLevel(priceLevel);
        }

        private async Task UpdatePriceLevelCache(List<PriceLevelDto> cachePriceLevels)
        {
            await _cache.SetAsync(CacheName, cachePriceLevels);
        }

        private async Task UpdateLastPriceCache(PriceDto price)
        {
            await _cache.SetAsync(LastPriceCache, new PriceDto() {Ask = price.Ask, Bid = price.Bid});
        }

        public static bool ValidateSell(PriceDto price, decimal lastBidPrice, PriceLevelDto priceLevel)
        {
            return price.Bid > priceLevel.BidPrice && lastBidPrice < priceLevel.BidPrice;
        }

        public static bool ValidateBuy(PriceDto price, decimal lastAskPrice, PriceLevelDto priceLevel)
        {
            return price.Ask < priceLevel.AskPrice && lastAskPrice > priceLevel.AskPrice;
        }

        private void RaiseTradeEvent(PriceLevelDto priceLevel, TimedLogger tradeLog, string tradeProfile, PriceDto price)
        {
            priceLevel.LastLevelBrokenDate = DateTime.Now;
            priceLevel.LevelsBroken++;

            if (priceLevel.LevelsBroken > 0)
            {
                return;
            }

            priceLevel.BookedTrades++;

            tradeLog.UpdateLog($"PriceLevel {priceLevel.BuySell} {priceLevel}");

            BuildTradeEvent(tradeProfile, price, priceLevel.BuySell);
        }

        private void BuildTradeEvent(string tradeProfile, PriceDto price, string buySell)
        {
            var transaction = _tradeProfileFactory.GetTradeGenerationService(tradeProfile).Generate(price, buySell);

            var eventArgs = new TradeMessageHandlerEventArgs() {Transaction = transaction};

            TradeMessageEventHandler?.Invoke(this, eventArgs);
        }
    }
}