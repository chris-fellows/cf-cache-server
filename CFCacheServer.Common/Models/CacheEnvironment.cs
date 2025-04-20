namespace CFCacheServer.Models
{
    /// <summary>
    /// Cache environment
    /// </summary>
    public class CacheEnvironment
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Environment name
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Security key
        /// </summary>
        public string SecurityKey { get; set; } = String.Empty;

        /// <summary>
        /// Max size (Bytes)
        /// </summary>
        public long MaxSize { get; set; }

        /// <summary>
        /// Max key length
        /// </summary>
        public int MaxKeyLength { get; set; }

        /// <summary>
        /// Percent used for warning
        /// </summary>
        public int PercentUsedForWarning { get; set; }
    }
}
