using CFCacheServer.Models;
using CFConnectionMessaging.Models;

namespace CFCacheServer.Interfaces
{
    /// <summary>
    /// Processes messages received
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Configures resources
        /// </summary>
        /// <param name="serverResources"></param>
        void SetServerResources(object serverResources);

        /// <summary>
        /// Process message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo);

        /// <summary>
        /// Whether instance can process message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool CanProcess(MessageBase message);
    }
}
