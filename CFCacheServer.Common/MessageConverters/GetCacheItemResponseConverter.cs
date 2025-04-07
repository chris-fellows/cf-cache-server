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
    public class GetCacheItemResponseConverter : IExternalMessageConverter<GetCacheItemResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetCacheItemResponse externalMessage)
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
                       Name = "CacheItem",
                       Value = externalMessage.CacheItem == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.CacheItem,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetCacheItemResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetCacheItemResponse()
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
            var cacheItemParameter = connectionMessage.Parameters.First(p => p.Name == "CacheItem");
            if (!String.IsNullOrEmpty(cacheItemParameter.Value))
            {
                externalMessage.CacheItem = JsonUtilities.DeserializeFromBase64String<CacheItemInternal>(cacheItemParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
