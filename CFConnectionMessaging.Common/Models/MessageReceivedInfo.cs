using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFConnectionMessaging.Models
{
    public class MessageReceivedInfo
    {
        public DateTimeOffset ReceivedTime { get; set; }

        public EndpointInfo RemoteEndpointInfo { get; set; }
    }
}
