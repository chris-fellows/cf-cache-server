using CFConnectionMessaging.Exceptions;
using CFConnectionMessaging.Models;
using CFConnectionMessaging.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;

namespace CFConnectionMessaging
{
    /// <summary>
    ///  Connection for ConnectionMessage instances with transport via TCP.
    /// </summary>
    public class ConnectionTcp : ConnectionSocketBase, IDisposable
    {
        private Mutex _mutex = new Mutex();        
        private Thread? _listenerThread;
        private Thread? _receiveThread;

        private int _listenPort;

        private CancellationTokenSource? _cancellationTokenSource;        

        private class ClientInfo
        {            
            public TcpClient? TcpClient { get; set; }

            public NetworkStream? Stream { get; set; }
         
            public EndpointInfo EndpointInfo { get; set; }

            /// <summary>
            /// Time message last sent or received. May be used for closing connections that have been inactive
            /// after a timeout
            /// </summary>
            public DateTimeOffset? LastMessageTime { get; set; }
        }

        private List<ClientInfo> _clientInfos = new List<ClientInfo>();    

        // Event for connection message received
        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        /// <summary>
        /// Event for client connected
        /// </summary>
        /// <param name="endpointInfo"></param>
        public delegate void ClientConnected(EndpointInfo endpointInfo);
        public event ClientConnected? OnClientConnected;

        /// <summary>
        /// Event for client disconnected
        /// </summary>
        /// <param name="endpointInfo"></param>
        public delegate void ClientDisconnected(EndpointInfo endpointInfo);
        public event ClientDisconnected? OnClientDisconnected;
    
        public void Dispose()
        {
            StopListening();

            // Clean up clients
            while(_clientInfos.Any())
            {
                _clientInfos[0].TcpClient?.Close();
                _clientInfos[0].TcpClient?.Dispose();
                _clientInfos.RemoveAt(0);
            }            
        }

        /// <summary>
        /// Endpoints for clients
        /// </summary>
        public List<EndpointInfo> ClientRemoteEndpoints
        {
            get
            {                
                var endpoints = _clientInfos.Select(clientInfo =>
                {
                    IPEndPoint remoteEndpoint = clientInfo.TcpClient.Client.RemoteEndPoint as IPEndPoint;
                    return new EndpointInfo()
                    {
                        Ip = remoteEndpoint.Address.ToString(),
                        Port = remoteEndpoint.Port
                    };
                }).ToList();

                return endpoints;
            }
        }

        public bool IsListening => _listenerThread != null;

        public void StartListening()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Start listener thread
            _listenPort = ReceivePort;
            _listenerThread = new Thread(ListenerWorker);
            _listenerThread.Start();

            // Start receive thread
            _receiveThread = new Thread(ReceiveWorker);
            _receiveThread.Start();
        }

        public void StopListening()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }

            // Wait for listening thread to exit
            if (_listenerThread != null)
            {
                _listenerThread.Join();
                _listenerThread = null;
            }

            // Wait for receive thread to exist
            if (_receiveThread != null)
            {
                _receiveThread.Join();
                _receiveThread = null;
            }
        }

        /// <summary>
        /// Worker thread that listens for connection requests
        /// </summary>
        public void ListenerWorker()
        {
            // Start listening
            var tcpLisener = new TcpListener(System.Net.IPAddress.Any, _listenPort);
            tcpLisener.Start();

            // Run until cancelled
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Accept TCP client
                    var tcpClient = AcceptTcpClientAsync(tcpLisener).Result;
                    if (tcpClient != null)
                    {                        
                        var clientInfo = new ClientInfo()
                        {
                            LastMessageTime = DateTimeOffset.UtcNow,
                            TcpClient = tcpClient,
                            Stream = tcpClient.GetStream(),                            
                        };
                        _clientInfos.Add(clientInfo);

                        IPEndPoint remoteEndpoint = clientInfo.TcpClient.Client.RemoteEndPoint as IPEndPoint;
                        Console.WriteLine($"Client {remoteEndpoint.Address.ToString()}:{remoteEndpoint.Port} connected");

                        // Notify connected
                        if (OnClientConnected != null)
                        {
                            OnClientConnected(new EndpointInfo() { Ip = remoteEndpoint.Address.ToString(), Port = remoteEndpoint.Port });
                        }
                    }
                }
                catch(AggregateException aggregateException)
                {
                    if (aggregateException.InnerException != null &&
                        aggregateException.InnerException is TaskCanceledException)
                    {
                        // No action
                    }
                    else
                    {
                        throw;
                    }
                }                
                catch(SocketException socketException)
                {
                    System.Diagnostics.Debug.WriteLine($"Error accepting TCP client: {socketException.Message}");
                    Thread.Sleep(500);
                }

                System.Threading.Thread.Yield();
            }

            tcpLisener.Stop();
        }

        private async Task<TcpClient?> AcceptTcpClientAsync(TcpListener tcpListener)
        {
            return await tcpListener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
        }

        /// <summary>
        /// Checks if client is connection. TcpClient.Connected doesn't report the current state, only last state
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        private bool IsClientConnected(TcpClient tcpClient)
        {
            if (tcpClient.Connected)
            {
                var connected = !(tcpClient.Client.Poll(1, SelectMode.SelectRead) && tcpClient.Client.Available == 0);
                return connected;
            }

            /*
            if (tcpClient.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] data = new byte[1];
                if (tcpClient.Client.Receive(data, SocketFlags.Peek) == 0)
                {
                    return false;
                }            
                else
                {
                    int xxx = 1000;
                }
            }
            */

            return false;
        }

        /// <summary>
        /// Checks for disconnected clients
        /// </summary>
        private void CheckClientsDisconnected()
        {
            // Get disconnected clients
            var clientInfos = _clientInfos.Where(ci => !IsClientConnected(ci.TcpClient)).ToList();

            // Clean up disconnected clients
            while(clientInfos.Any())
            {
                var clientInfo = clientInfos[0];
                IPEndPoint remoteEndpoint = clientInfo.TcpClient.Client.RemoteEndPoint as IPEndPoint;

                clientInfo.Stream.Close();
                clientInfo.TcpClient.Close();                

                // Notify disconnected
                if (OnClientDisconnected != null)
                {
                    OnClientDisconnected(new EndpointInfo()
                    {
                        Ip = remoteEndpoint.Address.ToString(),
                        Port = remoteEndpoint.Port
                    });
                }

                clientInfos.Remove(clientInfo);
                _clientInfos.Remove(clientInfo);
            }
        }

        /// <summary>
        /// Worker thread that receives packets of data        
        /// </summary>
        public void ReceiveWorker()
        {
            var receiveTasks = new List<Task>();

            // Run until cancelled
            var lastCheckClients = DateTimeOffset.UtcNow;
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Receive data from clients
                var clientInfos = _clientInfos.Where(ci => ci.Stream.DataAvailable).ToList();
                foreach (var clientInfo in clientInfos)
                {
                    var receiveTask = ReceiveAsync(clientInfo);
                    receiveTasks.Add(receiveTask);
                }
                System.Threading.Thread.Sleep(2);

                // Wait for receive complete
                if (receiveTasks.Any())
                {
                    Task.WaitAll(receiveTasks.ToArray());
                    receiveTasks.Clear();                    
                }
                System.Threading.Thread.Sleep(2);

                // Process packets
                if (_packets.Any())
                {
                    ProcessPackets();
                }

                // Check clients
                if (lastCheckClients.AddSeconds(5) <= DateTimeOffset.UtcNow)    // Was 30
                {
                    lastCheckClients = DateTimeOffset.UtcNow;
                    CheckClientsDisconnected();
                }

                System.Threading.Thread.Sleep(2);
            }
        }       

        /// <summary>
        /// Receives from client asynchronously
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <returns></returns>
        private Task ReceiveAsync(ClientInfo clientInfo)
        {
            return Task.Factory.StartNew(() =>
            {
                var data = new byte[1024 * 50];     // Use same array for all packets. No need to reset between packets
                while (clientInfo.Stream.DataAvailable)
                {                    
                    var byteCount = clientInfo.Stream.Read(data, 0, data.Length);
                    if (byteCount > 0)
                    {
                        // Add packet to queue
                        IPEndPoint remoteEndpoint = clientInfo.TcpClient.Client.RemoteEndPoint as IPEndPoint;
                        var packet = new Packet()
                        {
                            Endpoint = new EndpointInfo()
                            {
                                Ip = remoteEndpoint.Address.ToString(),
                                Port = remoteEndpoint.Port = remoteEndpoint.Port
                            },
                            Data = new byte[byteCount]
                        };
                        Buffer.BlockCopy(data, 0, packet.Data, 0, byteCount);

                        _mutex.WaitOne();
                        _packets.Add(packet);
                        _mutex.ReleaseMutex();

                        //System.Diagnostics.Debug.WriteLine($"Packet received from {packet.Endpoint.Ip}:{packet.Endpoint.Port} ({packet.Data.Length} bytes)");
                    }
                    Thread.Sleep(5);

                    clientInfo.LastMessageTime = DateTimeOffset.UtcNow;
                }
            });
        }

        /// <summary>
        /// Sends message
        /// </summary>
        /// <param name="connectionMessage"></param>
        /// <param name="remoteEndpointInfo"></param>
        public void SendMessage(ConnectionMessage connectionMessage, EndpointInfo remoteEndpointInfo)
        {
            // Serialize message
            var data = InternalUtilities.Serialise(connectionMessage);

            // Get ClientInfo for remote endpoint
            var clientInfo = GetClientInfoByRemoteEndpoint(remoteEndpointInfo);

            // If no connection then connect
            if (clientInfo == null)
            {
                try
                {
                    clientInfo = ConnectToClient(remoteEndpointInfo);
                }
                catch (Exception exception)
                {
                    throw new ConnectionException($"Error connecting to {remoteEndpointInfo.Ip}:{remoteEndpointInfo.Port}", exception);
                }
            }

            //Console.WriteLine($"Sending data for {connectionMessage.TypeId} to {remoteEndpointInfo.Ip}:{remoteEndpointInfo.Port}");

            // Send data
            clientInfo.TcpClient.Client.Send(data);
            clientInfo.LastMessageTime = DateTimeOffset.UtcNow;

            //Console.WriteLine($"Sent data to {remoteEndpointInfo.Ip}:{remoteEndpointInfo.Port}");
        }

        /// <summary>
        /// Connects to client
        /// </summary>
        /// <param name="remoteEndpointInfo"></param>
        /// <returns></returns>
        private ClientInfo ConnectToClient(EndpointInfo remoteEndpointInfo)
        {
            //Console.WriteLine($"Connecting to {remoteEndpointInfo.Ip}:{remoteEndpointInfo.Port}");
            var tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse(remoteEndpointInfo.Ip), remoteEndpointInfo.Port);
            //Console.WriteLine($"Connected to {remoteEndpointInfo.Ip}:{remoteEndpointInfo.Port}");

            var clientInfo = new ClientInfo()
            {                
                TcpClient = tcpClient,
                Stream = tcpClient.GetStream()
            };
            _clientInfos.Add(clientInfo);

            // Notify client connected 
            if (OnClientConnected != null)
            {
                OnClientConnected(new EndpointInfo()
                {
                    Ip = remoteEndpointInfo.Ip,
                    Port = remoteEndpointInfo.Port
                });
            }

            return clientInfo;
        }

        /// <summary>
        /// Gets ClientInfo by remote endpoint
        /// </summary>
        /// <param name="endpointInfo"></param>
        /// <returns></returns>
        private ClientInfo? GetClientInfoByRemoteEndpoint(EndpointInfo endpointInfo)
        {                        
            // Map to IPv6 address
            var address = IPAddress.Parse($"{endpointInfo.Ip}").MapToIPv6();            

            // Check each client
            foreach (var clientInfo in _clientInfos)
            {
                IPEndPoint clientRemoteEndpoint = clientInfo.TcpClient.Client.RemoteEndPoint as IPEndPoint;
                IPAddress clientAddress = clientRemoteEndpoint.Address.MapToIPv6();

                if (clientAddress.ToString().Equals(address.ToString()) && 
                    clientRemoteEndpoint.Port == endpointInfo.Port)
                {
                    return clientInfo;
                }
            }

            return null;
        }

        protected override void ConnectMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
        {
            if (OnConnectionMessageReceived != null)
            {
                OnConnectionMessageReceived(connectionMessage, messageReceivedInfo);
            }
        }    
    }
}
