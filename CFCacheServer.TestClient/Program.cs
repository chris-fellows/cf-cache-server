using CFCacheServer.Interfaces;
using CFCacheServer.Services;
using CFCacheServer.Utilities;
using CFCacheServer.TestClient.Models;
using CFConnectionMessaging.Models;
using System.Diagnostics;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

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
stopwatch.Start();
cacheServerClient.AddAsync("TestObject1", testObject, TimeSpan.FromHours(12)).Wait();
stopwatch.Stop();
Console.WriteLine($"Add cache item took {stopwatch.ElapsedMilliseconds} ms");

stopwatch.Restart();
var testObjectCached = cacheServerClient.GetByKeyAsync<TestObject?>("TestObject1").Result;
stopwatch.Stop();
Console.WriteLine($"Get cache item took {stopwatch.ElapsedMilliseconds} ms");

int xxxx = 1000;