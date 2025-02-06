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
    public FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    public FrozenSet<Guid> DisposableRecords { get; private init; } = FrozenSet<Guid>.Empty;
    public FrozenSet<Guid> AsyncDisposableRecords { get; private init; } = FrozenSet<Guid>.Empty;
    
    private IScopedProvider? RootScopedProvider { get; set; }
    
    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ServiceCollection collection) {
        // Ensure all record IDs are unique (if necessary, though this should ideally be handled on input for efficiency)
        EnsureUniqueIds(collection);

        // Combine iterations for records to populate all frozen structures in one pass
        var disposableIds = new HashSet<Guid>();
        var asyncDisposableIds = new HashSet<Guid>();

        FrozenDictionary<Type, FrozenServiceRecord> frozenRecords = collection.Records.ToFrozenDictionary(
            kvp => kvp.Key,
            kvp => {
                FrozenServiceRecord frozenRecord = kvp.Value.ToFrozen();

                switch (frozenRecord.Disposal) {
                    case FrozenServiceRecord.DisposalType.Disposable: disposableIds.Add(frozenRecord.Id); break;
                    case FrozenServiceRecord.DisposalType.AsyncDisposable: asyncDisposableIds.Add(frozenRecord.Id); break;
                    case FrozenServiceRecord.DisposalType.None: break;
                    default: throw new ArgumentOutOfRangeException(nameof(collection), $"Unknown disposal type for record with ID {frozenRecord.Id}");
                }

                return frozenRecord;
            }
        );

        // Construct a container with precomputed frozen sets
        var container = new ServiceContainer {
            ServiceRecords = frozenRecords,
            DisposableRecords = disposableIds.ToFrozenSet(),
            AsyncDisposableRecords = asyncDisposableIds.ToFrozenSet(),
        };

        // Log discarded records only if collection contains some and logging is enabled
        if (!collection.DiscardedRecords.IsEmpty) container.TryLogDiscardedRecords(collection);

        return container;

    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetSingletonService<TService>(FrozenServiceRecord record, IScopedProvider serviceProvider) where TService : class {
        if (SingletonInstances.TryGetValue(record.Id, out object? instance) && instance is TService singletonService) return singletonService;

        record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory);
        if (factory?.Invoke(serviceProvider) is not {} casted) return null;

        SingletonInstances.TryAdd(record.Id, casted);
        return casted;
    }

    private static void EnsureUniqueIds(ServiceCollection collection) {
        var seenIds = new HashSet<Guid>();
        foreach (IServiceRecord record in collection.Records.Values) {
            while (!seenIds.Add(record.Id)) record.Id = Guid.CreateVersion7(); // Adjust IDs inline if needed to ensure uniqueness
        }
    }

    private void TryLogDiscardedRecords(ServiceCollection collection) {
        IScopedProvider provider = GetRootScopedProvider();
        if (provider.GetService<ILogger>() is not { } logger) return;

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
