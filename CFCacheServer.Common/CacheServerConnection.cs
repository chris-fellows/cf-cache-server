using CFCacheServer.Models;
using CFConnectionMessaging;
using CFConnectionMessaging.Models;

namespace CFCacheServer
{
    /// <summary>
    /// Cache server connection
    /// </summary>
    public class CacheServerConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        public delegate void MessageReceived(MessageBase messageBase, MessageReceivedInfo messageReceivedInfo);
        public event MessageReceived? OnMessageReceived;
        
        public CacheServerConnection()
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

        public void SendMessage(MessageBase externalMessage, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetConnectionMessage(externalMessage), remoteEndpointInfo);
        }     
    }
}
