using System;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeExecutor
    {
        void ExecuteLocked(PriceDto price, string tradeProfile);
        event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;
    }

    public class TradeMessageHandlerEventArgs:EventArgs
    {
        public Transaction Transaction { get; set; }
    }
}