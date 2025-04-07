using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class GetCacheItemKeysRequest : MessageBase
    {
        public CacheItemFilter? Filter { get; set; }

        public GetCacheItemKeysRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetCacheItemKeysRequest;
        }
    }
}
