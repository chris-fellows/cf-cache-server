using CFCacheServer.Common.Data;
using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using Microsoft.EntityFrameworkCore;

namespace CFCacheServer.Common.Services
{
    /// <summary>
    /// Cache item service. Cache items are stored in memory and some may be persisted
    /// </summary>
    public class CacheItemService : ICacheItemService, IDisposable
    {
        private readonly IDbContextFactory<CFCacheServerDataContext> _dbFactory;
        private CFCacheServerDataContext? _context;

        private string _cacheEnvironmentId = String.Empty;
        private Dictionary<string, CacheItem> _cacheItems = new();

        private Mutex _mutex = new Mutex();

        private readonly System.Timers.Timer? _timer;        // Initialised when cache contains items

        /// <summary>
        /// Total size of cache (Approx).
        /// </summary>
        private long _totalSize = 0;

        public CacheItemService(IDbContextFactory<CFCacheServerDataContext> dbFactory)
        {
            _dbFactory = dbFactory;
            
            // Initialise timer for expiry            
            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 30000;
            _timer.Enabled = true;            
        }   

        public string CacheEnvironmentId
        {
            get { return _cacheEnvironmentId; }
            set
            {
                try
                {
                    _mutex.WaitOne();

                    _cacheEnvironmentId = value;
                    
                    // Load cache items
                    var cacheItems = Context.CacheItem
                                .Where(i => i.CacheEnvironmentId == _cacheEnvironmentId).ToList();

                    _totalSize = 0;
                    _cacheItems.Clear();
                    foreach (var cacheItem in cacheItems)
                    {
                        _cacheItems.Add(cacheItem.Key, cacheItem);
                        _totalSize += cacheItem.GetTotalSize();
                    }
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
        }                

        protected CFCacheServerDataContext Context
        {
            get
            {
                lock (_dbFactory)
                {
                    if (_context == null) _context = _dbFactory.CreateDbContext();
                    return _context;
                }
            }
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;
                _mutex.WaitOne();

                Expire();
            }
            finally
            {
                _mutex.ReleaseMutex();
                _timer.Enabled = true;
            }            
        }

        public void DeleteAll()
        {
            try
            {
                _mutex.WaitOne();

                while (_cacheItems.Any())
                {
                    DeleteInternal(_cacheItems.First().Value);
                }                
            }
            finally
            {
                _totalSize = 0;
                _mutex.ReleaseMutex();
            }
        }

        private void Expire()
        {
            var now = DateTimeOffset.UtcNow;
                      
            var keysToExpire = new List<string>();

            foreach (var key in _cacheItems.Keys)
            {
                if (IsExpired(_cacheItems[key], now))
                {
                    keysToExpire.Add(key);
                }
            }

            while (keysToExpire.Any())
            {
                if (_cacheItems.ContainsKey(keysToExpire[0]))
                {
                    DeleteInternal(_cacheItems[keysToExpire[0]]);
                }
                keysToExpire.RemoveAt(0);
            }           
        }

        public void Add(CacheItem cacheItem)
        {                     
            try
            {                
                _mutex.WaitOne();
                
                if (_cacheItems.ContainsKey(cacheItem.Key))
                { 
                    DeleteInternal(_cacheItems[cacheItem.Key]);                 
                }

                AddInternal(cacheItem);
            }            
            finally
            {
                _mutex.ReleaseMutex();
            }            
        }

        private static bool IsExpired(CacheItem cacheItem, DateTimeOffset now)
        {
            return cacheItem.ExpiryMilliseconds > 0 &&
                cacheItem.CreatedDateTime.AddMilliseconds(cacheItem.ExpiryMilliseconds) <= now;
        }

        public CacheItem? Get(string key)
        {            
            try
            {
                _mutex.WaitOne();

                if (_cacheItems.ContainsKey(key))
                {
                    var cacheItem = _cacheItems[key];

                    if (IsExpired(cacheItem, DateTimeOffset.UtcNow))
                    {
                        DeleteInternal(cacheItem);
                    }
                    else
                    {
                        return cacheItem;
                    }
                }                
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            
            return null;
        }

        public void Delete(string key)
        {
            try
            {
                _mutex.WaitOne();
                
                    if (_cacheItems.ContainsKey(key))
                    {
                        DeleteInternal(_cacheItems[key]);
                    }
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        public int ItemCount
        {
            get
            {
                try
                {
                    _mutex.WaitOne();

                    return _cacheItems.Count;
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }                
            }
        }

        public List<string> GetKeysByFilter(CacheItemFilter filter)
        {
            try
            {
                _mutex.WaitOne();

                var keys = new List<string>();

                var now = DateTimeOffset.UtcNow;
                
                    foreach (var key in _cacheItems.Keys)
                    {
                        if (IsValidForFilter(_cacheItems[key], filter) &&
                            !IsExpired(_cacheItems[key], now))
                        {
                            keys.Add(key);
                        }
                    }

                    keys.Sort();


                return keys;
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private static bool IsValidForFilter(CacheItem cacheItem, CacheItemFilter filter)
        {
            if (!String.IsNullOrEmpty(filter.KeyStartsWith) &&
                !cacheItem.Key.StartsWith(filter.KeyStartsWith))
            {
                return false;
            }

            if (!String.IsNullOrEmpty(filter.KeyEndsWith) &&
                !cacheItem.Key.EndsWith(filter.KeyEndsWith))
            {
                return false;
            }

            if (!String.IsNullOrEmpty(filter.KeyContains) &&
                !cacheItem.Key.Contains(filter.KeyContains))
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Adds cache item (Memory and DB)
        /// </summary>
        /// <param name="cacheItem"></param>
        private void AddInternal(CacheItem cacheItem)
        {
            // Add to DB
            if (cacheItem.Persist)
            {                
                Context.CacheItem.Add(cacheItem);
                Context.SaveChanges();                
            }

            // Add to memory            
            _cacheItems.Add(cacheItem.Key, cacheItem);
            _totalSize += cacheItem.GetTotalSize();
        }

        /// <summary>
        /// Deletes cache item (Memory and DB)
        /// </summary>
        /// <param name="cacheItem"></param>
        private void DeleteInternal(CacheItem cacheItem)
        {
            // Delete from DB
            if (cacheItem.Persist)
            {
                var item = Context.CacheItem.FirstOrDefault(i => i.CacheEnvironmentId == _cacheEnvironmentId && i.Key == cacheItem.Key);
                if (item != null)
                {
                    Context.CacheItem.Remove(item);
                    Context.SaveChanges();
                }
            }

            // Delete from memory
            if (_cacheItems.ContainsKey(cacheItem.Key))
            {
                _cacheItems.Remove(cacheItem.Key);
                _totalSize -= cacheItem.GetTotalSize();
            }           
        }

        public long TotalSize => _totalSize;
    }
}
