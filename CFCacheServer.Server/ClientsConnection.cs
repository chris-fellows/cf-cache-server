using CFCacheServer.Models;
using CFConnectionMessaging.Models;
using CFConnectionMessaging;

namespace CFCacheServer.Server
{
    /// <summary>
    /// Connections for clients
    /// </summary>
    internal class ClientsConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        public delegate void MessageReceived(MessageBase message, MessageReceivedInfo messageReceivedInfo);
        public event MessageReceived? OnMessageReceived;

        //private TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);

        //private List<MessageBase> _responseMessages = new();

        public ClientsConnection()
        {
            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (OnMessageReceived != null)
                {
                    OnMessageReceived(_messageConverterList.GetExternalMessage(connectionMessage), messageReceivedInfo);
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

        /// <summary>
        /// Send message to remote client
        /// </summary>
        /// <param name="externalMessage"></param>
        /// <param name="remoteEndpointInfo"></param>
        public void SendMessage(MessageBase externalMessage, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetConnectionMessage(externalMessage), remoteEndpointInfo);
        }
    }
}
