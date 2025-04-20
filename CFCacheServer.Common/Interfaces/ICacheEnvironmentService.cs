using CFCacheServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Interfaces
{
    public interface ICacheEnvironmentService
    {
        void Add(CacheEnvironment cacheEnvironment);

        List<CacheEnvironment> GetAll();
    }
}
