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
    internal class GetCacheItemRequestConverter : IExternalMessageConverter<GetCacheItemRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetCacheItemRequest externalMessage)
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
                       Name = "ItemKey",
                       Value = externalMessage.ItemKey
                   }
                }
            };
            return connectionMessage;
        }

        public GetCacheItemRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetCacheItemRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                Environment = connectionMessage.Parameters.First(p => p.Name == "Environment").Value,
                ItemKey = connectionMessage.Parameters.First(p => p.Name == "ItemKey").Value
            };    

            return externalMessage;
        }
    }
}
