using CFConnectionMessaging.Models;
using CFConnectionMessaging.Utilities;

namespace CFConnectionMessaging
{
    /// <summary>
    /// Base class for socket based connection (TCP, UDP)
    /// </summary>
    public abstract class ConnectionSocketBase
    {
        protected List<Packet> _packets = new List<Packet>();

        protected int _receivePort = 11000;       // Default

        public int ReceivePort
        {
            get { return _receivePort; }
            set { _receivePort = value; }
        }

        /// <summary>
        /// Process packets. Converts to ConnectionMessage, removes used packets, notifies client.       
        /// </summary>
        protected void ProcessPackets()
        {
            if (_packets.Any())
            {
                // Get all distinct endpoints
                var endpoints = _packets.Select(p => $"{p.Endpoint.Ip}\t{p.Endpoint.Port}").Distinct().ToList();

                // Process packets for each endpoint
                foreach (var endpoint in endpoints)
                {
                    ProcessPackets(endpoint.Split('\t')[0], Convert.ToInt32(endpoint.Split('\t')[1]));
                }
            }
        }

        /// <summary>
        /// Process packets for endpoint
        /// </summary>
        private void ProcessPackets(string endpointIP, int endpointPort)
        {
            // Get all packets for client related to first packet
            var packetsForEndpoint = _packets.Where(p => p.Endpoint.Ip == endpointIP && p.Endpoint.Port == endpointPort).ToList();

            // Try and get message header
            var messageHeader = InternalUtilities.GetMessageHeader(packetsForEndpoint.First());
            if (messageHeader != null)   // Header read from first packet
            {
                // Try and get connection message
                var connectionMessage = GetConnectionMessage(messageHeader, packetsForEndpoint);
                if (connectionMessage != null)
                {
                    // Remove all fully used packets
                    _packets.RemoveAll(packet => packetsForEndpoint.Where(p => p.Data.Length == 0).Contains(packet));

                    // Notify
                    ConnectMessageReceived(connectionMessage, new MessageReceivedInfo()
                    {
                        ReceivedTime = DateTimeOffset.UtcNow,    // Doesn't really need to be packet receive time                        
                        RemoteEndpointInfo = new EndpointInfo()
                        {
                            Ip = endpointIP,
                            Port = endpointPort
                        }                        
                    });
                }
            }
        }

        /// <summary>
        /// ConnectionMessage received. Derived class will override this and raise OnConnectionMessageReceived event
        /// </summary>
        /// <param name="connectionMessage"></param>
        /// <param name="messageReceivedInfo"></param>
        protected virtual void ConnectMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
        {

        }

        //public void AddPacketForTesting(Packet packet)
        //{
        //    _packets.Add(packet);
        //}

        /// <summary>
        /// Gets first ConnectionMessage if sufficient packets received. Packet.Data will be updated to remove any data
        /// used. We need to consider that we may have a packet with the end of message #1 and the start of message #2
        /// if the client is sending messages in quick succession.
        /// </summary>
        /// <param name="messageHeader"></param>
        /// <param name="packetsForEndpoint"></param>
        /// <returns>ConnectionMessage if sufficient packets received</returns>
        private static ConnectionMessage? GetConnectionMessage(MessageHeader messageHeader, List<Packet> packetsForEndpoint)
        {
            // Get total of all packets
            int totalPacketBytes = packetsForEndpoint.Sum(p => p.Data.Length);

            // Check that we have sufficient data
            if (totalPacketBytes >= messageHeader.PayloadLength)    // Sufficient data packets for message
            {
                // Set array of payload to create from each packet
                byte[] payloadData = new byte[messageHeader.PayloadLength];
                int payloadOffset = 0;

                // Concatenate packet payloads
                var packet = packetsForEndpoint.First();
                int packetIndex = -1;
                do
                {
                    packetIndex++;

                    System.Diagnostics.Debug.WriteLine($"{packetIndex} Packet.Data.Length={packet.Data.Length}");

                    // Calculate bytes remaining to copy. We may get a situation where a packet contains multiple ConnectionMessage
                    // instances and so we only need the first part of Packet.Data
                    var bytesRemainingToCopy = messageHeader.PayloadLength - payloadOffset;

                    // Determine position in Packet.Data to copy from to payloadData
                    var sourceOffset = packet == packetsForEndpoint.First() ?
                                            messageHeader.HeaderLength :   // For 1st packet then copy from after header
                                            0;                  // For 2nd+ packet then no header, so copy from start

                    // Determine how many packet bytes to copy to payloadData
                    var packetBytesToCopy = packet.Data.Length - sourceOffset;  // Default to all
                    var newData = new byte[0];      // Default to using all data
                    if (packetBytesToCopy > bytesRemainingToCopy)   // Only need some of the packet
                    {
                        packetBytesToCopy = bytesRemainingToCopy;

                        // Remove used data
                        newData = new byte[packet.Data.Length - packetBytesToCopy];
                        Buffer.BlockCopy(packet.Data, packetBytesToCopy, newData, 0, packet.Data.Length - packetBytesToCopy);                        
                    }

                    // Copy from Packet.Data to payloadData
                    Buffer.BlockCopy(packet.Data, sourceOffset, payloadData, payloadOffset, packetBytesToCopy);

                    // Set remaining packet data (if any). We may have a packet with the end of message #1 and the start of
                    // message #2
                    packet.Data = newData;

                    // Move payload offset for next packet to copy in
                    payloadOffset += packetBytesToCopy;

                    System.Diagnostics.Debug.WriteLine($"{packetIndex} bytesRemainingToCopy={bytesRemainingToCopy}, sourceOffset={sourceOffset}, packetBytesToCopy={packetBytesToCopy}, payloadOffset={payloadOffset}");

                    // Set next packet
                    packet = packet != packetsForEndpoint.Last() ? packetsForEndpoint[packetsForEndpoint.IndexOf(packet) + 1] : null;

                } while (payloadOffset < messageHeader.PayloadLength);

                // Deserialize payload
                var connectionMessage = InternalUtilities.DeserialiseToConnectionMessage(payloadData);
                return connectionMessage;
            }

            return null;
        }
    }
}
