using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.TestClient.Models
{
    public class TestObject
    {
        public string Id { get; set; } = String.Empty;

        public DateTimeOffset CreatedDateTime { get; set; }

        public bool BoolValue { get; set; }

        public Int16 Int16Value { get; set; }

        public Int32 Int32Value { get; set; }

        public Int64 Int64Value { get; set; }
    }
}
