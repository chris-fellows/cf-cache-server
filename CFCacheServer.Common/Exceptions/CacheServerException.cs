using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Exceptions
{
    public class CacheServerException : Exception
    {
        //public ResponseErrorCodes? ResponseErrorCode { get; set; }

        public CacheServerException()
        {
        }

        public CacheServerException(string message) : base(message)
        {
        }

        public CacheServerException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public CacheServerException(string message, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
