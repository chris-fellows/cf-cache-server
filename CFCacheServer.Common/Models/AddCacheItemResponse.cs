using CFCacheServer.Constants;

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
