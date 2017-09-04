using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Data.Storage
{
    public abstract class RepositoryCacheItem<T> : IRepositoryCacheItem<T>
    {
        private static Lazy<MemoryCacheEntryOptions> _memoryCacheEntryOptionsLazy = new Lazy<MemoryCacheEntryOptions>();
        private static Lazy<IMemoryCache> _memoryCacheLazy = new Lazy<IMemoryCache>(GetDefaultMemoryCache);

        protected RepositoryCacheItem() : this(TimeSpan.FromMinutes(30))
        {
            AddNullToCache = true;
        }

        protected RepositoryCacheItem(TimeSpan timeout)
        {
            Timeout = timeout;

            _memoryCacheEntryOptionsLazy = new Lazy<MemoryCacheEntryOptions>(() =>
            {
                return new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    AbsoluteExpirationRelativeToNow = timeout
                };
            });
        }

        protected IRepositoryCacheDelegate<T> RepositoryDelegate { get; set; }
        private static IMemoryCache Cache => _memoryCacheLazy.Value;
        private static MemoryCacheEntryOptions MemoryCacheEntryOptions => _memoryCacheEntryOptionsLazy.Value;
        private bool AddNullToCache { get; set; }
        private TimeSpan Timeout { get; set; }

        public virtual async Task<T> GetResultAsync(CancellationToken cancellationToken, bool useCache = true)
        {
            T result;

            if (useCache)
            {
                if (Cache.TryGetValue(RepositoryDelegate, out result))
                {
                    return result;
                }
            }

            result = await RepositoryDelegate.GetResult(cancellationToken);

            if (!useCache || (result == null && !AddNullToCache))
            {
                return result;
            }

            Cache.Set(RepositoryDelegate, result, MemoryCacheEntryOptions);

            return result;
        }

        public virtual void Invalidate()
        {
            Cache.Dispose();

            _memoryCacheLazy = new Lazy<IMemoryCache>(GetDefaultMemoryCache);
        }

        public void Remove()
        {
            Cache.Remove(RepositoryDelegate);
        }

        public virtual void SetAddNullToCache(bool addNullToCache)
        {
            AddNullToCache = addNullToCache;
        }

        public virtual void SetTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
        }

        private static IMemoryCache GetDefaultMemoryCache()
        {
            var memoryCacheOptions = new MemoryCacheOptions
            {
                CompactOnMemoryPressure = true
            };

            return new MemoryCache(memoryCacheOptions);
        }
    }
}