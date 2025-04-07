using CFCacheServer.Common.Interfaces;
using CFCacheServer.Constants;
using CFCacheServer.Enums;
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

        private readonly ICacheService _cacheService;

        private ISimpleLog _log;

        private TimeSpan _archiveLogsFrequency = TimeSpan.FromHours(12);
        private DateTimeOffset _lastArchiveLogs = DateTimeOffset.MinValue;

        public Worker(SystemConfig systemConfig, ICacheService cacheService, ISimpleLog log)
        {
            _systemConfig = systemConfig;
            _cacheService = cacheService;
            _log = log;

            // Handle connection message received
            _clientsConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Received message {connectionMessage.TypeId} from {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");

                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.ConnectionMessage,
                    ConnectionMessage = connectionMessage,
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

            // Stop listening
            _clientsConnection.StopListening();
        }
       
        /// <summary>
        /// Processes queue item
        /// </summary>
        /// <param name="queueItem"></param>
        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (queueItem.ItemType == QueueItemTypes.ConnectionMessage && queueItem.ConnectionMessage != null)
            {
                switch (queueItem.ConnectionMessage.TypeId)
                {
                    case MessageTypeIds.AddCacheItemRequest:
                        var addCacheItemRequest = _clientsConnection.MessageConverterList.AddCacheItemRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleAddCacheItemRequestAsync(addCacheItemRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.DeleteCacheItemRequest:
                        var deleteCacheItemRequest = _clientsConnection.MessageConverterList.DeleteCacheItemRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleDeleteCacheItemRequestAsync(deleteCacheItemRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetCacheItemKeysRequest:
                        var getCacheItemKeysRequest = _clientsConnection.MessageConverterList.GetCacheItemKeysRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetCacheItemKeysRequestAsync(getCacheItemKeysRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetCacheItemRequest:
                        var getCacheItemRequest = _clientsConnection.MessageConverterList.GetCacheItemRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetCacheItemRequestAsync(getCacheItemRequest, queueItem.MessageReceivedInfo), queueItem));
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
                switch (queueItemTask.QueueItem.ItemType)
                {
                    case QueueItemTypes.ConnectionMessage:
                        //_log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ConnectionMessage.TypeId}");
                        break;
                    default:
                        //_log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ItemType}");
                        break;
                }
            }
            else
            {
                //_log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }
        private Task HandleAddCacheItemRequestAsync(AddCacheItemRequest addCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
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
                    // TODO: Sending DateTimeOffset does not serialize
                    addCacheItemRequest.CacheItem.CreatedDateTime = DateTimeOffset.UtcNow;

                    // Add cache item
                    _cacheService.Add(addCacheItemRequest.CacheItem);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Adding {addCacheItemRequest.CacheItem.Key} to cache");
                }

                // Send response
                _clientsConnection.SendAddCacheItemResponse(response, messageReceivedInfo.RemoteEndpointInfo);
            });
        }

        private Task HandleGetCacheItemRequestAsync(GetCacheItemRequest getCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getCacheItemRequest.TypeId} {getCacheItemRequest.ItemKey}");

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
                    // Get cache item
                    response.CacheItem = _cacheService.Get(getCacheItemRequest.ItemKey);
                }
                
                // Send response                
                _clientsConnection.SendGetCacheItemResponse(response, messageReceivedInfo.RemoteEndpointInfo);                
            });
        }

        private Task HandleGetCacheItemKeysRequestAsync(GetCacheItemKeysRequest getCacheItemKeysRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
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
                    response.ItemKeys = _cacheService.GetKeysByFilter(getCacheItemKeysRequest.Filter);

                }

                // Send response                
                _clientsConnection.SendGetCacheItemKeysResponse(response, messageReceivedInfo.RemoteEndpointInfo);
            });
        }

        private Task HandleDeleteCacheItemRequestAsync(DeleteCacheItemRequest deleteCacheItemRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
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
                    if (String.IsNullOrEmpty(deleteCacheItemRequest.ItemKey))
                    {
                        _log.Log(DateTimeOffset.UtcNow, "Information", "Cleared cache");

                        // Clear
                        _cacheService.DeleteAll();
                    }
                    else
                    {
                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleted cache item {deleteCacheItemRequest.ItemKey}");

                        // Delete cache item
                        _cacheService.Delete(deleteCacheItemRequest.ItemKey);
                    }                    
                }

                // Send response
                _clientsConnection.SendDeleteCacheItemResponse(response, messageReceivedInfo.RemoteEndpointInfo);
            });
        }

        /// <summary>
        /// Archives logs
        /// </summary>
        /// <returns></returns>
        private Task ArchiveLogsAsync()
        {
            return Task.Factory.StartNew(() =>
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
