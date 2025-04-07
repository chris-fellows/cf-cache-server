using CFCacheServer.Common.Interfaces;
using CFCacheServer.Models;
using System;

namespace CFCacheServer.Common.Services
{
    public class MemoryCacheService : ICacheService
    {
        private Dictionary<string, CacheItem> _cacheItems = new();

        private Mutex _mutex = new Mutex();

        private System.Timers.Timer? _timer;        // Initialised when cache contains items

        public MemoryCacheService()
        { 
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

                _cacheItems.Clear();
            }
            finally
            {
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

            while(keysToExpire.Any())
            {
                if (_cacheItems.ContainsKey(keysToExpire[0]))
                {                    
                    _cacheItems.Remove(keysToExpire[0]);
                }
                keysToExpire.RemoveAt(0);
            }
        }

        public void Add(CacheItem cacheItem)
        {            
            // Initialise timer for expiry
            if (_timer == null)
            {
                _timer = new System.Timers.Timer();
                _timer.Elapsed += _timer_Elapsed;
                _timer.Interval = 30000;
                _timer.Enabled = true;
            }

            try
            {                
                _mutex.WaitOne();

                if (_cacheItems.ContainsKey(cacheItem.Key))
                {
                    _cacheItems.Remove(cacheItem.Key);
                }
                
                _cacheItems.Add(cacheItem.Key, cacheItem);                
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
                        _cacheItems.Remove(key);
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
                    _cacheItems.Remove(key);
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
                foreach(var key in _cacheItems.Keys)
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
    }
}
