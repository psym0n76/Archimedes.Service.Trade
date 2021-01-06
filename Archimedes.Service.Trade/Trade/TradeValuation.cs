using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeValuation : ITradeValuation
    {
        private const string TransactionCache = "transaction";
        private readonly object _locker = new object();
        private readonly ICacheManager _cache;
        private readonly BatchLog _batchLog = new BatchLog();
        private string _logId;
        private readonly ILogger<TradeValuation> _logger;

        public TradeValuation(ICacheManager cache, ILogger<TradeValuation> logger)
        {
            _cache = cache;
            _logger = logger;
        }


        public void UpdateTradeLocked(PriceDto price)
        {
            lock (_locker)
            {
                try
                {

                    var id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    _logId = _batchLog.Start();
                    UpdateTrade(price);
                    _logger.LogInformation(_batchLog.Print(_logId));
                }
                catch (Exception e)
                {
                    _logger.LogError(_batchLog.Print(_logId, $"{e.Message} {e.StackTrace}"));
                }
            }
        }


        public async void UpdateTrade(PriceDto price)
        {
            var transactions = await _cache.GetAsync<List<Transaction>>(TransactionCache);

            if (transactions == null)
            {
                _batchLog.Update(_logId, "No Transactions returned from Cache");
                return;
            }

            if (!transactions.Any())
            {
                _batchLog.Update(_logId, "No Transactions returned from Cache");
                return;
            }

            _batchLog.Update(_logId, $"Returned from Transaction Cache {transactions.Count}");

            foreach (var target in transactions.SelectMany(transaction => transaction.ProfitTargets))
            {
                _batchLog.Update(_logId,
                    $"Apply price Bid: {price.Bid} Ask: {price.Ask} to ProfitTargets {target.Units} {target.TargetPrice} {target.LastUpdated}");
                target.UpdateTrade(price);
            }

            foreach (var target in transactions.SelectMany(transaction => transaction.StopTargets))
            {
                _batchLog.Update(_logId,
                    $"Apply price Bid: {price.Bid} Ask: {price.Ask} to StopTargets {target.Units} {target.TargetPrice} {target.LastUpdated}");
                target.UpdateTrade(price);
            }

            _batchLog.Update(_logId, "Updating Transaction Cache");
            await _cache.SetAsync(TransactionCache, transactions);
        }
    }
}