namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Manager of ICacheItemService instances
    /// </summary>
    public interface ICacheItemServiceManager
    {        
        ICacheItemService? GetByCacheEnvironmentId(string id);
    }
}
