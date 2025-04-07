namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Cache server client
    /// </summary>
    public interface ICacheServerClient
    {
        /// <summary>
        /// Adds cache item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        Task AddAsync<T>(string key, T value, TimeSpan expiry);

        /// <summary>
        /// Deletes cache item by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task DeleteAsync(string key);

        /// <summary>
        /// Gets cache item by key
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T?> GetByKeyAsync<T>(string key);
    }
}
