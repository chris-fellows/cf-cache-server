using CFCacheServer.Enums;
using CFCacheServer.Exceptions;
using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Services
{
    public class CacheServerClient : ICacheServerClient, IDisposable
    {
        private readonly CacheServerConnection _cacheServerConnection = new CacheServerConnection();

        private readonly EndpointInfo _remoteEndpointInfo;
        private readonly string _securityKey;

        public CacheServerClient(EndpointInfo remoteEndpointInfo, int localPort, string securityKey)
        {
            _remoteEndpointInfo = remoteEndpointInfo;
            _securityKey = securityKey;

            _cacheServerConnection.StartListening(localPort);
        }

        public void Dispose()
        {
            _cacheServerConnection.Dispose();
        }

        public Task AddAsync<T>(string key, T value, TimeSpan expiry)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var serializer = new CacheItemValueSerializer();
            
            var request = new AddCacheItemRequest()
            {
                SecurityKey = _securityKey,
                CacheItem = new CacheItem()
                {
                    Key = key,
                    Value = serializer.Serialize(value, value.GetType()),
                    ValueType = value.GetType().AssemblyQualifiedName,
                    ExpiryMilliseconds = (long)expiry.TotalMilliseconds
                }
            };                        

            try
            {
                var response = _cacheServerConnection.SendAddCacheItemRequst(request, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new CacheServerException("Error adding cache item", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAllAsync()
        {
            var request = new DeleteCacheItemRequest()
            {
                SecurityKey = _securityKey,
                ItemKey = ""
            };

            try
            {
                var response = _cacheServerConnection.SendDeleteCacheItemRequst(request, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new CacheServerException("Error deleting all cache items", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var request = new DeleteCacheItemRequest()
            {
                SecurityKey = _securityKey,
                ItemKey = key
            };

            try
            {
                var response = _cacheServerConnection.SendDeleteCacheItemRequst(request, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new CacheServerException("Error deleting cache item", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task<T?> GetByKeyAsync<T>(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var request = new GetCacheItemRequest()
            {
                SecurityKey = _securityKey,
                ItemKey = key                
            };
            
            try
            {
                var response = _cacheServerConnection.SendGetCacheItemRequst(request, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                if (response.CacheItem == null)
                {
                    return null;
                }
                else
                {
                    var serializer = new CacheItemValueSerializer();
                    return Task.FromResult((T)serializer.Deserialize(response.CacheItem.Value, Type.GetType(response.CacheItem.ValueType)));
                }                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new CacheServerException("Error adding cache item", messageConnectionException);
            }            
        }

        public Task<List<string>> GetKeysByFilterAsync(CacheItemFilter filter)
        {           
            var request = new GetCacheItemKeysRequest()
            {
                SecurityKey = _securityKey,
                Filter = filter
            };

            try
            {
                var response = _cacheServerConnection.SendGetCacheItemKeysRequst(request, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.ItemKeys);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new CacheServerException("Error adding cache item", messageConnectionException);
            }
        }

        /// <summary>
        /// Checks connection message response and throws an exception if an error
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="MessageConnectionException"></exception>
        private void ThrowResponseExceptionIfRequired(MessageBase message)
        {
            if (message.Response != null && message.Response.ErrorCode != null)
            {
                throw new MessageConnectionException(message.Response.ErrorMessage);
            }
        }
    }
}
