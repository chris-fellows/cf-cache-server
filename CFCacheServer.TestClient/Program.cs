using CFCacheServer.Interfaces;
using CFCacheServer.Models;
using CFCacheServer.Services;
using CFCacheServer.Utilities;
using CFCacheServer.TestClient.Models;
using CFConnectionMessaging.Models;
using System.Diagnostics;

var remoteEndpointInfo = new EndpointInfo()
{
    Ip = "192.168.1.45",
    Port = 10200
};
var securityKey = "ABCDE";

// Initialise cache server client
var cacheServerClient = new CacheServerClient(remoteEndpointInfo, NetworkUtilities.GetFreeLocalPort(10000, 10100, new()), securityKey);

var testObject = new TestObject()
{
    Id = Guid.NewGuid().ToString(),
    BoolValue = true,
    Int32Value = 1234567
};

var stopwatch = new Stopwatch();
//stopwatch.Start();
//cacheServerClient.AddAsync("TestObject1", testObject, TimeSpan.FromHours(12)).Wait();
//stopwatch.Stop();
//Console.WriteLine($"Add cache item took {stopwatch.ElapsedMilliseconds} ms");

for(int index = 0; index < 200; index++)
{
    var testObject1 = new TestObject()
    {
        Id = Guid.NewGuid().ToString(),
        BoolValue = true,
        Int32Value = 1234567
    };

    cacheServerClient.AddAsync($"TestObject{index}", testObject1, TimeSpan.FromHours(12), true).Wait();
}

// Get keys for filter
var keys = cacheServerClient.GetKeysByFilterAsync(new CacheItemFilter() {  KeyContains = "es" }).Result;

stopwatch.Restart();
var testObjectCached = cacheServerClient.GetByKeyAsync<TestObject?>("TestObject1").Result;
stopwatch.Stop();
Console.WriteLine($"Get cache item took {stopwatch.ElapsedMilliseconds} ms");

cacheServerClient.DeleteAsync("TestObject2").Wait();

int xxxx = 1000;