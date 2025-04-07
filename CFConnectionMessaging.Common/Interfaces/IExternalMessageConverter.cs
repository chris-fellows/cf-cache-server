using CFConnectionMessaging.Models;

namespace CFConnectionMessaging.Interfaces
{
    /// <summary>
    /// Interface for converting between an external message and ConnectionMessage
    /// </summary>
    /// <typeparam name="TExternalMessage"></typeparam>
    public interface IExternalMessageConverter<TExternalMessage>
    {
        /// <summary>
        /// Returns ConnectionMessage from external message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        ConnectionMessage GetConnectionMessage(TExternalMessage message);

        /// <summary>
        /// Returns external message from ConnectionMessage
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        TExternalMessage GetExternalMessage(ConnectionMessage message);
    }
}
