using System.Threading;

namespace Archimedes.Service.Trade
{
    public interface IStrategyRunner
    {
        void Run(string market, string granularity, CancellationToken token);
    }
}