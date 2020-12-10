using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeProfile
    {
        Transaction Generate(PriceDto price, string buySell);
    }
}