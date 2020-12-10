using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Trade.Trade;

namespace Archimedes.Service.Trade
{
    public class Transaction
    {
        private readonly TradeParameters _tradeParameters;

        public Transaction(TradeParameters tradeParameters)
        {
            _tradeParameters = tradeParameters;
        }


        public List<TransactionPriceTarget> ProfitTargets => _tradeParameters.ProfitTargets;
        public List<TransactionPriceTarget> StopTargets => _tradeParameters.StopTargets;


        public string RiskRewardProfile => _tradeParameters.RiskRewardProfile;
        public string Market => _tradeParameters.Market;
        public string BuySell => _tradeParameters.BuySell;
        public double RiskReward => _tradeParameters.RiskReward;
        public decimal Spread => _tradeParameters.SpreadAsPips;
        public decimal EntryPrice => _tradeParameters.EntryPrice;
        public int TradeCounter => _tradeParameters.TradeCounter;
        public bool Closed { get; set; }

        public decimal MaxPrice { get; private set; }



        public void SetMaxPrice(decimal price)
        {
            if (BuySell == "buy")
            {
                MaxPrice = price > MaxPrice ? price : MaxPrice;
            }
            else
            {
                if (MaxPrice == 0)
                {
                    MaxPrice = price;
                }
                else
                {
                    MaxPrice = price < MaxPrice ? price : MaxPrice;
                }

            }
        }

        public void UpdateTrades(PriceDto price)
        {
            foreach (var profitTarget in ProfitTargets)
            {
                profitTarget.UpdateTrade(price);
            }

            Closed = ProfitTargets.Any(a => a.Closed);

            foreach (var stop in StopTargets)
            {
                stop.UpdateTrade(price);
                Closed = stop.Closed;
            }
        }

    }
}