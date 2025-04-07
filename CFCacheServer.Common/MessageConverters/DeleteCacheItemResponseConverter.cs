using CFCacheServer.Models;
using CFCacheServer.Utilities;
using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;

namespace CFCacheServer.MessageConverters
{
    public class DeleteCacheItemResponseConverter : IExternalMessageConverter<DeleteCacheItemResponse>
    {           
        public ConnectionMessage GetConnectionMessage(DeleteCacheItemResponse externalMessage)
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

        public DeleteCacheItemResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new DeleteCacheItemResponse()
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
