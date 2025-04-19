using CFCacheServer.Models;
using CFCacheServer.Utilities;
using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.MessageConverters
{
    internal class GetCacheItemKeysRequestConverter : IExternalMessageConverter<GetCacheItemKeysRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetCacheItemKeysRequest externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                     new ConnectionMessageParameter()
                    {
                        Name = "SecurityKey",
                        Value = externalMessage.SecurityKey
                    },
                        new ConnectionMessageParameter()
                      {
                          Name = "ClientSessionId",
                          Value = externalMessage.ClientSessionId
                      },
                          new ConnectionMessageParameter()
                      {
                          Name = "Environment",
                          Value = externalMessage.Environment
                      },
                   new ConnectionMessageParameter()
                   {
                       Name = "Filter",
                       Value = externalMessage.Filter == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.Filter,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetCacheItemKeysRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetCacheItemKeysRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                Environment = connectionMessage.Parameters.First(p => p.Name == "Environment").Value
            };

            // Get filter
            var filterParameter = connectionMessage.Parameters.First(p => p.Name == "Filter");
            if (!String.IsNullOrEmpty(filterParameter.Value))
            {
                externalMessage.Filter = JsonUtilities.DeserializeFromBase64String<CacheItemFilter>(filterParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
