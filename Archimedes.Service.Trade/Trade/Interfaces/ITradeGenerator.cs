namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeGenerator
    {
        void Executor_TradeMessageEventHandler(object sender, TradeMessageHandlerEventArgs e);
    }
}