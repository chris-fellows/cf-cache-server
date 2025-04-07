using CFCacheServer.Models;

namespace CFCacheServer.Common.Interfaces
{
    /// <summary>
    /// Cache service
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Clears cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="cacheItem"></param>
        void Add(CacheItemInternal cacheItem);

        /// <summary>
        /// Get item
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        CacheItemInternal? Get(string key);

        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="key"></param>
        void Delete(string key);

        /// <summary>
        /// Item count
        /// </summary>
        int ItemCount { get; }
    }
}
