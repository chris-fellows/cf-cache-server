using CFConnectionMessaging.Models;
using CFConnectionMessaging.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CFConnectionMessaging.Utilities
{
    internal static class InternalUtilities
    {       
        public static byte[] Serialise(ConnectionMessage connectionMessage)
        {            
            // Serialize to JSON and to byte array
            var serialized = Encoding.UTF8.GetBytes(JsonUtilities.SerializeToString(connectionMessage, JsonUtilities.DefaultJsonSerializerOptions));

            // Return header + body
            var messageHeader = new MessageHeader()
            {
                PayloadLength = serialized.Length
            };            
            return Serialise(messageHeader).Concat(serialized).ToArray();
        }

        public static ConnectionMessage DeserialiseToConnectionMessage(byte[] data)
        {
            var connectionMessage = JsonUtilities.DeserializeFromString<ConnectionMessage>(Encoding.UTF8.GetString(data), JsonUtilities.DefaultJsonSerializerOptions);
            return connectionMessage;
        }

        private static byte[] Serialise(MessageHeader messageHeader)
        {
            var header = BitConverter.GetBytes(messageHeader.PayloadLength);
            return header;
        }
       
        public static MessageHeader? GetMessageHeader(Packet packet)
        {
            int lengthBytes = sizeof(Int32);
            if (packet.Data.Length > lengthBytes)
            {                
                return new MessageHeader()
                {
                    PayloadLength = BitConverter.ToInt32(packet.Data, 0)
                };                
            }

            return null;    // Insufficient data
        }

        /// <summary>
        /// Splits byte array in to sections of max length or less
        /// </summary>
        /// <param name="data"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static List<byte[]> SplitByteArray(byte[] data, int maxLength)
        {
            var sections = new List<byte[]>();
            int startPos = 0;
            int endPos = 0;
            do
            {
                // Set end position for section
                endPos = startPos + maxLength;
                if (endPos > data.Length) endPos = data.Length;

                byte[] section = new byte[endPos - startPos];
                Buffer.BlockCopy(data, startPos, section, 0, endPos - startPos);

                // Set next start position
                startPos = endPos;

                sections.Add(section);
            } while (endPos < data.Length);

            return sections;
        }
    }
}
