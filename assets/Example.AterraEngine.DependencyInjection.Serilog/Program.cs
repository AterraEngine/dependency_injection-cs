// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using AterraEngine.DependencyInjection.Bridges.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger=Microsoft.Extensions.Logging.ILogger;

namespace Example.AterraEngine.DependencyInjection.Serilog;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Program {

    public static void Main(string[] args) {
        var collection = new MsBridgeServiceCollection();
        
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        collection.MsServiceCollection.AddLogging(builder => builder.AddSerilog());
        IScopedProvider provider = collection.Build();
        
        ILogger logger = provider.GetRequiredService<ILogger<LoggerConfiguration>>();
        logger.LogInformation("Hello World!");
    }
}
