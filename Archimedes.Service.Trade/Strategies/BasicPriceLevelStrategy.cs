using System;
using System.Collections.Generic;
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

        public void Consume(string market, string granularity, CancellationToken token)
        {
            _logId = _batchLog.Start();

            InitialLoad(market, granularity).ConfigureAwait(false);

            _logger.LogInformation(_batchLog.Print(_logId));

            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler;
            _priceLevelSubscriber.Consume(token);
        }

        private async Task InitialLoad(string market, string granularity)
        {
            var priceLevels = await _priceLevel.LoadAsync(market, granularity);

            if (!priceLevels.Any())
            {
                _batchLog.Update(_logId, $"WARNING Missing PriceLevel(s)");
            }
            else
            {
                _batchLog.Update(_logId,
                    $"PriceLevel Range: {priceLevels.Min(a => a.TimeStamp)} to {priceLevels.Max(a => a.TimeStamp)}");
                _batchLog.Update(_logId, $"{priceLevels.Count} PriceLevel(s) returned from Table");

                await _cache.SetAsync(PriceLevelCache, priceLevels);
                _batchLog.Update(_logId, $"{priceLevels.Count} PriceLevel(s) added to Cache");
            }
        }

        private void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender,
            PriceLevelMessageHandlerEventArgs e)
        {
            try
            {
                _logId = _batchLog.Start();
                UpdateCache(e.PriceLevels);
            }
            catch (Exception ex)
            {
                _logger.LogError(_batchLog.Print(_logId,
                    $"Error returned from PriceLevelSubscriber_PriceLevelMessageEventHandler", ex));
                return;
            }

            _logger.LogInformation(_batchLog.Print(_logId));
        }


        public async void UpdateCache(List<PriceLevelDto> priceLevels)
        {
            _batchLog.Update(_logId,
                $"PriceLevel Update {priceLevels[0].Strategy} {priceLevels[0].BidPrice} {priceLevels[0].BidPriceRange} {priceLevels[0].TimeStamp}");

            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            foreach (var priceLevel in priceLevels)
            {
                if (cachePriceLevels.Any(a => a.TimeStamp == priceLevel.TimeStamp))
                {
                    _batchLog.Update(_logId, $"PriceLevel exists {priceLevel.TimeStamp}");
                }
                else
                {
                    cachePriceLevels.Add(priceLevel);

                    _batchLog.Update(_logId, $"PriceLevel added to Cache {priceLevel.TimeStamp}");
                    await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
                }
            }
        }
    }
}