using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CFCacheServer.Server.Models;
using CFCacheServer.Server;
using CFCacheServer.Common.Interfaces;
using CFCacheServer.Common.Services;
using CFCacheServer.Common.Logging;
using CFCacheServer.Logging;
using CFCacheServer.Utilities;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Starting CF Cache Server ({NetworkUtilities.GetLocalIPV4Addresses()[0].ToString()}");

        // Get system config
        SystemConfig? systemConfig;
        try
        {
            systemConfig = GetSystemConfig(args);
        }
        catch(Exception exception)
        {
            Console.WriteLine($"Error getting system configuration: {exception.Message}");
            throw;
        }

        // Display config
        Console.WriteLine($"Local Port={systemConfig.LocalPort}, Max Concurrent Tasks={systemConfig.MaxConcurrentTasks}, Max Log Days={systemConfig.MaxLogDays}");

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

    /// <summary>
    /// Gets system config, defaults from App.config, can be overriden from command line
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    private static SystemConfig GetSystemConfig(string[] args)
    {
        // Get defaults from config file
        var systemConfig = new SystemConfig()
        { 
            LocalPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LocalPort"].ToString()),
            LogFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log"),
            MaxLogDays = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxLogDays"].ToString()),
            MaxConcurrentTasks = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxConcurrentTasks"].ToString()),
            SecurityKey = System.Configuration.ConfigurationManager.AppSettings["SecurityKey"].ToString()
        };

        // Override with arguments
        foreach(var arg in args)
        {
            if (arg.ToLower().StartsWith("-localport="))
            {
                systemConfig.LocalPort = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }
            else if (arg.ToLower().StartsWith("-maxconcurrenttasks="))
            {
                systemConfig.MaxConcurrentTasks = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }
            else if (arg.ToLower().StartsWith("-maxlogdays="))
            {
                systemConfig.MaxLogDays = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }
            else if (arg.ToLower().StartsWith("-securitykey="))
            {
                systemConfig.SecurityKey = arg.Trim().Split('=')[1];
            }            
        }

        if (systemConfig.LocalPort <= 0)
        {
            throw new ArgumentException($"Local Port config setting is invalid");
        }
        if (systemConfig.MaxConcurrentTasks < 0)
        {
            throw new ArgumentException($"Max Concurrent Tasks config setting is invalid");
        }
        if (systemConfig.MaxLogDays < 0)
        {
            throw new ArgumentException($"Max Log Days config setting is invalid");
        }
        if (String.IsNullOrEmpty(systemConfig.SecurityKey))
        {
            throw new ArgumentException($"Security Key config setting is invalid");
        }

        return systemConfig;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        //var configFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Config");
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