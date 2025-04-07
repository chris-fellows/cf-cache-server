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
    public class GetCacheItemKeysResponseConverter : IExternalMessageConverter<GetCacheItemKeysResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetCacheItemKeysResponse externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                   new ConnectionMessageParameter()
                    {
                        Name = "Response",
                        Value = externalMessage.Response == null ? "" :
                                    JsonUtilities.SerializeToBase64String(externalMessage.Response,
                                            JsonUtilities.DefaultJsonSerializerOptions)
                    },
                      new ConnectionMessageParameter()
                   {
                       Name = "ItemKeys",
                       Value = externalMessage.ItemKeys == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.ItemKeys,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetCacheItemKeysResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetCacheItemKeysResponse()
            {
                Id = connectionMessage.Id
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get cache item
            var itemKeysParameter = connectionMessage.Parameters.First(p => p.Name == "ItemKeys");
            if (!String.IsNullOrEmpty(itemKeysParameter.Value))
            {
                externalMessage.ItemKeys = JsonUtilities.DeserializeFromBase64String<List<string>>(itemKeysParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
