using System.ComponentModel;

namespace CFCacheServer.Enums
{
    public enum ResponseErrorCodes
    {
        [Description("Invalid parameters")]
        InvalidParameters,

        [Description("Permission denied")]
        PermissionDenied,

        [Description("Unknown")]
        Unknown
    }
}
