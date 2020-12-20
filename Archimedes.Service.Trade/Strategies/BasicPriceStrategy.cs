﻿using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Price;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{

    public class BasicPriceStrategy : IBasicPriceStrategy
    {
        private readonly IPriceSubscriber _priceSubscriber;
        private readonly ILogger<BasicPriceStrategy> _logger;
        private readonly ITradeExecutor _tradeExecutor;
        private readonly ITradeValuation _tradeValuation;

        private readonly BatchLog _batchLog = new();
        private string _logId;

        private const string LastPriceCache = "price";
        private readonly ICacheManager _cache;

        private string _tradeProfile;

        public BasicPriceStrategy(IPriceSubscriber priceSubscriber, ILogger<BasicPriceStrategy> logger, ITradeExecutor tradeExecutor, ITradeValuation tradeValuation, ICacheManager cache)
        {
            _priceSubscriber = priceSubscriber;
            _logger = logger;
            _tradeExecutor = tradeExecutor;
            _tradeValuation = tradeValuation;
            _cache = cache;
        }

        public async void Consume(string tradeProfile, PriceDto price, CancellationToken token)
        {
            _priceSubscriber.PriceMessageEventHandler += PriceSubscriber_PriceMessageEventHandler;
            _tradeProfile = tradeProfile;

            await InitialCacheLoad(price);

            await _priceSubscriber.Consume(token);
        }

        private async Task InitialCacheLoad(PriceDto price)
        {
            await _cache.SetAsync(LastPriceCache, new PriceDto() {Bid = price.Bid, Ask = price.Ask});
            _logger.LogInformation($"Initial load to the Last Price Cache {price.Bid} : {price.Ask}");
        }

        private void PriceSubscriber_PriceMessageEventHandler(object sender, PriceMessageHandlerEventArgs e)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,"Price Update Received");

            _tradeExecutor.ExecuteLocked(e.Price, _tradeProfile);
            _batchLog.Update(_logId, $"Trade Executor {_tradeProfile} Price:{e.Price.TimeStamp}");

            _tradeValuation.UpdateTradeLocked(e.Price);
            _batchLog.Update(_logId, $"Trade Valuation Price:{e.Price.TimeStamp}");

            _logger.LogInformation(_batchLog.Print(_logId));
        }
    }
}