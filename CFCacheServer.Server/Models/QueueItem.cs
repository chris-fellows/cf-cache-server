using CFConnectionMessaging.Models;
using CFCacheServer.Server.Enums;
using CFCacheServer.Models;

namespace CFCacheServer.Server.Models
{
    public class QueueItem
    {
        public QueueItemTypes ItemType { get; set; }

        public MessageBase? Message { get; set; }

        public MessageReceivedInfo? MessageReceivedInfo { get; set; }
    }
}
