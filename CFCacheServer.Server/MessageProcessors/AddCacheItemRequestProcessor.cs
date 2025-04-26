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
    internal class AddCacheItemRequestProcessor :MessageProcessorBase, IMessageProcessor
    {
        public AddCacheItemRequestProcessor(ICacheItemServiceManager cacheItemServiceManager, ISimpleLog log, IServiceProvider serviceProvider) : base(cacheItemServiceManager, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                var addCacheItemRequest = (AddCacheItemRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing request to add {addCacheItemRequest.CacheItem.Key} to cache ({messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port})");

                var response = new AddCacheItemResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addCacheItemRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {
                    // Get cache environment                    
                    var cacheEnvironment = GetCacheEnvironmentBySecurityKey(addCacheItemRequest.SecurityKey);

                    // Get cache item service for environment (Might not exist)
                    var cacheItemService = cacheEnvironment == null ? null : _cacheItemServiceManager.GetByCacheEnvironmentId(cacheEnvironment.Id);

                    if (cacheEnvironment == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (cacheItemService == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.CacheEnvironmentNotFound;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (String.IsNullOrWhiteSpace(addCacheItemRequest.CacheItem.Key))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Key is not set";
                    }
                    else if (cacheEnvironment.MaxKeyLength > 0 &&
                        addCacheItemRequest.CacheItem.Key.Length > cacheEnvironment.MaxKeyLength)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Key is too long";
                    }
                    else if (cacheEnvironment.MaxSize > 0 &&
                        cacheItemService.TotalSize + addCacheItemRequest.CacheItem.GetTotalSize() > cacheEnvironment.MaxSize)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.CacheFull;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        // Set cache item properties before saving
                        addCacheItemRequest.CacheItem.Id = Guid.NewGuid().ToString();
                        addCacheItemRequest.CacheItem.CreatedDateTime = DateTimeOffset.UtcNow;         // TODO: Sending DateTimeOffset does not serialize                    
                        addCacheItemRequest.CacheItem.CacheEnvironmentId = cacheEnvironment.Id;

                        cacheItemService.Add(addCacheItemRequest.CacheItem);
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

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed request to add {addCacheItemRequest.CacheItem.Key} to cache");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.AddCacheItemRequest;
        }
    }
}
