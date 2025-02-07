// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {
    public FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    public ConcurrentDictionary<Type, FrozenServiceRecord> ClosedGenericRecords { get; } = new();

    private ImmutableDictionary<Guid, object> SingletonInstances { get; set; } = ImmutableDictionary<Guid, object>.Empty;
    public FrozenSet<Guid> DisposableRecords { get; private init; } = FrozenSet<Guid>.Empty;
    public FrozenSet<Guid> AsyncDisposableRecords { get; private init; } = FrozenSet<Guid>.Empty;

    private IScopedProvider? RootScopedProvider { get; set; }
    
    private static readonly ObjectPool<Queue<FrozenServiceRecord>> QueuePool = ObjectPool.Create<Queue<FrozenServiceRecord>>();


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
        // If it already exists we don't need to other fancy stuff like ...
        //      Just read the rest for the "stuff"
        if (SingletonInstances.TryGetValue(record.Id, out object? instance)) return instance as TService;

        // Don't need to do complex stuff if we aren't a generic service
        if (record.GenericService == FrozenServiceRecord.GenericServiceState.None) {
            if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory)) return null;
            return RegisterAndReturn(record, factory(serviceProvider));
        }
        
        // Beyond this point is only really used for generic type definitions
        //      This queue shouldn't be used more than two times. Once for the original record
        //      Once for the newly created record
        Queue<FrozenServiceRecord> queue = QueuePool.Get(); 
        queue.Enqueue(record);
        TService? newInstance = null;
        while (queue.TryDequeue(out FrozenServiceRecord queuedRecord)) {
            // Handle open generic records
            if (queuedRecord.GenericService == FrozenServiceRecord.GenericServiceState.OpenGeneric) {
                Type serviceType = typeof(TService);
                if (!serviceType.IsGenericType) return null; // Resolve closed generic type

                // Resolve until we get it
                Type genericTypeDefinition = serviceType.GetGenericTypeDefinition();
                if (!TryGetClosedGenericRecord(genericTypeDefinition, serviceType, out record)) return null;
                queue.Enqueue(record);
                continue;
            }
            
            if (record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? factory)) {
                newInstance = factory(serviceProvider);
                break;
            }

            // ReSharper disable once InvertIf
            if (record.TryGetFactory<object>(out Func<IScopedProvider, object>? factoryOfObject)) {
                if (factoryOfObject(serviceProvider) is not TService casted) return null;
                newInstance = casted;
                break;
            }
        }
        if (newInstance is null) return null;
        return RegisterAndReturn(record, newInstance);
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
    public bool TryGetClosedGenericRecord(Type genericTypeDefinition, Type typeOfService, out FrozenServiceRecord closedRecord) {
        // Get the open generic record first
        if (!ServiceRecords.TryGetValue(genericTypeDefinition, out FrozenServiceRecord openRecord)) {
            closedRecord = default;
            return false;
        }
        
        // When we have this, we can proceed
        if (ClosedGenericRecords.TryGetValue(typeOfService, out closedRecord)) return true;
        closedRecord = FrozenServiceRecordHelper.CreateClosedGenericRecord(typeOfService, openRecord);
        ClosedGenericRecords.TryAdd(typeOfService,closedRecord);
        return true;
    }

    private TService RegisterAndReturn<TService>(FrozenServiceRecord record, TService service) where TService : class {
        SingletonInstances = SingletonInstances.Add(record.Id, service);
        return service;
        
    }
}
