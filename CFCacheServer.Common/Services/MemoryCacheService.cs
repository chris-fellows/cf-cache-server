using CFCacheServer.Common.Interfaces;
using CFCacheServer.Models;
using System;

namespace CFCacheServer.Common.Services
{
    public class MemoryCacheService : ICacheService
    {
        private Dictionary<string, CacheItemInternal> _cacheItems = new();

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

        public void Clear()
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
                if (_cacheItems[key].ExpiryMilliseconds > 0 &&
                        _cacheItems[key].CreatedDateTime.AddMilliseconds(_cacheItems[key].ExpiryMilliseconds) <= now)
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

        public void Add(CacheItemInternal cacheItem)
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

        public CacheItemInternal? Get(string key)
        {            
            try
            {
                _mutex.WaitOne();

                if (_cacheItems.ContainsKey(key))
                {
                    var cacheItem = _cacheItems[key];
                    
                    if (cacheItem.ExpiryMilliseconds == 0 ||
                        cacheItem.CreatedDateTime.AddMilliseconds(cacheItem.ExpiryMilliseconds) > DateTimeOffset.UtcNow)
                    {
                        return cacheItem;
                    }
                    else     // Expired, remove it
                    {
                        _cacheItems.Remove(key);
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
    }
}
