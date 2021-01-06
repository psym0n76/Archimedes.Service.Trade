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
        private readonly IPriceTradeExecutor _tradeExecutor;
        private readonly ITradeValuation _tradeValuation;
        private readonly ILastPriceLoader _price;

        private readonly BatchLog _batchLog = new();
        private string _logId;

        private const string LastPriceCache = "price";
        private const decimal ToleranceOnePip = 0.001m;
        private readonly ICacheManager _cache;

        private string _tradeProfile;

        public BasicPriceStrategy(IPriceSubscriber priceSubscriber, ILogger<BasicPriceStrategy> logger,
            IPriceTradeExecutor tradeExecutor, ITradeValuation tradeValuation, ICacheManager cache,
            ILastPriceLoader price)
        {
            _priceSubscriber = priceSubscriber;
            _logger = logger;
            _tradeExecutor = tradeExecutor;
            _tradeValuation = tradeValuation;
            _cache = cache;
            _price = price;
        }

        public async void Consume(string tradeProfile, string market, CancellationToken token)
        {
            _logId = _batchLog.Start();
            _tradeProfile = tradeProfile;

            await InitialLoad(market);

            _logger.LogInformation(_batchLog.Print(_logId));

            _priceSubscriber.PriceMessageEventHandler += PriceSubscriber_PriceMessageEventHandler;
            await _priceSubscriber.Consume(token);
        }

        private async Task InitialLoad(string market)
        {
            var price = await _price.LoadAsync(market);
            _batchLog.Update(_logId,
                $"LastPrice Bid: {price.Bid} Ask: {price.Ask} {price.TimeStamp} returned from Table ");

            await _cache.SetAsync(LastPriceCache, new PriceDto() {Bid = price.Bid, Ask = price.Ask});
            _batchLog.Update(_logId, $"LastPrice Bid: {price.Bid} Ask: {price.Ask} {price.TimeStamp} added to Cache");
        }


        private void PriceSubscriber_PriceMessageEventHandler(object sender, PriceMessageHandlerEventArgs e)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,
                $"Price Update Bid: {e.Prices[0].Bid} Ask: {e.Prices[0].Ask} {e.Prices[0].TimeStamp}");

            _tradeExecutor.ExecuteLocked(e.Prices[0], ToleranceOnePip);
            _batchLog.Update(_logId, $"Trade Executor [{_tradeProfile}]");

            _tradeValuation.UpdateTradeLocked(e.Prices[0]);
            _batchLog.Update(_logId, $"Trade Valuation");

            _logger.LogInformation(_batchLog.Print(_logId));
        }
    }
}