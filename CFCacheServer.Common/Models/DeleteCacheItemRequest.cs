using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class DeleteCacheItemRequest : MessageBase
    {
        public string Environment { get; set; } = String.Empty;

        public string ItemKey { get; set; } = String.Empty;

        public DeleteCacheItemRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.DeleteCacheItemRequest;
        }
    }
}
