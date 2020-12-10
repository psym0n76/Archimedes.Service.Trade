using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeValuation : ITradeValuation
    {
        private const string TransactionCache = "transaction";
        private readonly object _locker = new object();
        private readonly ICacheManager _cache;

        public TradeValuation(ICacheManager cache)
        {
            _cache = cache;
        }


        public void UpdateTradeLocked(PriceDto price)
        {
            lock (_locker)
            {
                UpdateTrade(price);
            }
        }


        public async void UpdateTrade(PriceDto price)
        {
            var transactions = await _cache.GetAsync<List<Transaction>>(TransactionCache);

            foreach (var target in transactions.SelectMany(transaction => transaction.ProfitTargets))
            {
                target.UpdateTrade(price);
            }

            foreach (var target in transactions.SelectMany(transaction => transaction.StopTargets))
            {
                target.UpdateTrade(price);
            }

            await _cache.SetAsync(TransactionCache, transactions);
        }
    }
}