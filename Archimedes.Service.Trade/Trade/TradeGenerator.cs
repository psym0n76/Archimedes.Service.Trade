using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Message;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade.Http;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeGenerator : ITradeGenerator
    {
        private readonly IHttpTradeRepository _http;
        private readonly IProducer<TradeMessage> _producer;

        private readonly ICacheManager _cache;
        private const string TransactionCache = "transaction";
        private readonly object _locker = new object();

        public TradeGenerator(ITradeExecutor executor, IHttpTradeRepository http, IProducer<TradeMessage> producer, ICacheManager cache)
        {
            _http = http;
            _producer = producer;
            _cache = cache;
            executor.TradeMessageEventHandler += Executor_TradeMessageEventHandler;
        }

        public void Executor_TradeMessageEventHandler(object sender, TradeMessageHandlerEventArgs e)
        {
            lock (_locker)
            {
                AddTradeToTable(e.Transaction);
                AddTradeToCache(e.Transaction);
                PublishTradeToQueue(e.Transaction);
            }
        }

        private void PublishTradeToQueue(Transaction transaction)
        {
            _producer.PublishMessage(new TradeMessage(), "TradeRequest");
        }

        private async void AddTradeToCache(Transaction transaction)
        {
            var transactions = await _cache.GetAsync<List<Transaction>>(TransactionCache);

            transactions.Add(transaction);

            await _cache.SetAsync(TransactionCache, transactions);
        }

        public void AddTradeToTable(Transaction transaction)
        {
            var trades = transaction.ProfitTargets.Select(profitTarget => new TradeDto()
                {
                    Market = transaction.Market,
                    BuySell = transaction.BuySell,
                    EntryPrice = profitTarget.EntryPrice,
                    TargetPrice = profitTarget.TargetPrice,
                    ClosePrice = transaction.StopTargets.First().TargetPrice,
                    Strategy = transaction.RiskRewardProfile,
                    Success = false,
                    Timestamp = DateTime.Now
                })
                .ToList();

            _http.AddTrade(trades);
        }
    }
}