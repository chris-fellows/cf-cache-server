using CFCacheServer.Models;

namespace CFCacheServer.Server
{
    internal class ServerResources
    {
        public ClientsConnection ClientsConnection { get; set; } = new();

        public List<CacheEnvironment> CacheEnvironments { get; set; } = new();
    }
}
