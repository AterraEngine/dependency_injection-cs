// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceCollection : IServiceCollection {
    private ConcurrentDictionary<Type, IServiceRecord> Records { get; } = new();
    // private ConcurrentDictionary<Type, object> Instances { get; } = new();
    
    public IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService {
        Records.AddOrUpdate(
            typeof(TService),
            _ => ServiceRecordFactory.CreateWithFactory<TService, TImplementation>(0),
            (_, _) => ServiceRecordFactory.CreateWithFactory<TService, TImplementation>(0)
        );
        
        return this;
    }

    public IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService {
        Records.AddOrUpdate(
            typeof(TService),
            _ => ServiceRecordFactory.CreateWithFactory<TService, TImplementation>(-1),
            (_, _) => ServiceRecordFactory.CreateWithFactory<TService, TImplementation>(-1)
        );

        return this;
    }

    public IServiceProvider Build() => new ServiceProvider {
        Records = Records.ToFrozenDictionary()
    };
}
