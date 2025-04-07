using CFCacheServer.Constants;
using CFCacheServer.Exceptions;
using CFCacheServer.Models;
using CFConnectionMessaging;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer
{
    /// <summary>
    /// Cache server connection
    /// </summary>
    public class CacheServerConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        private TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);

        private List<MessageBase> _responseMessages = new();

        public CacheServerConnection()
        {
            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (IsResponseMessage(connectionMessage))
                {
                    _responseMessages.Add(GetExternalMessage(connectionMessage));
                }
                else if (OnConnectionMessageReceived!= null)
                {
                    OnConnectionMessageReceived(connectionMessage, messageReceivedInfo);
                }
            };
        }

        public MessageConverterList MessageConverterList => _messageConverterList;

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();  // Stops listening, no need to call it explicitly
            }
        }

        public void StartListening(int port)
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", $"Listening on port {port}");

            _connection.ReceivePort = port;
            _connection.StartListening();
        }

        public void StopListening()
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", "Stopping listening");
            _connection.StopListening();
        }

        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            var responseMessageTypeIds = new[]
            {
                MessageTypeIds.AddCacheItemResponse,
                MessageTypeIds.DeleteCacheItemResponse,
                MessageTypeIds.GetCacheItemResponse
            };

            return responseMessageTypeIds.Contains(connectionMessage.TypeId);
        }

        public AddCacheItemResponse SendAddCacheItemRequst(AddCacheItemRequest request, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddCacheItemRequestConverter.GetConnectionMessage(request), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(request, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (AddCacheItemResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to add cache item");
        }

        public DeleteCacheItemResponse SendDeleteCacheItemRequst(DeleteCacheItemRequest request, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.DeleteCacheItemRequestConverter.GetConnectionMessage(request), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(request, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (DeleteCacheItemResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to delete cache item");
        }

        public GetCacheItemResponse SendGetCacheItemRequst(GetCacheItemRequest request, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetCacheItemRequestConverter.GetConnectionMessage(request), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(request, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (GetCacheItemResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to get cache item");
        }

        /// <summary>
        /// Waits for all responses for request until completed or timeout. Where multiple responses are required then
        /// MessageBase.Response.IsMore=true for all except the last one.
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <param name="timeout">Timeout receiving responses</param>
        /// <param name="responseMessagesToCheck">List where responses are added</param>
        /// <param name="responseMessageAction">Action to forward next response</param>
        /// <returns>Whether all responses received</returns>
        private static bool WaitForResponses(MessageBase request, TimeSpan timeout,
                                      List<MessageBase> responseMessagesToCheck,
                                      Action<MessageBase> responseMessageAction)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var isGotAllResponses = false;
            while (!isGotAllResponses &&
                    stopwatch.Elapsed < timeout)
            {
                // Check for next response message
                var responseMessage = responseMessagesToCheck.FirstOrDefault(m => m.Response != null && m.Response.MessageId == request.Id);

                if (responseMessage != null)
                {
                    // Discard
                    responseMessagesToCheck.Remove(responseMessage);

                    // Check if last response
                    isGotAllResponses = !responseMessage.Response.IsMore;

                    // Pass response to caller
                    responseMessageAction(responseMessage);
                }

                if (!isGotAllResponses) Thread.Sleep(20);
            }

            return isGotAllResponses;
        }

        public MessageBase? GetExternalMessage(ConnectionMessage connectionMessage)
        {
            switch (connectionMessage.TypeId)
            {
                case MessageTypeIds.AddCacheItemResponse:
                    return _messageConverterList.AddCacheItemResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.DeleteCacheItemResponse:
                    return _messageConverterList.DeleteCacheItemResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.GetCacheItemResponse:
                    return _messageConverterList.GetCacheItemResponseConverter.GetExternalMessage(connectionMessage);
            }

            return null;
        }

    }
}
