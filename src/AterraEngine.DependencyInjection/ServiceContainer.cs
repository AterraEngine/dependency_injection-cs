// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {
    private FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; init; } = null!;
    private ConcurrentDictionary<Type, FrozenServiceRecord> ClosedGenericServiceRecords { get; } = new();
    private ConcurrentDictionary<Guid, object> SingletonInstances { get; } = [];
    private ConcurrentDictionary<Guid, Delegate> FactoriesCache { get; } = new();
    internal Lazy<IScopedProvider> ContainerProvider { get; set; } = null!;
    public Lazy<IScopedProvider> RootScopedProvider { get; set; } = null!;

    public Lazy<FrozenSet<Guid>> DisposableRecords { get; private set; } = new(static () => FrozenSet<Guid>.Empty);
    public Lazy<FrozenSet<Guid>> AsyncDisposableRecords { get; private set; } = new(static () => FrozenSet<Guid>.Empty);
    
    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ServiceCollection collection) {
        // Create the container
        var container = new ServiceContainer {
            ServiceRecords = collection.Records.ToFrozenDictionary(
                keySelector: kvp => kvp.Key,
                elementSelector: kvp => kvp.Value.ToFrozen()
            )
        };

        // The following lazies need the container to work correctly
        container.ContainerProvider = new Lazy<IScopedProvider>(() => new ServiceContainerScopedProvider(container));
        container.RootScopedProvider = new Lazy<IScopedProvider>(() => new ScopedProvider(container));
        if (collection.HasDisposalRecords) container.DisposableRecords = new Lazy<FrozenSet<Guid>>(() => container.ServiceRecords.Values.Where(record => record.Disposal is FrozenServiceRecord.DisposalType.Disposable).Select(record => record.Id).ToFrozenSet());
        if (collection.HasAsyncDisposalRecords) container.AsyncDisposableRecords = new Lazy<FrozenSet<Guid>>(() => container.ServiceRecords.Values.Where(record => record.Disposal is FrozenServiceRecord.DisposalType.AsyncDisposable).Select(record => record.Id).ToFrozenSet());

        return container;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public IScopedProvider GetRootScopedProvider()
        => RootScopedProvider.Value;
    
    public TService GetSingletonService<TService>(Guid id) where TService : class
        => (TService)SingletonInstances.GetOrAdd(id,
            valueFactory: static (id, container) => container.CreateInstance<TService>(id, container.ContainerProvider.Value),
            this
        );

    public TService GetTransientService<TService>(Guid id) where TService : class
        => GetFactory<TService>(id) switch {
            Func<IScopedProvider, TService> directFactory => directFactory(ContainerProvider.Value),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(ContainerProvider.Value),
            _ => throw new InvalidOperationException("Could not resolve factory for generic service.")
        };

    public TService CreateInstance<TService>(Guid id, IScopedProvider serviceProvider) where TService : class {
        var factory = GetFactory<TService>(id);
        return factory switch {
            Func<IScopedProvider, TService> directFactory => directFactory(serviceProvider),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(serviceProvider),
            _ => throw new InvalidOperationException("Could not resolve factory for generic service.")
        };
    }

    private Delegate GetFactory<TService>(Guid id) where TService : class
        => FactoriesCache.GetOrAdd(
            id,
            valueFactory: static (_, container) => {
                if (container.ServiceRecords.TryGetValue(typeof(TService), out FrozenServiceRecord? foundRecord) && foundRecord.GenericService is FrozenServiceRecord.GenericServiceState.None) return foundRecord.GetFactory<TService>();
                FrozenServiceRecord genericRecord = container.ServiceRecords[typeof(TService).GetGenericTypeDefinition()];
                if (genericRecord.GenericService is FrozenServiceRecord.GenericServiceState.ClosedGeneric) return genericRecord.ImplementationFactory;
                return container.GetGenericRecord(typeof(TService)).ImplementationFactory;
            },
            this
        );

    private FrozenServiceRecord GetGenericRecord(Type typeOfService)
        => ClosedGenericServiceRecords.GetOrAdd(typeOfService,
            valueFactory: static (type, container) => FrozenServiceRecordHelper.CreateClosedGenericRecord(
                type,
                container.ServiceRecords[type.GetGenericTypeDefinition()]
            ),
            this
        );

    public bool TryResolveRecord<TService>([NotNullWhen(true)] out FrozenServiceRecord? record) where TService : class {
        if (ServiceRecords.TryGetValue(typeof(TService), out record)) return true;
        if (typeof(TService).IsGenericType) {
            record = GetGenericRecord(typeof(TService));
            return true;
        }

        record = null;
        return false;
    }

    #region IEnumerable<FrozenServiceRecord>
    public IEnumerator<FrozenServiceRecord> GetEnumerator()
        => ServiceRecords.Values.ToBuilder().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count => ServiceRecords.Count;
    #endregion
}
