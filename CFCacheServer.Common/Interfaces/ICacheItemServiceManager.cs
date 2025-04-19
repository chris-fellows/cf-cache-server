namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Manager of ICacheItemService instances
    /// </summary>
    public interface ICacheItemServiceManager
    {
        string[] Environments { get; }

        ICacheItemService? GetByEnvironment(string environment, bool addIfMissing);
    }
}
