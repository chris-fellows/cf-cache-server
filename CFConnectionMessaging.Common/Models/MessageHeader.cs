using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFConnectionMessaging.Models
{
    internal class MessageHeader
    {
        public int PayloadLength { get; set; }

        public int HeaderLength = sizeof(Int32);
    }
}
