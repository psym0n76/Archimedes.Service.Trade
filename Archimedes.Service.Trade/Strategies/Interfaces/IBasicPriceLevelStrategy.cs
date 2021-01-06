using System.Threading;

namespace Archimedes.Service.Price
{
    public interface IBasicPriceLevelStrategy
    {
        void Consume(string market, string granularity, CancellationToken token);
    }
}