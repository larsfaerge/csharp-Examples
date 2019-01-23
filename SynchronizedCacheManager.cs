using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace csharpExamples.Caching
{
    public class  : ISynchronizedCacheManager
    {
        private readonly ICache _cache;
        public string CacheKeyPrefix { get { return "cachekey:"; } }

        public SynchronizedCacheManager(ICache cache)
        {
            _cache = cache;
        }

        public T Get<T>(string cacheKey, Func<T> delegateFunction, TimeSpan cacheTimeout, object syncLock) where T : class
        {
            return Get(cacheKey, delegateFunction, cacheTimeout, null, syncLock);
        }

        public T Get<T>(string cacheKey, Func<T> delegateFunction, TimeSpan cacheTimeout, IEnumerable<string> masterKeys, object syncLock) where T : class
        {
            var obj = _cache.Get(cacheKey);

            if (obj != null)
            {
                return obj is CachedNullValue ? null : (T)obj;
            }

            lock (syncLock) //double-checked sync lock
            {
                obj = _cache.Get(cacheKey);
                if (obj != null)
                {
                    return obj is CachedNullValue ? null : (T)obj;
                }

                obj = delegateFunction() ?? (object)new CachedNullValue();
                _cache.Add(cacheKey, obj, DateTime.Now.Add(cacheTimeout), GetMasterKeys(masterKeys));

                return obj is CachedNullValue ? null : (T)obj;
            }
        }

        public async Task<T> GetAsync<T>(string cacheKey, Func<Task<T>> delegateFunction, TimeSpan cacheTimeout, SemaphoreSlim mutex) where T : class
        {
            return await GetAsync(cacheKey, delegateFunction, cacheTimeout, null, mutex);
        }

        public async Task<T> GetAsync<T>(string cacheKey, Func<Task<T>> delegateFunction, TimeSpan cacheTimeout,
            IEnumerable<string> masterKeys, SemaphoreSlim mutex) where T : class
        {
            var obj = _cache.Get(cacheKey);

            if (obj != null)
            {
                return obj is CachedNullValue ? null : (T)obj;
            }

            await mutex.WaitAsync();

            try
            {
                obj = _cache.Get(cacheKey);
                if (obj != null)
                {
                    return obj is CachedNullValue ? null : (T)obj;
                }

                obj = await delegateFunction().ConfigureAwait(false) ?? (object)new CachedNullValue();
                _cache.Add(cacheKey, obj, DateTime.Now.Add(cacheTimeout), GetMasterKeys(masterKeys));
            }
            finally
            {
                mutex.Release();
            }

            return obj is CachedNullValue ? null : (T)obj;

        }

        private IEnumerable<string> GetMasterKeys(IEnumerable<string> masterKeys)
        {
            var masterCacheKeys = new List<string> { CacheKeyPrefix };

            if (masterKeys != null)
            {
                masterCacheKeys.AddRange(masterKeys);
            }

            return masterCacheKeys;
        }
    }
}
