using System.Threading;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IBasicPriceStrategy
    {
        void Consume(string tradeProfile, string market, CancellationToken token);
    }
}