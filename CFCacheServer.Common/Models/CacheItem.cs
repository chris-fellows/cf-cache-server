using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Models
{
    public class CacheItem
    {
        public string Key { get; set; } = String.Empty;

        public byte[] Value { get; set; } = new byte[0];

        public string ValueType { get; set; } = String.Empty;

        public long ExpiryMilliseconds { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.MinValue;
    }
}
