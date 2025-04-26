using CFCacheServer.Interfaces;
using CFCacheServer.Logging;
using CFCacheServer.Models;
using CFCacheServer.Server.Enums;
using CFCacheServer.Server.Models;
using CFConnectionMessaging.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Runtime.Loader;

namespace CFCacheServer.Server
{
    /// <summary>
    /// Cache server worker. Handles requests from clients
    /// </summary>
    internal class Worker : IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly List<QueueItemTask> _queueItemTasks = new();

        private Thread? _thread;

        private readonly SystemConfig _systemConfig;

        private readonly ICacheItemServiceManager _cacheItemServiceManager;

        private ISimpleLog _log;

        private TimeSpan _archiveLogsFrequency = TimeSpan.FromHours(12);
        private DateTimeOffset _lastArchiveLogs = DateTimeOffset.MinValue;

        private TimeSpan _checkCacheSizeFrequency = TimeSpan.FromMinutes(10);
        private DateTimeOffset _lastCheckCacheSize = DateTimeOffset.MinValue;        

        private ServerResources _serverResources;

        public Worker(SystemConfig systemConfig, ICacheEnvironmentService cacheEnvironmentService,
                      ICacheItemServiceManager cacheItemServiceManager, ISimpleLog log,
                      IServiceProvider serviceProvider)
        {
            _systemConfig = systemConfig;
            _cacheItemServiceManager = cacheItemServiceManager;
            _log = log;
            _serviceProvider = serviceProvider;

            _serverResources = new ServerResources()
            {
                ClientsConnection = new ClientsConnection(),
                CacheEnvironments = cacheEnvironmentService.GetAll()
            };

            // Handle message received
            _serverResources.ClientsConnection.OnMessageReceived += delegate (MessageBase message, MessageReceivedInfo messageReceivedInfo)
            {                
                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.MessageReceived,
                    Message = message,
                    MessageReceivedInfo = messageReceivedInfo
                };
                _queueItems.Enqueue(queueItem);                
            };           
        }

        public void Dispose()
        {
            _serverResources.ClientsConnection.Dispose();
        }

        private static bool IsInDockerContainer
        {
            get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
        }

        /// <summary>
        /// Starts overdue tasks, adds a queue item
        /// </summary>
        private void StartOverdueTasks()
        {         
            // Periodically archive logs
            if (_lastArchiveLogs.Add(_archiveLogsFrequency) <= DateTimeOffset.UtcNow &&
                !_queueItems.Any(i => i.ItemType == QueueItemTypes.ArchiveLogs))
            {
                _lastArchiveLogs = DateTimeOffset.UtcNow;
                _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ArchiveLogs });
            }

            Thread.Yield();

            // Periodically check cache size
            if (_lastCheckCacheSize.Add(_checkCacheSizeFrequency) <= DateTimeOffset.UtcNow &&
                !_queueItems.Any(i => i.ItemType == QueueItemTypes.CheckCacheSize))
            {
                _lastCheckCacheSize = DateTimeOffset.UtcNow;
                _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.CheckCacheSize });
            }
        }

        /// <summary>
        /// Waits for container to stop
        /// </summary>
        private void WaitForContaininerStop()
        {
            // Register event handler to detect container stopping
            var loadContext = AssemblyLoadContext.GetLoadContext(typeof(Program).Assembly);
            if (loadContext != null)
            {
                loadContext.Unloading += delegate (AssemblyLoadContext context)
                {
                    _log.Log(DateTimeOffset.UtcNow, "Information", "Detected that container is stopping");
                    _cancellationTokenSource.Cancel();
                };
            }

            // Wait until container unloads
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep(200);

                StartOverdueTasks();                              
                     
                Thread.Yield();
            }

            // Wait for thread to exit
            _thread.Join();
        }

        /// <summary>
        /// Waits for process to stop (User presses Escape)
        /// </summary>
        private void WaitForDefaultStop()
        {
            // Wait until user stops
            do
            {
                Console.WriteLine("Press ESCAPE to stop");  // Also displayed if user presses other key
                while (!Console.KeyAvailable &&
                    !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep(200);

                    StartOverdueTasks();
                    
                    Thread.Yield();
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape &&
                    !_cancellationTokenSource.Token.IsCancellationRequested);
            
            // Notify thread to exit
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            // Wait for thread to exit
            _thread.Join();
        }

        /// <summary>
        /// Starts worker thread, waits for event that triggers stop (Container stop, user presses Escape)
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        public void Run(CancellationTokenSource cancellationTokenSource)
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");            

            _cancellationTokenSource = cancellationTokenSource; 

            // Start thread
            _thread = new Thread(WorkerThread);
            _thread.Start();
     
            if (IsInDockerContainer)
            {
                WaitForContaininerStop();              
            }
            else     // Normal process
            {
                WaitForDefaultStop();
            }

            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");
        }

        /// <summary>
        /// Performs worker processing
        /// </summary>
        public void WorkerThread()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            // Listen for clients
            _serverResources.ClientsConnection.StartListening(_systemConfig.LocalPort);            

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Process queue
                    while (_queueItems.Any() &&
                        _queueItemTasks.Count < _systemConfig.MaxConcurrentTasks)
                    {
                        if (_queueItems.TryDequeue(out var queueItem))
                        {
                            ProcessQueueItem(queueItem);
                        }

                        Thread.Sleep(1);
                    }

                    Thread.Sleep(1);

                    CheckCompleteQueueItemTasks(_queueItemTasks);

                    Thread.Sleep(1);
                }
                catch(Exception exception)
                {
                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error in worker: {exception.Message}");
                    if (!cancellationToken.IsCancellationRequested) Thread.Sleep(5000);
                }
            }

            // Stop listening
            _serverResources.ClientsConnection.StopListening();
        }
       
        /// <summary>
        /// Processes queue item
        /// </summary>
        /// <param name="queueItem"></param>
        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (queueItem.ItemType == QueueItemTypes.MessageReceived && queueItem.Message != null)
            {
                var messageProcessor = _serviceProvider.GetServices<IMessageProcessor>().FirstOrDefault(p => p.CanProcess(queueItem.Message));
                if (messageProcessor != null)
                {
                    messageProcessor.SetServerResources(_serverResources);

                    _queueItemTasks.Add(new QueueItemTask(messageProcessor.ProcessAsync(queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                }    
            }
            else if (queueItem.ItemType == QueueItemTypes.ArchiveLogs)
            {
                _queueItemTasks.Add(new QueueItemTask(ArchiveLogsAsync(), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.CheckCacheSize)
            {
                _queueItemTasks.Add(new QueueItemTask(CheckCacheSizeAsync(), queueItem));
            }
        }

        private void CheckCompleteQueueItemTasks(List<QueueItemTask> queueItemTasks)
        {
            // Get completed tasks
            var completedTasks = queueItemTasks.Where(t => t.Task.IsCompleted).ToList();

            // Process completed tasks
            while (completedTasks.Any())
            {
                var queueItemTask = completedTasks.First();
                completedTasks.Remove(queueItemTask);
                queueItemTasks.Remove(queueItemTask);

                ProcessCompletedQueueItemTask(queueItemTask);
            }
        }

        private void ProcessCompletedQueueItemTask(QueueItemTask queueItemTask)
        {
            if (queueItemTask.Task.Exception == null)
            {
                //_log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ItemType}");
            }
            else
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }               

        /// <summary>
        /// Archives logs
        /// </summary>
        /// <returns></returns>
        private Task ArchiveLogsAsync()
        {
            return Task.Run(() =>
            {
                for (int days = _systemConfig.MaxLogDays; days < _systemConfig.MaxLogDays + 30; days++)
                {
                    var logDate = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(days));

                    var logFile = Path.Combine(_systemConfig.LogFolder, $"CacheServer-{logDate.ToString("yyyy-MM-dd")}.txt");
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                    }
                }                
            });
        }

        /// <summary>
        /// Check cache size, logs warning.
        /// </summary>
        /// <returns></returns>
        private Task CheckCacheSizeAsync()
        {
            return Task.Run(() =>
            {                
                foreach(var cacheEnvironment in _serverResources.CacheEnvironments.Where(e => e.MaxSize > 0 && e.PercentUsedForWarning > 0))
                {
                    // Don't need to create a scoped service because TotalSize just returns local variable
                    var totalSize = _cacheItemServiceManager.GetByCacheEnvironmentId(cacheEnvironment.Id).TotalSize;

                    // Calculate percent full
                    var percentFull = (totalSize / cacheEnvironment.MaxSize) * 100;

                    if (percentFull >= cacheEnvironment.PercentUsedForWarning)
                    {
                        _log.Log(DateTimeOffset.UtcNow, "Warning", $"Cache size is {totalSize} and max size is {cacheEnvironment.MaxSize} ({percentFull}%)");
                    }
                }              
            });
        }
    }
}
