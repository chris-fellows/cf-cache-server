using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Logging
{
    public class SimpleConsoleLog : ISimpleLog
    {
        public void Log(DateTimeOffset date, string type, string message)
        {
            Console.WriteLine($"{date} {type} {message}");
        }
    }
}
