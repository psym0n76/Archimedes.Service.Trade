using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;
using Microsoft.Extensions.Logging;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeValuation : ITradeValuation
    {
        private const string TransactionCache = "transaction";
        private readonly object _locker = new object();
        private readonly ICacheManager _cache;
        private readonly IHttpTradeRepository _tradeRepository;
        private readonly BatchLog _batchLog = new();
        private string _logId;
        private readonly ILogger<TradeValuation> _logger;

        public TradeValuation(ICacheManager cache, ILogger<TradeValuation> logger, IHttpTradeRepository tradeRepository)
        {
            _cache = cache;
            _logger = logger;
            _tradeRepository = tradeRepository;
        }

        public void UpdateTradeLocked(PriceDto price)
        {
            lock (_locker)
            {
                try
                {
                    //var id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    _logId = _batchLog.Start();
                    UpdateTrade(price);
                    //UpdateTradeTable();
                    _logger.LogInformation(_batchLog.Print(_logId));
                }
                catch (Exception e)
                {
                    _logger.LogError(_batchLog.Print(_logId,
                        $"Error returned from TradeValuation Price Bid: {price.Bid} Ask: {price.Ask} {price.TimeStamp}", e));
                }
            }
        }

        private async void UpdateTradeTable()
        {
            var transactions = await _cache.GetAsync<List<Transaction>>(TransactionCache);

            // map a transaction to a trade

            foreach (var transaction in transactions)
            {
                foreach (var priceTarget in transaction.ProfitTargets)
                {
                    
                }
            }
            
            var trade = new TradeDto()
            {
                //Id = transactions[0].
            };

            await _tradeRepository.UpdateTrade(trade);
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