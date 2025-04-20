using Microsoft.EntityFrameworkCore;

namespace CFCacheServer.Models
{
    /// <summary>
    /// Cache item
    /// </summary>
    [Index(nameof(CacheEnvironmentId), nameof(Key), IsUnique = true)]
    public class CacheItem
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Cache enviroment
        /// </summary>
        public string CacheEnvironmentId { get; set; } = String.Empty;

        /// <summary>
        /// Key
        /// </summary>        
        public string Key { get; set; } = String.Empty;

        /// <summary>
        /// Value (Serialized)
        /// </summary>
        public byte[] Value { get; set; } = new byte[0];

        /// <summary>
        /// Value type
        /// </summary>
        public string ValueType { get; set; } = String.Empty;

        /// <summary>
        /// Expiry (Milliseconds)
        /// </summary>
        public long ExpiryMilliseconds { get; set; }

        /// <summary>
        /// Whether to persist to DB
        /// </summary>
        public bool Persist { get; set; }

        /// <summary>
        /// Created date and time
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.MinValue;

        public int GetTotalSize() => Value.Length;
    }
}
