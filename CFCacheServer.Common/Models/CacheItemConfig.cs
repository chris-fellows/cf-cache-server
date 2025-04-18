namespace CFCacheServer.Models
{
    public class CacheItemConfig
    {
        public TimeSpan Expiry { get; set; }

        public bool Persist { get; set; }
    }
}
