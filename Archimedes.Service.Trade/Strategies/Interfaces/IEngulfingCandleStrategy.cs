using System;
using System.Threading;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IEngulfingCandleStrategy
    {
        void Consume(string market, string granularity, string tradeProfile, CancellationToken token);
        event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;
    }
}