using System.Threading;
using Archimedes.Library.Message.Dto;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IBasicPriceStrategy
    {
        void Consume(string tradeProfile, PriceDto price, CancellationToken token);
    }
}