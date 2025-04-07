using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class DeleteCacheItemResponse : MessageBase
    {
        public DeleteCacheItemResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.DeleteCacheItemResponse;
        }
    }
}
