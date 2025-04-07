using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Server.Models
{
    public class SystemConfig
    {
        public int LocalPort { get; set; }

        public string SecurityKey { get; set; } = String.Empty;
    }
}
