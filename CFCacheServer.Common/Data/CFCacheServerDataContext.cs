using CFCacheServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Common.Data
{
    public class CFCacheServerDataContext : DbContext
    {
        public CFCacheServerDataContext(DbContextOptions<CFCacheServerDataContext> options)
            : base(options)
        {
        }

        public DbSet<CacheEnvironment> CacheEnvironment { get; set; }

        public DbSet<CacheItem> CacheItem { get; set; } = default!;
    }
}
