using CFCacheServer.Constants;
using CFCacheServer.Enums;
using CFCacheServer.Interfaces;
using CFCacheServer.Logging;
using CFCacheServer.Models;
using CFCacheServer.Services;
using CFCacheServer.Utilities;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Server.MessageProcessors
{
    internal class DeleteCacheItemRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public DeleteCacheItemRequestProcessor(ICacheItemServiceManager cacheItemServiceManager, ISimpleLog log, IServiceProvider serviceProvider) : base(cacheItemServiceManager, log, serviceProvider)
        {

        }


        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                var deleteCacheItemRequest = (DeleteCacheItemRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {deleteCacheItemRequest.TypeId}");

                var response = new DeleteCacheItemResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = deleteCacheItemRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {
                    // Get cache environment
                    var cacheEnvironment = GetCacheEnvironmentBySecurityKey(deleteCacheItemRequest.SecurityKey);

                    // Get cache item service for environment (Might not exist)
                    var cacheItemService = cacheEnvironment == null ? null : _cacheItemServiceManager.GetByCacheEnvironmentId(cacheEnvironment.Id);

                    if (cacheEnvironment == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(deleteCacheItemRequest.ItemKey))
                        {
                            _log.Log(DateTimeOffset.UtcNow, "Information", "Cleared cache");

                            // Clear
                            cacheItemService.DeleteAll();
                        }
                        else
                        {
                            _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleted cache item {deleteCacheItemRequest.ItemKey}");

                            // Delete cache item
                            cacheItemService.Delete(deleteCacheItemRequest.ItemKey);
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";
                }
                finally
                {
                    // Send response
                    _serverResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {deleteCacheItemRequest.TypeId}");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.DeleteCacheItemRequest;
        }
    }
}
