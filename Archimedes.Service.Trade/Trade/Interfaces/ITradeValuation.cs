using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface ITradeValuation
    {
        void UpdateTradeLocked(PriceDto price);
    }
}