namespace CFCacheServer.Server.Models
{
    public class SystemConfig
    {
        public int LocalPort { get; set; }

        public string LogFolder { get; set; } = String.Empty;

        public int MaxLogDays { get; set; }

        public int MaxConcurrentTasks { get; set; }

        public string SecurityKey { get; set; } = String.Empty;
    }
}
