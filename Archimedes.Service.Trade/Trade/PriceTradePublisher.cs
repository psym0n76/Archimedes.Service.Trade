using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Archimedes.Service.Trade.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class PriceTradePublisher : IPriceTradePublisher
    {
        private readonly BatchLog _batchLog = new();
        private string _logId;

        private readonly ILogger<PriceTradePublisher> _logger;
        private readonly IHttpPriceLevelRepository _priceLevel;
        private readonly IHubContext<PriceLevelHub> _priceLevelHub;
        private readonly ICacheManager _cache;
        private const string PriceLevelCache = "price-levels";
        private const string LastPriceCache = "price";

        public PriceTradePublisher(IHttpPriceLevelRepository priceLevel, IHubContext<PriceLevelHub> priceLevelHub, ICacheManager cache, ILogger<PriceTradePublisher> logger)
        {
            _priceLevel = priceLevel;
            _priceLevelHub = priceLevelHub;
            _cache = cache;
            _logger = logger;
        }

        public async Task UpdateSubscribers(PriceLevelDto priceLevel)
        {
            _logId = _batchLog.Start("UpdateSubscribers");

            try
            {
                RaiseTradeEvent(priceLevel);
                await UpdatePriceLevelTable(priceLevel);
                UpdatePriceLevelHub(priceLevel);
            }
            catch (Exception e)
            {
                _logger.LogError(_batchLog.Print(_logId),e);
            }
            
            _logger.LogInformation(_batchLog.Print(_logId));
        }

        public async Task UpdatePriceLevelTable(PriceLevelDto priceLevel)
        {
            _batchLog.Update(_logId,
                $"Update PriceLevel Table {priceLevel.Strategy} {priceLevel.TimeStamp}");
            await _priceLevel.UpdatePriceLevel(priceLevel);
        }

        public async Task UpdatePriceLevelCache(List<PriceLevelDto> cachePriceLevels)
        {
            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
        }

        public async Task UpdateLastPriceCache(PriceDto price)
        {
            await _cache.SetAsync(LastPriceCache,
                new PriceDto() { Ask = price.Ask, Bid = price.Bid, TimeStamp = price.TimeStamp });
        }

        public void UpdatePriceLevelHub(PriceLevelDto level)
        {
            _batchLog.Update(_logId, "Update PriceLevelHub");
            _priceLevelHub.Clients.All.SendAsync("Update", level);
        }

        public void RaiseTradeEvent(PriceLevelDto priceLevel)
        {
            priceLevel.LevelBrokenDate = DateTime.Now;
            priceLevel.LevelsBroken++;
            priceLevel.LevelBroken = true;

            _batchLog.Update(_logId,
                $"=================================================================================================================================");
            _batchLog.Update(_logId,
                $"PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED - PRICE LEVEL CROSSED");
            _batchLog.Update(_logId,
                $"=================================================================================================================================");
        }
    }
}