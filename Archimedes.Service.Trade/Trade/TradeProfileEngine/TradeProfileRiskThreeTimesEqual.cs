using Archimedes.Library;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeProfileRiskThreeTimesEqual : ITradeProfile
    {
        public TradeTransaction Generate(PriceDto price, string buySell)
        {
            var tradeParams = new TradeParameters()
            {
                RiskRewardProfile = nameof(TradeProfileRiskThreeTimesEqual),
                SpreadAsPips = 10,
                RiskReward = 3,
                TradeCounter = 3,
                EntryPrice = 1.3m,
                BuySell = buySell,
                Market = price.Market
            };

            return new TradeTransaction(tradeParams);

        }
    }
}