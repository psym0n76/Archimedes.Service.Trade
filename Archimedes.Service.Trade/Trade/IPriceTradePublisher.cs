using System.Collections.Generic;
using System.Threading.Tasks;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IPriceTradePublisher
    {
        Task UpdateSubscribers(PriceLevelDto priceLevel);
        Task UpdatePriceLevelCache(List<PriceLevelDto> cachePriceLevels);
        Task UpdateLastPriceCache(PriceDto price);
    }
}