using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public class Transaction
    {
        private readonly decimal _spread;

        public Transaction(decimal spread, double riskReward, decimal entryPrice, int tradeCounter, string riskRewardProfile,
            int totalUnits, string buySell, PriceLevel priceLevel)
        {
            ProfitTargets = new List<TransactionPriceTarget>();
            StopTargets = new List<TransactionPriceTarget>();
            _spread = spread;
            SpreadWithDirectionApplied = buySell == "sell" ? -Spread : Spread;
            RiskReward = riskReward;
            EntryPrice = entryPrice;
            TradeCounter = tradeCounter;
            RiskRewardProfile = riskRewardProfile;
            TotalUnits = totalUnits;
            BuySell = buySell;
            PriceLevel = priceLevel;
            MaxPrice = entryPrice;

            AddProfitTargets();
            AddCloseTargets();
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

        public PriceLevel PriceLevel { get; }

        public List<TransactionPriceTarget> ProfitTargets { get; set; }
        public List<TransactionPriceTarget> StopTargets { get; set; }

        public string TradeStatus { get; set; }


        public string BuySell { get; }
        public string RiskRewardProfile { get; }
        public double RiskReward { get; }


        public decimal Spread => _spread / 10000;

        public decimal EntryPrice { get; }
        public int TotalUnits { get; }

        public decimal SpreadWithDirectionApplied { get; }

        public int TradeCounter { get; }

        public bool Closed { get; set; }


        public decimal MaxPrice { get; set; }

        public void SetMaxPrice(decimal price)
        {
            if (BuySell == "buy")
            {
                MaxPrice = price > MaxPrice ? price : MaxPrice;
            }
            else
            {
                MaxPrice = price < MaxPrice ? price : MaxPrice; 
            }
        }

        public double MaxRiskReward { get; set; }


        private void AddProfitTargets()
        {
            if (RiskRewardProfile == "basic-split-profit-equally")
            {
                for (var i = 1; i <= TradeCounter; i++)
                {
                    ProfitTargets.Add(new TransactionPriceTarget()
                    {
                        EntryPrice = EntryPrice,
                        Closed = false,
                        Units = TotalUnits / TradeCounter,
                        TargetPrice = EntryPrice + (SpreadWithDirectionApplied * i),
                        LastUpdated = DateTime.Now
                    });
                }
            }
        }

        private void AddCloseTargets()
        {
            StopTargets.Add(new TransactionPriceTarget()
            {
                Units = TotalUnits,
                EntryPrice = EntryPrice,
                TargetPrice = EntryPrice - SpreadWithDirectionApplied,
                Closed = false,
                LastUpdated = DateTime.Now
            });
        }
    }
}