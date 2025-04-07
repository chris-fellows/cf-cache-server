using CFConnectionMessaging.Models;
using CFCacheServer.Server.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Server.Models
{
    public class QueueItem
    {
        public QueueItemTypes ItemType { get; set; }

        public ConnectionMessage? ConnectionMessage { get; set; }

        public MessageReceivedInfo? MessageReceivedInfo { get; set; }
    }
}
