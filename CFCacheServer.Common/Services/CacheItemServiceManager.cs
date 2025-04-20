using CFCacheServer.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CFCacheServer.Services
{
    public class CacheItemServiceManager : ICacheItemServiceManager
    {        
        private readonly List<ICacheItemService> _cacheItemServices = new();        
        private readonly IServiceProvider _serviceProvider;     

        public CacheItemServiceManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Set cache environment services
            var cacheEnvironmentService = _serviceProvider.GetRequiredService<ICacheEnvironmentService>();
            var cacheEnvironments = cacheEnvironmentService.GetAll();
            foreach(var cacheEnvironment in cacheEnvironments)
            {
                var cacheItemService = _serviceProvider.GetRequiredService<ICacheItemService>();
                cacheItemService.CacheEnvironmentId = cacheEnvironment.Id;
                _cacheItemServices.Add(cacheItemService);
            }
        }    
        
        public ICacheItemService? GetByCacheEnvironmentId(string cacheEnvironmentId)
        {            
            return _cacheItemServices.First(c => c.CacheEnvironmentId == cacheEnvironmentId);            
        }
    }
}
