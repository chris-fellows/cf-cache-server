using CFConnectionMessaging.Models;
using CFConnectionMessaging.Utilities;
using System.Net.Sockets;
using System.Net;
using static CFConnectionMessaging.ConnectionTcp;

namespace CFConnectionMessaging
{
    /// <summary>
    /// Connection for ConnectionMessage instances with transport via UDP.
    /// 
    /// Notes:
    /// - Thread listens for packets and adds them to queue.
    /// - Thread processes queue packets and deserializes them in to messages.
    /// - This class is thread safe so that multiple clients can use it simultaneously.
    /// </summary>
    public class ConnectionUdp : ConnectionSocketBase
    {        
        private List<Packet> _packets = new List<Packet>();
        private Thread? _receiveThread;
        private Thread? _packetThread;
        private bool _listening;
        private Mutex _mutex = new Mutex();

        private UdpClient _receiveClient;
        private UdpClient _sendClient;

        private CancellationTokenSource? _cancellationTokenSource;

        //// Event handler for connection messages
        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;      
            
        /// <summary>
        /// Starts listening for packets
        /// </summary>
        public void StartListening()
        {
            if (_listening)
            {
                throw new ArgumentException("Already listening");
            }
            if (ReceivePort <= 0)
            {
                throw new ArgumentException("ReceivePort must be set to a valid port");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _listening = true;

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, ReceivePort);
            
            // Configure receive & send clients. Need to be bound to same address
            _receiveClient = new UdpClient();
            _receiveClient.ExclusiveAddressUse = false;
            _receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _receiveClient.Client.Bind(endpoint);            
            //_receiveClient.Receive(ref ep1);            

            _sendClient = new UdpClient();
            _sendClient.ExclusiveAddressUse = false;
            _sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _sendClient.Client.SendBufferSize = 1024 * 500;
            //IPEndPoint ep2 = new IPEndPoint(IPAddress.Parse("X.Y.Z.W"), 1234);
            _sendClient.Client.Bind(endpoint);
            //sendClient.Send(new byte[] { ... }, sizeOfBuffer, ep2);

            _receiveThread = new Thread(ReceiveWorker);
            _receiveThread.Start();

            _packetThread = new Thread(PacketWorker);
            _packetThread.Start();            
        }

        public void StopListening()
        {
            if (_listening && _cancellationTokenSource != null)
            {
                _listening = false;
                _cancellationTokenSource.Cancel();

                // Wait for receive thread to exit
                _receiveThread!.Join();

                // Wait for packet thread to exit
                _packetThread!.Join();
            }
        }

        //public void StartTest()
        //{
        //    _thread = new Thread(Worker);
        //    _thread.Start();
        //}

        public void SendMessage(ConnectionMessage connectionMessage, EndpointInfo remoteEndpointInfo)
        {
            _mutex.WaitOne();

            // Serialize message
            var data = InternalUtilities.Serialise(connectionMessage);
                        
            /*
            var client = new UdpClient();            
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(remoteEndpointInfo.Ip), remoteEndpointInfo.Port); // endpoint where server is listening
            client.Connect(endpoint);
            client.Send(data);
            */

            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(remoteEndpointInfo.Ip), remoteEndpointInfo.Port); // endpoint where server is listening            
            _sendClient.Send(data, endpoint);

            //// Split in to packets. Increasing send buffer still causes an error            
            //var packets = InternalUtilities.SplitByteArray(data, 50000);
            //while (packets.Any())
            //{
            //    _sendClient.Send(packets[0], endpoint);
            //    packets.RemoveAt(0);
            //    Thread.Yield();
            //}
            
            _mutex.ReleaseMutex();
        }
        

        /// <summary>
        /// Worker thread to process packets received
        /// </summary>
        public void PacketWorker()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                if (_packets.Any())
                {
                    ProcessPackets();
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Worker thread to receive packets
        /// </summary>
        public void ReceiveWorker()
        {         
            //int port = 11000;
            //UdpClient udpClient = new UdpClient(ReceivePort);            

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                /*
                var remoteEP = new IPEndPoint(IPAddress.Any, ReceivePort);
                //var data = udpServer.Receive(ref remoteEP); // listen on port 11000
                //var message = Encoding.UTF8.GetString(data);
                var packet = new Packet() {
                    Data = udpClient.Receive(ref remoteEP),
                    EndpointIP = remoteEP.ToString(),
                    EndpointPort = remoteEP.Port
                };
                */

                // Receive packet
                try
                {
                    //var result = udpClient.ReceiveAsync(_cancellationTokenSource.Token).Result;
                    var result = _receiveClient.ReceiveAsync(_cancellationTokenSource.Token).Result;

                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        var packet = new Packet()
                        {                            
                            Endpoint = new EndpointInfo()
                            {
                                Ip = result.RemoteEndPoint.Address.ToString(),
                                Port = result.RemoteEndPoint.Port
                            },
                            Data = result.Buffer
                        };
                        _packets.Add(packet);

                        Console.WriteLine($"Received packet from {packet.Endpoint.Ip}:{packet.Endpoint.Port}");
                    }
                }
                catch(OperationCanceledException ocException)   // Receive cancelled
                {
                    // No action
                }

                Thread.Sleep(10);

                //Console.Write("receive data from " + remoteEP.ToString());
                //udpServer.Send(new byte[] { 1 }, 1, remoteEP); // reply back
            }
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
