using CFCacheServer.Constants;
using CFCacheServer.Enums;
using CFCacheServer.Interfaces;
using CFCacheServer.Logging;
using CFCacheServer.Models;
using CFCacheServer.Server.Enums;
using CFCacheServer.Server.Models;
using CFCacheServer.Utilities;
using CFConnectionMessaging.Models;
using System;
using System.Collections.Concurrent;
using System.Runtime.Loader;
using System.Threading;

namespace CFCacheServer.Server
{
    /// <summary>
    /// Cache server worker. Handles requests from clients
    /// </summary>
    internal class Worker : IDisposable
    {
        private CancellationTokenSource? _cancellationTokenSource;

        private readonly ClientsConnection _clientsConnection = new ClientsConnection();

        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly List<QueueItemTask> _queueItemTasks = new();

        private Thread? _thread;

        private readonly SystemConfig _systemConfig;

        private readonly ICacheItemServiceManager _cacheItemServiceManager;

        private ISimpleLog _log;

        private TimeSpan _archiveLogsFrequency = TimeSpan.FromHours(12);
        private DateTimeOffset _lastArchiveLogs = DateTimeOffset.MinValue;

        public Worker(SystemConfig systemConfig, ICacheItemServiceManager cacheItemServiceManager, ISimpleLog log)
        {
            _systemConfig = systemConfig;
            _cacheItemServiceManager = cacheItemServiceManager;
            _log = log;

            // Handle message received
            _clientsConnection.OnMessageReceived += delegate (MessageBase message, MessageReceivedInfo messageReceivedInfo)
            {
                //_log.Log(DateTimeOffset.UtcNow, "Information", $"Received message {message.TypeId} from {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");

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
            _clientsConnection.Dispose();
        }

        private static bool IsInDockerContainer
        {
            get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
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
               
                // Periodically archive logs
                if (_lastArchiveLogs.Add(_archiveLogsFrequency) <= DateTimeOffset.UtcNow &&
                    !_queueItems.Any(i => i.ItemType == QueueItemTypes.ArchiveLogs))
                {
                    _lastArchiveLogs = DateTimeOffset.UtcNow;
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ArchiveLogs });
                }

                Thread.Yield();
            }
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

                    // Periodically archive logs
                    if (_lastArchiveLogs.Add(_archiveLogsFrequency) <= DateTimeOffset.UtcNow &&
                        !_queueItems.Any(i => i.ItemType == QueueItemTypes.ArchiveLogs))
                    {
                        _lastArchiveLogs = DateTimeOffset.UtcNow;
                        _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ArchiveLogs });
                    }

                    Thread.Yield();
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape &&
                    !_cancellationTokenSource.Token.IsCancellationRequested);
        }

        /// <summary>
        /// Starts worker thread, waits for event that triggers stop (Container stop, user presses Escape)
        /// </summary>
        /// <param name="cancellationTokenSource"></param>
        public void Start(CancellationTokenSource cancellationTokenSource)
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");            

            _cancellationTokenSource = cancellationTokenSource; 

            // Start thread
            _thread = new Thread(Run);
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

        public void Stop()
        {            
            if (_cancellationTokenSource != null) _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Performs worker processing
        /// </summary>
        public void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            // Listen for clients
            _clientsConnection.StartListening(_systemConfig.LocalPort);            

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Process queue
                    while (_queueItems.Any() &&
                        _queueItemTasks.Count < _systemConfig.MaxConcurrentTasks)
                    {
                        if (_queueItems.TryDequeue(out QueueItem queueItem))
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
                    Thread.Sleep(5000);
                }
            }

            // Stop listening
            _clientsConnection.StopListening();
        }
       
        /// <summary>
        /// Processes queue item
        /// </summary>
        /// <param name="queueItem"></param>
        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (queueItem.ItemType == QueueItemTypes.MessageReceived && queueItem.Message != null)
            {
                switch (queueItem.Message.TypeId)
                {
                    case MessageTypeIds.AddCacheItemRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleAddCacheItemRequestAsync((AddCacheItemRequest)queueItem.Message, queueItem.MessageReceivedInfo!), queueItem));
                        break;

                    case MessageTypeIds.DeleteCacheItemRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleDeleteCacheItemRequestAsync((DeleteCacheItemRequest)queueItem.Message, queueItem.MessageReceivedInfo!), queueItem));
                        break;

                    case MessageTypeIds.GetCacheItemKeysRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetCacheItemKeysRequestAsync((GetCacheItemKeysRequest)queueItem.Message, queueItem.MessageReceivedInfo!), queueItem));
                        break;

                    case MessageTypeIds.GetCacheItemRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetCacheItemRequestAsync((GetCacheItemRequest)queueItem.Message, queueItem.MessageReceivedInfo!), queueItem));
                        break;
                }
            }
            else if (queueItem.ItemType == QueueItemTypes.ArchiveLogs)
            {
                _queueItemTasks.Add(new QueueItemTask(ArchiveLogsAsync(), queueItem));
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
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ItemType}");

                //switch (queueItemTask.QueueItem.ItemType)
                //{
                //    case QueueItemTypes.ConnectionMessage:
                //        //_log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ConnectionMessage.TypeId}");
                //        break;
                //    default:
                //        //_log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ItemType}");
                //        break;
                //}
            }
            else
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }
        private Task HandleAddCacheItemRequestAsync(AddCacheItemRequest addCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing request to add {addCacheItemRequest.CacheItem.Key} to cache ({messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port})");

                var response = new AddCacheItemResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addCacheItemRequest.Id,
                        Sequence = 1
                    },
                };
             
                if (addCacheItemRequest.SecurityKey != _systemConfig.SecurityKey)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                    response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                }
                else
                {                    
                    addCacheItemRequest.CacheItem.Id = Guid.NewGuid().ToString();                   
                    addCacheItemRequest.CacheItem.CreatedDateTime = DateTimeOffset.UtcNow;         // TODO: Sending DateTimeOffset does not serialize

                    var cacheItemService = _cacheItemServiceManager.GetByEnvironment(addCacheItemRequest.Environment, true)!;

                    cacheItemService.Add(addCacheItemRequest.CacheItem);                    
                }

                // Send response
                _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed request to add {addCacheItemRequest.CacheItem.Key} to cache");
            });
        }

        private Task HandleGetCacheItemRequestAsync(GetCacheItemRequest getCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getCacheItemRequest.TypeId} {getCacheItemRequest.ItemKey} {getCacheItemRequest.Environment}");

                var response = new GetCacheItemResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getCacheItemRequest.Id,
                        Sequence = 1
                    },
                };

                if (getCacheItemRequest.SecurityKey != _systemConfig.SecurityKey)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                    response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                }
                else
                {
                    var cacheItemService = _cacheItemServiceManager.GetByEnvironment(getCacheItemRequest.Environment, true);

                    // Get cache item
                    response.CacheItem = cacheItemService.Get(getCacheItemRequest.ItemKey);                   
                }
                
                // Send response                
                _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);                
            });
        }

        private Task HandleGetCacheItemKeysRequestAsync(GetCacheItemKeysRequest getCacheItemKeysRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getCacheItemKeysRequest.TypeId}");

                var response = new GetCacheItemKeysResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getCacheItemKeysRequest.Id,
                        Sequence = 1
                    },
                };

                if (getCacheItemKeysRequest.SecurityKey != _systemConfig.SecurityKey)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                    response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                }
                else
                {
                    var cacheItemService = _cacheItemServiceManager.GetByEnvironment(getCacheItemKeysRequest.Environment, true);

                    response.ItemKeys = cacheItemService.GetKeysByFilter(getCacheItemKeysRequest.Filter);
                }

                // Send response                
                _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);
            });
        }

        private Task HandleDeleteCacheItemRequestAsync(DeleteCacheItemRequest deleteCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(() =>
            {
                var response = new DeleteCacheItemResponse()                        
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = deleteCacheItemRequest.Id,
                        Sequence = 1
                    },
                };

                if (deleteCacheItemRequest.SecurityKey != _systemConfig.SecurityKey)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                    response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                }
                else
                {
                    var cacheItemService = _cacheItemServiceManager.GetByEnvironment(deleteCacheItemRequest.Environment, true);

                    if (String.IsNullOrEmpty(deleteCacheItemRequest.ItemKey))
                    {
                        _log.Log(DateTimeOffset.UtcNow, "Information", "Cleared cache");

                        // Clear
                        cacheItemService.DeleteAll();
                    }
                    else
                    {
                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleted cache item {deleteCacheItemRequest.ItemKey}");

                        // Delete cache item
                        cacheItemService.Delete(deleteCacheItemRequest.ItemKey);
                    }                    
                }

                // Send response
                _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);
            });
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
    }
}
