using System.Threading;
using System.Threading.Tasks;

namespace Archimedes.Service.Trade
{
    public interface IStrategyRunner
    {
        Task Run(string market, string granularity, string engulfGranularity, CancellationToken token);
        
    }
}