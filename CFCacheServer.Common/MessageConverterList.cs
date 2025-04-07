using CFCacheServer.MessageConverters;
using CFCacheServer.Models;
using CFConnectionMessaging.Interfaces;

namespace CFCacheServer
{
    /// <summary>
    /// Message converters
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

        public IExternalMessageConverter<AddCacheItemRequest> AddCacheItemRequestConverter => _addCacheItemRequestConverter;
        public IExternalMessageConverter<AddCacheItemResponse> AddCacheItemResponseConverter => _addCacheItemResponseConverter;

        public IExternalMessageConverter<DeleteCacheItemRequest> DeleteCacheItemRequestConverter => _deleteCacheItemRequestConverter;
        public IExternalMessageConverter<DeleteCacheItemResponse> DeleteCacheItemResponseConverter => _deleteCacheItemResponseConverter;

        public IExternalMessageConverter<GetCacheItemKeysRequest> GetCacheItemKeysRequestConverter => _getCacheItemKeysRequestConverter;
        public IExternalMessageConverter<GetCacheItemKeysResponse> GetCacheItemKeysResponseConverter => _getCacheItemKeysResponseConverter;

        public IExternalMessageConverter<GetCacheItemRequest> GetCacheItemRequestConverter => _getCacheItemRequestConverter;
        public IExternalMessageConverter<GetCacheItemResponse> GetCacheItemResponseConverter => _getCacheItemResponseConverter;
    }
}
