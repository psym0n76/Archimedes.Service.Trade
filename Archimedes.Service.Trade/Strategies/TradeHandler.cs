using System;
using System.Linq;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Http;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeHandler : ITradeHandler
    {
        private readonly IHttpRepositoryClient _http;

        public TradeHandler(ITradeExecutorPrice executor, IHttpRepositoryClient http)
        {
            _http = http;
            executor.TradeMessageEventHandler += Executor_TradeMessageEventHandler;
        }

        private void Executor_TradeMessageEventHandler(object sender, TradeMessageHandlerEventArgs e)
        {
            // new trades are consumed here
            //create a transactino obect based on trade type
        }


        public void TradeConsumer()
        {
            throw new System.NotImplementedException();
        }

        public void PostTrade(Transaction transaction)
        {
            foreach (var tradeDto in transaction.ProfitTargets.Select(profitTarget => new TradeDto()
            {
                Market = transaction.PriceLevel.Market,
                BuySell = transaction.BuySell,
                EntryPrice = profitTarget.EntryPrice,
                TargetPrice = profitTarget.TargetPrice,
                ClosePrice = transaction.StopTargets.First().TargetPrice,
                Strategy = transaction.RiskRewardProfile,
                Success = false,
                Timestamp = DateTime.Now
            }))
            {
                _http.AddTrade(tradeDto);
            }
        }
    }
}