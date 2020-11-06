using System;

namespace Archimedes.Service.Trade
{
    public class PriceEventArgs:EventArgs
    {
        public int Price { get; set; }
    }
}