using CFCacheServer.Interfaces;
using CFCacheServer.Logging;
using CFCacheServer.Models;

namespace CFCacheServer.Server.MessageProcessors
{
    internal abstract class MessageProcessorBase
    {
        protected readonly ICacheItemServiceManager _cacheItemServiceManager;
        protected readonly ISimpleLog _log;
        protected readonly IServiceProvider _serviceProvider;
        protected ServerResources? _serverResources;

        public MessageProcessorBase(ICacheItemServiceManager cacheItemServiceManager, ISimpleLog log, IServiceProvider serviceProvider)
        {
            _cacheItemServiceManager = cacheItemServiceManager;
            _log = log;
            _serviceProvider = serviceProvider;
        }

        public void SetServerResources(object serverResources)
        {
            _serverResources = (ServerResources)serverResources;
        }

        /// <summary>
        /// Gets cache environment by security key
        /// </summary>
        /// <param name="securityKey"></param>
        /// <returns></returns>
        protected CacheEnvironment? GetCacheEnvironmentBySecurityKey(string securityKey)
        {
            return _serverResources.CacheEnvironments.FirstOrDefault(e => e.SecurityKey == securityKey);
        }
    }
}
