using Archimedes.Library;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeProfile
    {
        TradeTransaction Generate(PriceDto price, string buySell);
    }
}