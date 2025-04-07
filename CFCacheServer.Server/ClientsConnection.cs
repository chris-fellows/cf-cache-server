using CFCacheServer.Models;
using CFConnectionMessaging.Models;
using CFConnectionMessaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFCacheServer.Server
{
    /// <summary>
    /// Connections for clients
    /// </summary>
    internal class ClientsConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        private TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);

        private List<MessageBase> _responseMessages = new();

        public ClientsConnection()
        {
            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (OnConnectionMessageReceived != null)
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

        public void SendAddCacheItemResponse(AddCacheItemResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddCacheItemResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendGetCacheItemResponse(GetCacheItemResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetCacheItemResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendGetCacheItemKeysResponse(GetCacheItemKeysResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetCacheItemKeysResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendDeleteCacheItemResponse(DeleteCacheItemResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.DeleteCacheItemResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }
    }
}
