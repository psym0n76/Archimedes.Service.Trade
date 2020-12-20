using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public BasicPriceLevelStrategy(IPriceLevelSubscriber priceLevelSubscriber,
            ILogger<BasicPriceLevelStrategy> logger, ICacheManager cache)
        {
            _priceLevelSubscriber = priceLevelSubscriber;
            _logger = logger;
            _cache = cache;
        }

        public async void Consume(List<PriceLevelDto> priceLevels, CancellationToken token)
        {
            _priceLevelSubscriber.PriceLevelMessageEventHandler += PriceLevelSubscriber_PriceLevelMessageEventHandler;

            await InitialCacheLoad(priceLevels);

            await _priceLevelSubscriber.Consume(token);
        }

        private async Task InitialCacheLoad(List<PriceLevelDto> priceLevels)
        {
            await _cache.SetAsync(PriceLevelCache, priceLevels);
            _logger.LogInformation($"Initial load to the PriceLevel Cache {priceLevels.Count} item(s)");
        }

        private void PriceLevelSubscriber_PriceLevelMessageEventHandler(object sender, PriceLevelMessageHandlerEventArgs e)
        {
            _logger.LogInformation("PriceLevel Update Received");
            UpdateCache(e.PriceLevel);
        }


        public async void UpdateCache(PriceLevelDto priceLevel)
        {
            var cachePriceLevels = await _cache.GetAsync<List<PriceLevelDto>>(PriceLevelCache);

            if (cachePriceLevels.Any(a => a.TimeStamp == priceLevel.TimeStamp)) return;

            cachePriceLevels.Add(priceLevel);
            await _cache.SetAsync(PriceLevelCache, cachePriceLevels);
            _logger.LogInformation("Added PriceLevel to Cache");
        }
    }
}