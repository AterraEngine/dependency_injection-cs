// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer(IDictionary<Type, IServiceRecord> records) : IServiceContainer {
    private ConcurrentDictionary<Guid, object> SingletonInstances { get; } = new();
    public FrozenDictionary<Type, IServiceRecord> ServiceRecords { get; } = records.ToFrozenDictionary();
    
    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ConcurrentDictionary<Type, IServiceRecord> records) => new ServiceContainer(records);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetSingletonService<TService>(IServiceRecord record, IScopedProvider serviceProvider) where TService : class {
        if (SingletonInstances.TryGetValue(record.Id, out object? instance) && instance is TService singletonService) return singletonService;

        record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory);
        if (factory?.Invoke(serviceProvider) is not {} casted) return null;

        SingletonInstances.TryAdd(record.Id, casted);
        return casted;
    }
}
