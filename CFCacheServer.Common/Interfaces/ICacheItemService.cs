﻿using CFCacheServer.Models;
using System.Net;

namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Cache service
    /// </summary>
    public interface ICacheItemService
    {
        /// <summary>
        /// Environment
        /// </summary>
        string CacheEnvironmentId { get; set; }

        /// <summary>
        /// Deletes all cache items
        /// </summary>
        void DeleteAll();

        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="cacheItem"></param>
        /// <param name="persist"></param>
        void Add(CacheItem cacheItem);

        /// <summary>
        /// Get item
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        CacheItem? Get(string key);

        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="key"></param>
        void Delete(string key);

        /// <summary>
        /// Item count
        /// </summary>
        int ItemCount { get; }

        /// <summary>
        /// Total size (Bytes)
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// Gets cache item keys by filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        List<string> GetKeysByFilter(CacheItemFilter filter);
    }
}
