// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {
    private ConcurrentDictionary<Guid, object> SingletonInstances { get; } = new();
    public FrozenDictionary<Type, IServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, IServiceRecord>.Empty;
    
    private IScopedProvider? RootScopedProvider { get; set; }
    
    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ServiceCollection collection) {
        var container = new ServiceContainer {
            ServiceRecords = collection.Records.ToFrozenDictionary()
        };

        if (!collection.DiscardedRecords.IsEmpty) container.LogDiscardedRecords(collection);
        
        return container;
    }

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

    private void LogDiscardedRecords(ServiceCollection collection) {
        IScopedProvider provider = GetRootScopedProvider();
        if (provider.GetService<ILogger>() is not {} logger) return;

        logger = logger.ForContext<ServiceContainer>();
        logger.Debug("Discarded services count: {@DiscardedRecords}", collection.DiscardedRecords.Count);
        foreach (IServiceRecord discardedRecord in collection.DiscardedRecords) {
            logger.Debug("Discarded service: {@DiscardedRecord}", discardedRecord);
        }
    }

    public IScopedProvider GetRootScopedProvider() {
        if (RootScopedProvider is not null) return RootScopedProvider;
        RootScopedProvider = new ScopedProvider(this);
        return RootScopedProvider;
    }
}
