using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class GetCacheItemRequest : MessageBase
    {
        public string ItemKey { get; set; } = String.Empty;

        public GetCacheItemRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetCacheItemRequest;
        }
    }
}
