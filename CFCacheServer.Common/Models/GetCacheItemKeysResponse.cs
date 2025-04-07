using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class GetCacheItemKeysResponse : MessageBase
    {
        public List<string> ItemKeys { get; set; } = new();

        public GetCacheItemKeysResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetCacheItemKeysResponse;
        }
    }
}
