using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class AddCacheItemRequest : MessageBase
    {
        public string Environment { get; set; } = String.Empty;

        public CacheItem CacheItem { get; set; } = new();

        public AddCacheItemRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddCacheItemRequest;
        }
    }
}
