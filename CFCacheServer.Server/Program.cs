using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CFCacheServer.Server.Models;
using CFCacheServer.Server;
using CFCacheServer.Common.Interfaces;
using CFCacheServer.Common.Services;
using CFCacheServer.Common.Logging;
using CFCacheServer.Logging;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Starting CF Cache Server");

        // Get system config
        var systemConfig = GetSystemConfig();

        // Get service provider
        var serviceProvider = CreateServiceProvider();
        
        // Start worker
        var worker = new Worker(systemConfig,
                            serviceProvider.GetRequiredService<ICacheService>(),
                            serviceProvider.GetRequiredService<ISimpleLog>());

        var cancellationTokenSource = new CancellationTokenSource();
        worker.Start(cancellationTokenSource);
     
        Console.WriteLine("Terminating CF Cache Server");
    }  

    private static SystemConfig GetSystemConfig()
    {
        return new SystemConfig()
        {
            LocalPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LocalPort"].ToString()),
            SecurityKey = System.Configuration.ConfigurationManager.AppSettings["SecurityKey"].ToString()
        };
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var configFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Config");
        var logFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log");

        var configuration = new ConfigurationBuilder()
            .Build();

        var serviceProvider = new ServiceCollection()
              .AddSingleton<ICacheService, MemoryCacheService>()

              // Add logging (Console & CSV)
              .AddScoped<ISimpleLog>((scope) =>
              {
                  return new SimpleMultiLog(new() {
                        new SimpleConsoleLog(),
                        new SimpleLogCSV(Path.Combine(logFolder, "CacheServer-{date}.txt"))
                    });
              })

            .BuildServiceProvider();

        return serviceProvider;
    }

    /// <summary>
    /// Registers all types implementing interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <param name="lifetime"></param>
    private static IServiceCollection RegisterAllTypes<T>(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var typesFromAssemblies = assemblies.SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces().Contains(typeof(T))));
        foreach (var type in typesFromAssemblies)
        {
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }

        return services;
    }
}