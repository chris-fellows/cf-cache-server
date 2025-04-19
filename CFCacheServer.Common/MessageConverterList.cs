using CFCacheServer.Constants;
using CFCacheServer.MessageConverters;
using CFCacheServer.Models;
using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;

namespace CFCacheServer
{
    /// <summary>
    /// Message converters between connection message (Used internally by CFConnectionMessaging) and external message (Used by us)
    /// </summary>
    public class MessageConverterList
    {
        private readonly IExternalMessageConverter<AddCacheItemRequest> _addCacheItemRequestConverter = new AddCacheItemRequestConverter();
        private readonly IExternalMessageConverter<AddCacheItemResponse> _addCacheItemResponseConverter = new AddCacheItemResponseConverter();

        private readonly IExternalMessageConverter<DeleteCacheItemRequest> _deleteCacheItemRequestConverter = new DeleteCacheItemRequestConverter();
        private readonly IExternalMessageConverter<DeleteCacheItemResponse> _deleteCacheItemResponseConverter = new DeleteCacheItemResponseConverter();

        private readonly IExternalMessageConverter<GetCacheItemKeysRequest> _getCacheItemKeysRequestConverter = new GetCacheItemKeysRequestConverter();
        private readonly IExternalMessageConverter<GetCacheItemKeysResponse> _getCacheItemKeysResponseConverter = new GetCacheItemKeysResponseConverter();

        private readonly IExternalMessageConverter<GetCacheItemRequest> _getCacheItemRequestConverter = new GetCacheItemRequestConverter();
        private readonly IExternalMessageConverter<GetCacheItemResponse> _getCacheItemResponseConverter = new GetCacheItemResponseConverter();

        /// <summary>
        /// Gets external message from connection message
        /// </summary>
        /// <param name="connectionMessage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public MessageBase GetExternalMessage(ConnectionMessage connectionMessage)
        {
            return connectionMessage.TypeId switch
            {
                MessageTypeIds.AddCacheItemRequest => _addCacheItemRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.AddCacheItemResponse => _addCacheItemResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.DeleteCacheItemRequest => _deleteCacheItemRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.DeleteCacheItemResponse => _deleteCacheItemResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetCacheItemKeysRequest => _getCacheItemKeysRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetCacheItemKeysResponse => _getCacheItemKeysResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetCacheItemRequest => _getCacheItemRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetCacheItemResponse => _getCacheItemResponseConverter.GetExternalMessage(connectionMessage),
                
                _ => throw new ArgumentException("Cannot convert connection message to external message")
            };
        }

        /// <summary>
        /// Gets connection message from external message
        /// </summary>
        /// <param name="externalMessage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ConnectionMessage GetConnectionMessage(MessageBase externalMessage)
        {
            return externalMessage.TypeId switch
            {
                MessageTypeIds.AddCacheItemRequest => _addCacheItemRequestConverter.GetConnectionMessage((AddCacheItemRequest)externalMessage),
                MessageTypeIds.AddCacheItemResponse => _addCacheItemResponseConverter.GetConnectionMessage((AddCacheItemResponse)externalMessage),

                MessageTypeIds.DeleteCacheItemRequest => _deleteCacheItemRequestConverter.GetConnectionMessage((DeleteCacheItemRequest)externalMessage),
                MessageTypeIds.DeleteCacheItemResponse => _deleteCacheItemResponseConverter.GetConnectionMessage((DeleteCacheItemResponse)externalMessage),

                MessageTypeIds.GetCacheItemKeysRequest => _getCacheItemKeysRequestConverter.GetConnectionMessage((GetCacheItemKeysRequest)externalMessage),
                MessageTypeIds.GetCacheItemKeysResponse => _getCacheItemKeysResponseConverter.GetConnectionMessage((GetCacheItemKeysResponse)externalMessage),

                MessageTypeIds.GetCacheItemRequest => _getCacheItemRequestConverter.GetConnectionMessage((GetCacheItemRequest)externalMessage),
                MessageTypeIds.GetCacheItemResponse => _getCacheItemResponseConverter.GetConnectionMessage((GetCacheItemResponse)externalMessage),

                _ => throw new ArgumentException("Cannot convert external to connection message")
            };
        }
    }
}
