using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Archimedes.Service.Trade
{

    public class CacheManager : ICacheManager
    {
        private readonly IDistributedCache _cache;

        public CacheManager(IDistributedCache cache)
        {
            _cache = cache;
        }

        public byte[] Get(string key)
        {
            return _cache.Get(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _cache.GetAsync(key, token);
        }

        public void Refresh(string key)
        {
            _cache.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _cache.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _cache.RemoveAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _cache.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            return _cache.SetAsync(key, value, options, token);
        }

        public async Task SetAsync<T>(string key, T item, DistributedCacheEntryOptions options = null)
        {
            if (item == null)
                return;

            if (options == null)
                options = new DistributedCacheEntryOptions();

            //serialize item
            var serializedItem = JsonConvert.SerializeObject(item);

            await _cache.SetStringAsync(key, serializedItem, options);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            var serializedItem = await _cache.GetStringAsync(key);

            if (serializedItem == null)
                return default(T);

            //deserialize item
            var item = JsonConvert.DeserializeObject<T>(serializedItem);

            return item == null ? default(T) : item;
        }


        public void Set<T>(string key, T item, DistributedCacheEntryOptions options = null)
        {
            if (item == null)
                return;

            if (options == null)
                options = new DistributedCacheEntryOptions();

            var serializedItem = JsonConvert.SerializeObject(item);

            _cache.SetStringAsync(key, serializedItem, options);
        }

        public T Get<T>(string key)
        {
            var serializedItem = _cache.GetStringAsync(key).Result;

            if (serializedItem == null)
                return default(T);

            var item = JsonConvert.DeserializeObject<T>(serializedItem);

            return item == null ? default(T) : item;
        }
    }
}