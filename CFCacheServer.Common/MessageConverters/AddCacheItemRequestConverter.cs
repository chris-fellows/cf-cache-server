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
    internal class AddCacheItemRequestConverter : IExternalMessageConverter<AddCacheItemRequest>
    {
        public ConnectionMessage GetConnectionMessage(AddCacheItemRequest externalMessage)
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
                       Name = "CacheItem",
                       Value = externalMessage.CacheItem == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.CacheItem,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public AddCacheItemRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddCacheItemRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                Environment = connectionMessage.Parameters.First(p => p.Name == "Environment").Value
            };

            // Get cache item
            var cacheItemParameter = connectionMessage.Parameters.First(p => p.Name == "CacheItem");
            if (!String.IsNullOrEmpty(cacheItemParameter.Value))
            {
                externalMessage.CacheItem = JsonUtilities.DeserializeFromBase64String<CacheItem>(cacheItemParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
