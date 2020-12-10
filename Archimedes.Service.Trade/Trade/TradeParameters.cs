using System;
using System.Collections.Generic;

namespace Archimedes.Service.Trade.Trade
{
    public class TradeParameters
    {
        public decimal EntryPrice { get; set; }
        public string BuySell { get; set; }


        public int SpreadAsPips { get; set; }
        public decimal Spread => Convert.ToDecimal(SpreadAsPips) / 10000;


        public int RiskReward { get; set; }
        public int TradeCounter { get; set; }

        public string Market { get; set; }


        public decimal TargetPrice(int i)
        {
            return EntryPrice + (BuySell == "SELL" ? -Spread * i : Spread * i);
        }

        public decimal SetStopPrice()
        {
            return EntryPrice - (BuySell == "SELL" ? -Spread : Spread);
        }

        public List<TransactionPriceTarget> ProfitTargets => SetPriceTargets();
        public List<TransactionPriceTarget> StopTargets => SetStopTargets();
        public string RiskRewardProfile { get; set; }

        private List<TransactionPriceTarget> SetStopTargets()
        {
            var stopTargets = new List<TransactionPriceTarget>
            {
                new TransactionPriceTarget()
                {
                    EntryPrice = EntryPrice,
                    Closed = false,
                    TargetPrice = SetStopPrice(),
                    LastUpdated = DateTime.Now
                }
            };

            return stopTargets;
        }

        private List<TransactionPriceTarget> SetPriceTargets()
        {
            var priceTargets = new List<TransactionPriceTarget>();

            for (var tradeIndex = 1; tradeIndex <= TradeCounter; tradeIndex++)
            {
                priceTargets.Add(new TransactionPriceTarget()
                {
                    EntryPrice = EntryPrice,
                    Closed = false,
                    TargetPrice = TargetPrice(tradeIndex),
                    LastUpdated = DateTime.Now
                });
            }

            return priceTargets;
        }
    }
}