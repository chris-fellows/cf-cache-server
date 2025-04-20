namespace CFCacheServer.Server.Models
{
    /// <summary>
    /// System config
    /// </summary>
    public class SystemConfig
    {
        /// <summary>
        /// Local port to handle requests
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// Log folder
        /// </summary>
        public string LogFolder { get; set; } = String.Empty;

        /// <summary>
        /// Max days to get logs
        /// </summary>
        public int MaxLogDays { get; set; }

        /// <summary>
        /// Max concurrent tasks
        /// </summary>
        public int MaxConcurrentTasks { get; set; }

        /// <summary>
        /// Default max cache size
        /// </summary>
        public long DefaultMaxSize { get; set; }

        /// <summary>
        /// Default security key
        /// </summary>
        public string DefaultSecurityKey { get; set; } = String.Empty;
    }
}
