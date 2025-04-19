using CFCacheServer.Models;

namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Cache server client
    /// </summary>
    public interface ICacheServerClient
    {
        /// <summary>
        /// Cache server environment
        /// </summary>
        string Environment { get; set; }

        /// <summary>
        /// Adds cache item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <param name="persist"></param>
        /// <returns></returns>
        Task<bool> AddAsync<T>(string key, T value, TimeSpan expiry, bool persist);

        /// <summary>
        /// Deletes all cache items
        /// </summary>        
        Task<bool> DeleteAllAsync();

        /// <summary>
        /// Deletes cache item by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(string key);

        /// <summary>
        /// Gets cache item by key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T?> GetByKeyAsync<T>(string key);

        /// <summary>
        /// Gets cache item keys for filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<List<string>> GetKeysByFilterAsync(CacheItemFilter filter);
    }
}
