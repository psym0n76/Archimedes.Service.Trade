using System;
using System.Collections.Generic;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;
using Phema.Caching;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeExecutorPrice : ITradeExecutorPrice
    {
        private readonly ILogger<TradeExecutorPrice> _logger;
        private readonly IDistributedCache<List<PriceLevel>> _cache;
        private readonly object _locker = new object();
        private const string CacheName = "price-levels";
        private readonly IHttpRepositoryClient _http;

        public TradeExecutorPrice(ILogger<TradeExecutorPrice> logger, IDistributedCache<List<PriceLevel>> cache, IHttpRepositoryClient http)
        {
            _logger = logger;
            _cache = cache;
            _http = http;
        }

        public event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;


        public void ExecuteLocked(PriceDto price, decimal lastBidPrice, decimal lastAskPrice)
        {
            lock (_locker)
            {
                Execute(price, lastBidPrice, lastAskPrice);
            }
        }

        public async void Execute(PriceDto price, decimal lastBidPrice, decimal lastAskPrice)
        {
            var tradeLog = new TimedLogger($"UPDATE PRICE {price}");

            var cachePriceLevels = await _cache.GetAsync(CacheName);

            foreach (var priceLevel in cachePriceLevels)
            {
                switch (priceLevel.BuySell)
                {
                    case "BUY":

                    {
                        if (ValidateBuy(price, lastAskPrice, priceLevel))
                        {
                            RaiseTradeEvent(priceLevel, tradeLog);
                            await _cache.SetAsync(CacheName, cachePriceLevels);
                            await _http.UpdatePriceLevel(priceLevel);
                        }

                        break;
                    }
                    case "SELL":
                    {
                        if (ValidateSell(price, lastBidPrice, priceLevel))
                        {
                            RaiseTradeEvent(priceLevel, tradeLog);
                            await _cache.SetAsync(CacheName, cachePriceLevels);
                            await _http.UpdatePriceLevel(priceLevel);
                        }

                        break;
                    }
                }
            }

            tradeLog.EndLog();
            _logger.LogInformation(tradeLog.GetLog());
        }

        public static bool ValidateSell(PriceDto price, decimal lastBidPrice, PriceLevel priceLevel)
        {
            return price.Bid > priceLevel.BidPrice && lastBidPrice < priceLevel.BidPrice;
        }

        public static bool ValidateBuy(PriceDto price, decimal lastAskPrice, PriceLevel priceLevel)
        {
            return price.Ask < priceLevel.AskPrice && lastAskPrice > priceLevel.AskPrice;
        }

        private void RaiseTradeEvent(PriceLevel priceLevel, TimedLogger tradeLog)
        {
            priceLevel.LastLevelBrokenDate = DateTime.Now;
            priceLevel.LevelsBroken++;

            if (priceLevel.LevelBroken)
            {
                return;
            }

            var trade = new Transaction(5, 3, 1.32m, 3, "", 100, priceLevel.BuySell, null);

            priceLevel.BookedTrades++;
            tradeLog.UpdateLog($"PriceLevel {priceLevel.BuySell} {priceLevel}");

            TradeMessageEventHandler?.Invoke(this, new TradeMessageHandlerEventArgs() { });
        }
    }
}