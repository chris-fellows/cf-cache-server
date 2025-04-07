using CFCacheServer.Utilities;
using System.Text;

namespace CFCacheServer.Services
{
    public class CacheItemValueSerializer
    {
        public byte[] Serialize(object entity, Type entityType)
        {
            return Encoding.UTF8.GetBytes(JsonUtilities.SerializeToString(entity, entityType, JsonUtilities.DefaultJsonSerializerOptions));
        }

        public object Deserialize(byte[] content, Type entityType)
        {
            return JsonUtilities.DeserializeFromString(Encoding.UTF8.GetString(content), entityType, JsonUtilities.DefaultJsonSerializerOptions);
        }
    }
}
