// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Serilog;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {
    public FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    private ImmutableDictionary<Guid, object> SingletonInstances { get; set; } = ImmutableDictionary<Guid, object>.Empty;
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
            keySelector: kvp => kvp.Key,
            elementSelector: kvp => {
                // This way we only go over the values once
                //      Helps with reduced enu
                FrozenServiceRecord frozenRecord = kvp.Value.ToFrozen();

                switch (frozenRecord.Disposal) {
                    case FrozenServiceRecord.DisposalType.None: break;
                    case FrozenServiceRecord.DisposalType.Disposable: disposableIds.Add(frozenRecord.Id); break;
                    case FrozenServiceRecord.DisposalType.AsyncDisposable: asyncDisposableIds.Add(frozenRecord.Id); break;
                    default: throw new ArgumentOutOfRangeException(nameof(collection), $"Unknown disposal type for record with ID {frozenRecord.Id}");
                }

                return frozenRecord;
            }
        );

        // Create the container
        //      All should be init properties, so that w
        var container = new ServiceContainer {
            ServiceRecords = frozenRecords,
            DisposableRecords = disposableIds.ToFrozenSet(),
            AsyncDisposableRecords = asyncDisposableIds.ToFrozenSet()
        };

        // Log discarded records only if collection contains some and logging is enabled
        if (!collection.DiscardedRecords.IsEmpty) container.TryLogDiscardedRecords(collection);

        return container;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetSingletonService<TService>(FrozenServiceRecord record, IScopedProvider serviceProvider) where TService : class {
        if (SingletonInstances.TryGetValue(record.Id, out object? instance)) return instance as TService;

        record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory);
        if (factory?.Invoke(serviceProvider) is not {} casted) return null;

        SingletonInstances = SingletonInstances.Add(record.Id, casted);
        return casted;
    }

    public TService GetRequiredSingletonService<TService>(FrozenServiceRecord record, IScopedProvider serviceProvider) where TService : class {
        if (SingletonInstances.TryGetValue(record.Id, out object? instance)) return (TService)instance;

        record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory);
        if (factory?.Invoke(serviceProvider) is not {} casted) throw new InvalidOperationException($"Service of type {typeof(TService)} is not registered.");

        SingletonInstances = SingletonInstances.Add(record.Id, casted);
        return casted;
    }

    private static void EnsureUniqueIds(ServiceCollection collection) {
        var seenIds = new HashSet<Guid>();
        foreach (IServiceRecord record in collection.Records.Values) {
            while (!seenIds.Add(record.Id)) record.Id = Guid.CreateVersion7();// Adjust IDs inline if needed to ensure uniqueness
        }
    }

    private void TryLogDiscardedRecords(ServiceCollection collection) {
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
