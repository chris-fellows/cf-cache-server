namespace CFCacheServer.Server.Models
{
    public class QueueItemTask
    {
        public Task Task { get; internal set; }

        public QueueItem QueueItem { get; internal set; }

        public QueueItemTask(Task task, QueueItem queueItem)
        {
            Task = task;
            QueueItem = queueItem;
        }
    }
}
