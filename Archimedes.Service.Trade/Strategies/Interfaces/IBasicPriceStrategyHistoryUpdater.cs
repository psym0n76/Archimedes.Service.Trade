using System.Threading.Tasks;

namespace Archimedes.Service.Trade.Strategies
{
    public interface IBasicPriceStrategyHistoryUpdater
    {
        void UpdateHistory(string market, string granularity);
    }
}