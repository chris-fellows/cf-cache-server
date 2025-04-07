using CFConnectionMessaging.Models;
using CFConnectionMessaging.Utilities;

namespace CFConnectionMessaging
{
    public class ConnectionTest
    {
        public void TestByteArraySplit()
        {
            var data = new byte[41];
            for (int index = 0; index < data.Length; index++)
            {
                data[index] = (byte)index;
            }

            var sections = InternalUtilities.SplitByteArray(data, 20);
            int x = 1000;
        }

        /// <summary>
        /// Tests sending one message that's received in a single packet
        /// </summary>
        public void TestOneMessageInSinglePacket()
        {
            // Start connection
            var connection = new ConnectionUdp();
            //connection.StartTest();

            var message1 = new ConnectionMessage()
            {
                Id = Guid.NewGuid().ToString(),
                TypeId = "Test1",
                Parameters = new List<ConnectionMessageParameter>()
                {
                    new ConnectionMessageParameter() { Name = "P1", Value = "10" },
                    new ConnectionMessageParameter() { Name = "P1", Value = "20" }
                }
            };

            // Serialize message
            var data = InternalUtilities.Serialise(message1);

            // Create packet
            var packet = new Packet()
            {
                Data = data
            };

            // Pass to connection
            //connection.AddPacketForTesting(packet);

            int xxx = 1000;
        }

        /// <summary>
        /// Test sending one message that's received in multiple packets
        /// </summary>
        public void TestOnMessageInMultiplePackets()
        {
            // Start connection
            var connection = new ConnectionUdp();
            //connection.StartTest();

            // Set message to send
            var message1 = new ConnectionMessage()
            {
                Id = Guid.NewGuid().ToString(),
                TypeId = "Test1",
                Parameters = new List<ConnectionMessageParameter>()
                    {
                        new ConnectionMessageParameter() { Name = "P1", Value = "10" },
                        new ConnectionMessageParameter() { Name = "P1", Value = "20" }
                    }
            };
              
            // Serialize message
            var data = InternalUtilities.Serialise(message1);

            // Split data in to packets
            var dataPackets = InternalUtilities.SplitByteArray(data, 150);
            //var dataPackets = InternalUtilities.SplitByteArray(data, 20);

            // Send packets to connection
            foreach (var dataPacket in dataPackets)
            {
                // Create packet
                var packet = new Packet()
                {
                    Data = dataPacket
                };

                // Pass to connection
                //connection.AddPacketForTesting(packet);

                // Simulate delay to test that connection waits until all packets received
                Thread.Sleep(1000);
            }
            
            int xxx = 1000;
        }

        /// <summary>
        /// Test sending multiple messages in multiple packets
        /// </summary>
        public void TestMultipleMessagesInMultiplePackets()
        {
            // Start connection
            var connection = new ConnectionUdp();
            connection.OnConnectionMessageReceived += (connectionMessage, messageReceivedInfo) =>
            {
                System.Diagnostics.Debug.WriteLine($"Received connection message: ID={connectionMessage.Id}");
            };
            //connection.StartTest();

            // Set message to send
            var messages = new List<ConnectionMessage>()
            {
                new ConnectionMessage()
                {
                    Id = Guid.NewGuid().ToString(),
                    TypeId = "Test1",
                    Parameters = new List<ConnectionMessageParameter>()
                        {
                            new ConnectionMessageParameter() { Name = "P1", Value = "10" },
                            new ConnectionMessageParameter() { Name = "P1", Value = "20" }
                        }
                },
                new ConnectionMessage()
                {
                    Id = Guid.NewGuid().ToString(),
                    TypeId = "Test1",
                    Parameters = new List<ConnectionMessageParameter>()
                        {
                            new ConnectionMessageParameter() { Name = "P1", Value = "10" },
                            new ConnectionMessageParameter() { Name = "P1", Value = "20" }
                        }
                }
            };

            // Seraialise messages in to single byte array
            var data = InternalUtilities.Serialise(messages[0]).Concat(InternalUtilities.Serialise(messages[1])).ToArray();
   
            // Split data in to packets. One packet will contain the end of message #1 and the start of message #2
            var dataPackets = InternalUtilities.SplitByteArray(data, 150);

            // Send packets to connection
            foreach (var dataPacket in dataPackets)
            {
                // Create packet
                var packet = new Packet()
                {
                    Data = dataPacket
                };

                // Pass to connection
                //connection.AddPacketForTesting(packet);

                // Simulate delay to test that connection waits until all packets received
                Thread.Sleep(1000);
            }

            int xxx = 1000;
        }
    }
}
