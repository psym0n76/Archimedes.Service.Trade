namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeProfileFactory
    {
        ITradeProfile GetTradeGenerationService(string tradeProfile);
    }
}