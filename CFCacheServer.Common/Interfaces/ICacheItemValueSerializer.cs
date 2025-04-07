namespace CFCacheServer.Interfaces
{
    public interface ICacheItemValueSerializer
    {
        byte[] Serialize(object entity, Type entityType);

        object Deserialize(byte[] content, Type entityType);
    }
}
