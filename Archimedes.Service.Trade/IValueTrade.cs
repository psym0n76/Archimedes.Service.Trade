using System;

namespace Archimedes.Service.Trade
{
    public interface IValueTrade
    {
        event EventHandler<PriceEventArgs> HandleMessage;
    }
}