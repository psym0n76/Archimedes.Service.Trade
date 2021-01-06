﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Price
{
    public class BasicPriceLevelStrategy : IBasicPriceLevelStrategy
    {
        private readonly ILogger<BasicPriceLevelStrategy> _logger;

        private const string PriceLevelCache = "price-levels";
        private readonly ICacheManager _cache;
        private readonly IPriceLevelSubscriber _priceLevelSubscriber;
        private readonly IPriceLevelLoader _priceLevel;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public BasicPriceLevelStrategy(IPriceLevelSubscriber priceLevelSubscriber,
            ILogger<BasicPriceLevelStrategy> logger, ICacheManager cache, IPriceLevelLoader priceLevel)
        {
            _priceLevelSubscriber = priceLevelSubscriber;
            _logger = logger;
            _cache = cache;
            _priceLevel = priceLevel;
        }

        public async void Consume(string market, string granularity, CancellationToken token)
        {
            _logId = _batchLog.Start();

            await InitialLoad(market, granularity);

            _logger.LogInformation(_batchLog.Print(_logId));

            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler;
            await _priceLevelSubscriber.Consume(token);
        }

        private async Task InitialLoad(string market, string granularity)
        {
            var priceLevels = await _priceLevel.LoadAsync(market, granularity);
            _batchLog.Update(_logId, $"{priceLevels.Count} PriceLevel(s) returned from Table");

            await _cache.SetAsync(PriceLevelCache, priceLevels);
            _batchLog.Update(_logId, $"{priceLevels.Count} PriceLevel(s) added to Cache");

            _logger.LogInformation((_batchLog.Print(_logId)));
        }

        private void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender,
            PriceLevelMessageHandlerEventArgs e)
        {
            UpdateCache(e.PriceLevels);
        }


        public async void UpdateCache(List<PriceLevelDto> priceLevel)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"PriceLevel Update {priceLevel[0].Strategy} {priceLevel[0].BidPrice} {priceLevel[0].BidPriceRange} {priceLevel[0].TimeStamp}");

            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            foreach (var cachePriceLevel in cachePriceLevels)
            {
                if (cachePriceLevels.Any(a => a.TimeStamp == cachePriceLevel.TimeStamp))
                {
                    _batchLog.Update(_logId, $"PriceLevel exists {cachePriceLevel.TimeStamp}");
                }
                else
                {
                    cachePriceLevels.Add(cachePriceLevel);

                    _batchLog.Update(_logId, $"PriceLevel added to Cache {cachePriceLevel.TimeStamp}");
                    await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
                }
            }
        }
    }
}