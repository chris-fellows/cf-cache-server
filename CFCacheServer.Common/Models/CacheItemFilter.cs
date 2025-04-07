namespace CFCacheServer.Models
{
    public class CacheItemFilter
    {
        public string KeyStartsWith { get; set; } = String.Empty;

        public string KeyEndsWith { get; set; } = String.Empty;

        public string KeyContains { get; set; } = String.Empty;
    }
}
