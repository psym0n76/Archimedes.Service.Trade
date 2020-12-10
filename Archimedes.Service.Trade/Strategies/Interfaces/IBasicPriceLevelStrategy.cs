using System.Collections.Generic;
using System.Threading;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Price
{
    public interface IBasicPriceLevelStrategy
    {
        void Consume(List<PriceLevelDto> priceLevels, CancellationToken token);
    }
}