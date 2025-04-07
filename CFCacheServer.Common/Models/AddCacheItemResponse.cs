using CFCacheServer.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class AddCacheItemResponse : MessageBase
    {
        public AddCacheItemResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddCacheItemResponse;
        }
    }
}
