using CFCacheServer.Common.Data;
using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Services
{
    public class CacheEnvironmentService : ICacheEnvironmentService, IDisposable
    {
        private readonly IDbContextFactory<CFCacheServerDataContext> _dbFactory;
        private CFCacheServerDataContext? _context;

        public CacheEnvironmentService(IDbContextFactory<CFCacheServerDataContext> dbFactory)
        {
            _dbFactory = dbFactory;

        }

        public void Add(CacheEnvironment cacheEnvironment)
        {
            Context.CacheEnvironment.Add(cacheEnvironment);
            Context.SaveChanges();
        }

        public List<CacheEnvironment> GetAll()
        {
            var cacheEnvironments = Context.CacheEnvironment.ToList();

            return cacheEnvironments;
        }

        protected CFCacheServerDataContext Context
        {
            get
            {
                lock (_dbFactory)
                {
                    if (_context == null) _context = _dbFactory.CreateDbContext();
                    return _context;
                }
            }
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }
    }
}
