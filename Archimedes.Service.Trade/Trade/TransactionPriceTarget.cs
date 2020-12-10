using System;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade
{
    public class TransactionPriceTarget
    {
        public decimal EntryPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public bool Closed { get; set; }
        public int Units { get; set; }
        public DateTime LastUpdated { get; set; }

        public void UpdateTrade(PriceDto price)
        {
            if (TargetPrice > EntryPrice)
            {
                Closed = price.Bid > TargetPrice;
            }
            else
            {
                Closed = price.Ask < TargetPrice;
            }
        }
    }
}