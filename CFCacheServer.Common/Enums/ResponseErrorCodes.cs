using System.ComponentModel;

namespace CFCacheServer.Enums
{
    public enum ResponseErrorCodes
    {
        [Description("Cache environment not found")]
        CacheEnvironmentNotFound,

        [Description("Cache full")]
        CacheFull,        

        [Description("Invalid parameters")]
        InvalidParameters,

        [Description("Permission denied")]
        PermissionDenied,

        [Description("Unknown")]
        Unknown
    }
}
