using System;

namespace Archimedes.Service.Trade.Strategies
{
    public class TradeMessageHandlerEventArgs : EventArgs
    {
        public Transaction Transaction { get; set; }
    }
}