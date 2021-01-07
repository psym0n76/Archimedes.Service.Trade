using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Logger;
using Archimedes.Library.Message;
using Archimedes.Library.Message.Dto;
using Archimedes.Library.RabbitMq;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeGenerator : ITradeGenerator
    {
        private readonly IHttpTradeRepository _http;
        private readonly IProducer<TradeMessage> _producer;
        private readonly ILogger<TradeGenerator> _logger;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        private readonly ICacheManager _cache;
        private const string TransactionCache = "transaction";
        private readonly object _locker = new();

        public TradeGenerator(IEngulfingCandleStrategy executor, IHttpTradeRepository http,
            IProducer<TradeMessage> producer, ICacheManager cache, ILogger<TradeGenerator> logger)
        {
            _http = http;
            _producer = producer;
            _cache = cache;
            _logger = logger;
            executor.TradeMessageEventHandler += Executor_TradeMessageEventHandler;
        }

        public void Executor_TradeMessageEventHandler(object sender, TradeMessageHandlerEventArgs e)
        {
            lock (_locker)
            {
                try
                {
                    _logId = _batchLog.Start();
                    AddTradeToTable(e.Transaction);
                    AddTradeToCache(e.Transaction);
                    PublishTradeToQueue(e.Transaction);
                    _logger.LogInformation(_batchLog.Print(_logId));
                }
                catch (Exception a)
                {
                    _logger.LogError(_batchLog.Print(_logId,
                        $"Error returned from TradeGenerator Transaction: {e.Transaction.BuySell} : {e.Transaction.Market} {e.Transaction.EntryPrice}", a));
                }
            }
        }

        private void PublishTradeToQueue(Transaction transaction)
        {
            _producer.PublishMessage(new TradeMessage(), "TradeRequest");
            _batchLog.Update(_logId,
                $"Published to TradeRequest Queue {transaction.BuySell} : {transaction.Market} {transaction.EntryPrice}");
        }

        private async void AddTradeToCache(Transaction transaction)
        {
            var transactions = await _cache.GetAsync<List<Transaction>>(TransactionCache);

            transactions.Add(transaction);

            await _cache.SetAsync(TransactionCache, transactions);
            _batchLog.Update(_logId,
                $"Added to Transaction Cache {transaction.BuySell} : {transaction.Market} {transaction.EntryPrice}");
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

            _http.AddTrades(trades);
        }
    }
}