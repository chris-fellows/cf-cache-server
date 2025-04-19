using CFCacheServer.Common.Data;
using CFCacheServer.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CFCacheServer.Services
{
    public class CacheItemServiceManager : ICacheItemServiceManager
    {
        private readonly List<ICacheItemService> _cacheItemServices = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly Mutex _mutex = new();

        public CacheItemServiceManager(IDbContextFactory<CFCacheServerDataContext> dbFactory,
                                        IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Initialise cache item services per environment
            using (var context = dbFactory.CreateDbContext())
            {
                var environments = context.CacheItem.Select(c => c.Environment).Distinct().ToList();
                foreach (var environment in environments)
                {
                    var cacheItem = GetByEnvironment(environment, true);
                }
            }
        }    
        
        public string[] Environments
        {
            get { return _cacheItemServices.Select(c => c.Environment).ToArray(); }
        }

        public ICacheItemService? GetByEnvironment(string environment, bool addIfMissing)
        {
            try
            {
                _mutex.WaitOne();

                var cacheItemService = _cacheItemServices.FirstOrDefault(s => s.Environment.ToLower() == environment.ToLower());
                if (cacheItemService == null && addIfMissing)
                {                    
                    cacheItemService = _serviceProvider.GetRequiredService<ICacheItemService>();
                    cacheItemService.Environment = environment;     // Loads cache items
                    _cacheItemServices.Add(cacheItemService);
                }

                return cacheItemService;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}
