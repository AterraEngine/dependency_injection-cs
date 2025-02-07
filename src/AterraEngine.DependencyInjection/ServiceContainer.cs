// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {

    private static readonly ObjectPool<Queue<FrozenServiceRecord>> QueuePool = ObjectPool.Create<Queue<FrozenServiceRecord>>();
    public FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    public ConcurrentDictionary<Type, FrozenServiceRecord?> ClosedGenericRecords { get; } = new();

    private ConcurrentDictionary<Type, object> SingletonInstances { get; } = [];
    public FrozenSet<Type> DisposableRecords { get; private init; } = FrozenSet<Type>.Empty;
    public FrozenSet<Type> AsyncDisposableRecords { get; private init; } = FrozenSet<Type>.Empty;

    private IScopedProvider? RootScopedProvider { get; set; }

    public IScopedProvider GetRootScopedProvider() {
        if (RootScopedProvider is not null) return RootScopedProvider;

        RootScopedProvider = new ScopedProvider(this);
        return RootScopedProvider;
    }
    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ServiceCollection collection) {
        // Ensure all record IDs are unique (if necessary, though this should ideally be handled on input for efficiency)
        var seenIds = new HashSet<Guid>();
        foreach (IServiceRecord record in collection.Records.Values) {
            while (!seenIds.Add(record.Id)) record.Id = Guid.CreateVersion7();// Adjust IDs inline if needed to ensure uniqueness
        }

        // Combine iterations for records to populate all frozen structures in one pass
        var disposableIds = new HashSet<Type>();
        var asyncDisposableIds = new HashSet<Type>();

        FrozenDictionary<Type, FrozenServiceRecord> frozenRecords = collection.Records.ToFrozenDictionary(
            keySelector: kvp => kvp.Key,
            elementSelector: kvp => {
                // This way we only go over the values once
                //      Helps with reduced enu
                FrozenServiceRecord frozenRecord = kvp.Value.ToFrozen();

                switch (frozenRecord.Disposal) {
                    case FrozenServiceRecord.DisposalType.None: break;
                    case FrozenServiceRecord.DisposalType.Disposable: disposableIds.Add(frozenRecord.ServiceType); break;
                    case FrozenServiceRecord.DisposalType.AsyncDisposable: asyncDisposableIds.Add(frozenRecord.ServiceType); break;
                    default: throw new ArgumentOutOfRangeException(nameof(collection), $"Unknown disposal type for record with {frozenRecord.ServiceType.Name}");
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
        if (collection.DiscardedRecords.IsEmpty) return container;

        IScopedProvider provider = container.GetRootScopedProvider();
        if (provider.GetService<ILogger>() is not {} logger) return container;

        logger = logger.ForContext<ServiceContainer>();

        logger.Debug("Discarded services count: {@DiscardedRecords}", collection.DiscardedRecords.Count);
        foreach (IServiceRecord discardedRecord in collection.DiscardedRecords) {
            logger.Debug("Discarded service: {@DiscardedRecord}", discardedRecord);
        }

        return container;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetSingletonService<TService>(FrozenServiceRecord record, ScopedProvider serviceProvider) where TService : class {
        return SingletonInstances.GetOrAdd(record.ServiceType,
            valueFactory: static (type, provider) => {
                // Logic for non-generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? r)) {
                    return r.GetFactory<TService>().Invoke(provider);
                }

                // Logic for generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out r) && r.GenericService != FrozenServiceRecord.GenericServiceState.None) {
                    return provider.ServiceContainer.ResolveGenericService<TService>(r, provider);
                }

                // Return 0 to indicate failure
                // This looks weird, I know.
                // One of the few, and probably only times I've fought against the null checker and said "fuck it, I'm going to do something stupid"
                // And it works ... which is even scarier, so I used it a couple of times in this class.
                // Yes I know this is ... wrong ... but if it works, and is tested it's a "feature" :P
                return 0;
            },
            serviceProvider
        ) as TService;
    }

    internal object ResolveGenericService<TService>(FrozenServiceRecord record, ScopedProvider serviceProvider) where TService : class {
        if (ResolveGenericServiceFactory<TService>(record) is not {} factoryDelegate) return 0;

        return factoryDelegate switch {
            Func<IScopedProvider, TService> directFactory => directFactory(serviceProvider),
            Func<IScopedProvider, object> objectFactory => (objectFactory(serviceProvider) as TService)!,
            _ => 0
        };
    }

    internal Delegate? ResolveGenericServiceFactory<TService>(FrozenServiceRecord record) {
        // Pooled queue for handling generic service resolution
        Queue<FrozenServiceRecord> queue = QueuePool.Get();
        try {
            queue.Enqueue(record);
            while (queue.TryDequeue(out FrozenServiceRecord? queuedRecord)) {
                if (queuedRecord.GenericService != FrozenServiceRecord.GenericServiceState.OpenGeneric) return queuedRecord.ImplementationFactory;

                // Resolve the open generic to a closed generic record
                if (!TryGetGenericRecord(typeof(TService), out FrozenServiceRecord? newRecord)) return null;
                queue.Enqueue(newRecord);
            }

            return null;
        }
        finally {
            // Always return the queue to the pool
            queue.Clear();
            QueuePool.Return(queue);
        }
    }

    public bool TryGetGenericRecord(Type typeOfService, [NotNullWhen(true)] out FrozenServiceRecord? closedRecord) {
        closedRecord = ClosedGenericRecords.GetOrAdd(typeOfService,
            valueFactory: static (type, container) => {
                if (!container.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out FrozenServiceRecord? openRecord)) return null;
                return FrozenServiceRecordHelper.CreateClosedGenericRecord(type, openRecord);
            },
            this);

        return closedRecord is not null;
    }

    internal FrozenServiceRecord? ResolveRecord<TService>() where TService : class {
        Type typeOfService = typeof(TService);
        // Resolve the record and try and create the instance :  IService(...args)
        if (ServiceRecords.TryGetValue(typeOfService, out FrozenServiceRecord? record)) return record;

        // ReSharper disable once InvertIf
        // Check for open generic services : IService<T0,.. generic args>(...args)
        if (typeOfService.IsGenericType && TryGetGenericRecord(typeOfService, out FrozenServiceRecord? closedRecord)) return closedRecord;

        // If all else has failed
        return null;
    }
}
