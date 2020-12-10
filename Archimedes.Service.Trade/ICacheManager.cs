using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Archimedes.Service.Trade
{
    public interface ICacheManager
    {
        Task SetAsync<T>(string key, T item, DistributedCacheEntryOptions options = null);
        Task<T> GetAsync<T>(string key);
    }
}