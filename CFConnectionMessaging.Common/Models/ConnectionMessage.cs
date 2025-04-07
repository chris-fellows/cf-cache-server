
namespace CFConnectionMessaging.Models
{
    /// <summary>
    /// Connection message
    /// </summary>
    public class ConnectionMessage
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Message Type Id
        /// </summary>
        public string TypeId { get; set; } = String.Empty;

        /// <summary>
        /// Parameters
        /// </summary>
        public List<ConnectionMessageParameter> Parameters { get; set; } = new List<ConnectionMessageParameter>();
    }
}
