using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class GetCacheItemResponse : MessageBase
    {       
        public CacheItem? CacheItem { get; set; }

        public GetCacheItemResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetCacheItemResponse;
        }
    }
}
