using System;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeExecutorPrice
    {
        void Execute(PriceDto price, decimal lastBidPrice, decimal lastAskPrice);
        event EventHandler<TradeMessageHandlerEventArgs> TradeMessageEventHandler;
    }

    public class TradeMessageHandlerEventArgs:EventArgs
    {

    }
}