using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Trade;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeProfileRiskThreeTimesEqual : ITradeProfile
    {
        public Transaction Generate(PriceDto price, string buySell)
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

            return new Transaction(tradeParams);

        }
    }
}