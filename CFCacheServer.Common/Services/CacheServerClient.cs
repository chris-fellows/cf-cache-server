using CFCacheServer.Exceptions;
using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using CFCacheServer.Utilities;
using CFConnectionMessaging.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CFCacheServer.Services
{
    public class CacheServerClient : ICacheServerClient, IDisposable
    {
        private readonly CacheServerConnection _cacheServerConnection = new CacheServerConnection();

        private readonly EndpointInfo _remoteEndpointInfo;
        private readonly string _securityKey;

        private string _environment = String.Empty;
        
        private TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Session for receiving messages
        /// </summary>
        private class MessageReceiveSession
        {
            public string MessageId { get; internal set; }
            
            public Channel<Tuple<MessageBase, MessageReceivedInfo>> MessagesChannel = Channel.CreateBounded<Tuple<MessageBase, MessageReceivedInfo>>(50);

            public CancellationTokenSource CancellationTokenSource { get; internal set; }

            public MessageReceiveSession(string messageId, CancellationTokenSource cancellationTokenSource)
            {                
                MessageId = messageId;
                CancellationTokenSource = cancellationTokenSource;
            }
        }

        /// <summary>
        /// Active response sessions. Sending a message and waiting for response
        /// </summary>
        private Dictionary<string, MessageReceiveSession> _responseSessions = new();

        public CacheServerClient(EndpointInfo remoteEndpointInfo, int localPort, string securityKey)
        {
            _remoteEndpointInfo = remoteEndpointInfo;
            _securityKey = securityKey;          

            // Set event handler to accumulate messages received
            _cacheServerConnection.OnMessageReceived += delegate (MessageBase messageBase, MessageReceivedInfo messageReceivedInfo)
            {                
                // If response then forward to relevant session
                if (messageBase.Response != null &&_responseSessions.ContainsKey(messageBase.Response.MessageId))
                {                    
                    _responseSessions[messageBase.Response.MessageId].MessagesChannel.Writer.WriteAsync(new Tuple<MessageBase, MessageReceivedInfo>(messageBase, messageReceivedInfo));
                }                
            };            

            _cacheServerConnection.StartListening(localPort);
        }

        public void Dispose()
        {
            // Cancel any active request response, might be waiting for a response
            foreach(var session in _responseSessions.Values)
            {
                if (!session.CancellationTokenSource.IsCancellationRequested)
                {
                    session.CancellationTokenSource.Cancel();
                }
            }

            _cacheServerConnection.Dispose();
        }

        public string Environment
        {
            get { return _environment; }
            set { _environment = value; }
        }

        public async Task<bool> AddAsync<T>(string key, T value, TimeSpan expiry, bool persist)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var serializer = new CacheItemValueSerializer();

                    var request = new AddCacheItemRequest()
                    {
                        SecurityKey = _securityKey,
                        Environment = _environment,
                        CacheItem = new CacheItem()
                        {
                            Key = key,
                            Value = serializer.Serialize(value, value.GetType()),
                            ValueType = value.GetType().AssemblyQualifiedName,
                            ExpiryMilliseconds = (long)expiry.TotalMilliseconds,
                            Persist = persist
                        }
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());                        
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                        });

                        // Send request
                        _cacheServerConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);                        
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new CacheServerException("Error adding cache item", messageConnectionException);
                    }
                }

                return true;
            });            
        }    

        public async Task<bool> DeleteAllAsync()
        {
            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new DeleteCacheItemRequest()
                    {
                        SecurityKey = _securityKey,
                        Environment = _environment,
                        ItemKey = ""
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                        });

                        // Send request
                        _cacheServerConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);
                        
                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new CacheServerException("Error deleting all cache items", messageConnectionException);
                    }
                }

                return true;
            });
        }        

        public async Task<bool> DeleteAsync(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new DeleteCacheItemRequest()
                    {
                        SecurityKey = _securityKey,
                        Environment = _environment,
                        ItemKey = key
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                        });

                        // Send request
                        _cacheServerConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new CacheServerException("Error deleting cache item", messageConnectionException);
                    }
                }

                return true;
            });
        }

        public async Task<T?> GetByKeyAsync<T>(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetCacheItemRequest()
                    {
                        SecurityKey = _securityKey,
                        Environment = _environment,
                        ItemKey = key
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                        });

                        // Send request
                        _cacheServerConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                  
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetCacheItemResponse)responseMessages.First();

                        if (response.CacheItem == null)
                        {
                            return default(T?);
                        }
                        else
                        {
                            var serializer = new CacheItemValueSerializer();
                            return (T?)serializer.Deserialize(response.CacheItem.Value, Type.GetType(response.CacheItem.ValueType));
                        }
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new CacheServerException("Error adding cache item", messageConnectionException);
                    }
                }
            });
        }

        public async Task<List<string>> GetKeysByFilterAsync(CacheItemFilter filter)
        {
            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetCacheItemKeysRequest()
                    {
                        SecurityKey = _securityKey,
                        Filter = filter
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                        });

                        _cacheServerConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetCacheItemKeysResponse)responseMessages.First();

                        return response.ItemKeys;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new CacheServerException("Error adding cache item", messageConnectionException);
                    }
                }
            });
        }

        /// <summary>
        /// Checks connection message response and throws an exception if an error
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="MessageConnectionException"></exception>
        private static void ThrowResponseExceptionIfRequired(MessageBase? message)
        {
            if (message == null)
            {
                throw new MessageConnectionException("Timeout receiving response");
            }

            if (message.Response != null && message.Response.ErrorCode != null)
            {
                throw new MessageConnectionException(message.Response.ErrorMessage);
            }
        }

        /// <summary>
        /// Waits for all response messages or timeout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="messageReceiveSession"></param>
        /// <param name="timeout"></param>
        /// <returns>All responses received or empty if last response not received</returns>
        private static async Task<List<MessageBase>> WaitForResponsesAsync(MessageBase request, MessageReceiveSession messageReceiveSession)
        {
            var cancellationToken = messageReceiveSession.CancellationTokenSource.Token;

            var reader = messageReceiveSession.MessagesChannel.Reader;

            var isGotAllResponses = false;
            var responseMessages = new List<MessageBase>();
            while (!isGotAllResponses &&
                !cancellationToken.IsCancellationRequested)
            {
                // Wait for message received or timeout
                var response = await reader.ReadAsync(cancellationToken);                

                if (response != null && response.Item1 != null)
                {
                    var responseMessage = response.Item1;
                    if (responseMessage.Response.MessageId == request.Id)   // Sanity check
                    {
                        responseMessages.Add(responseMessage);

                        // Check if last response
                        isGotAllResponses = !responseMessage.Response.IsMore;
                    }
                }
            }

            return isGotAllResponses ? responseMessages : new();
        }
    }
}
