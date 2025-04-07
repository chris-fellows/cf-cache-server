using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Logging
{
    public interface ISimpleLog
    {
        void Log(DateTimeOffset date, string type, string message);
    }
}
