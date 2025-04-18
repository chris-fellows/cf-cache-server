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
    public class AddCacheItemResponseConverter : IExternalMessageConverter<AddCacheItemResponse>
    {
        public ConnectionMessage GetConnectionMessage(AddCacheItemResponse externalMessage)
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
                    }
                }
            };
            return connectionMessage;
        }

        public AddCacheItemResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddCacheItemResponse()
            {
                Id = connectionMessage.Id                
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
