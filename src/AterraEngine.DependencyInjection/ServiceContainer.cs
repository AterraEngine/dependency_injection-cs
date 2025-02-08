// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using Serilog;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer : IServiceContainer {
    private FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    private ConcurrentDictionary<Type, FrozenServiceRecord> ClosedGenericServiceRecords { get; } = new();
    private ConcurrentDictionary<Type, object> SingletonInstances { get; } = [];
    private ConcurrentDictionary<Type, Delegate> FactoriesCache { get; } = new();
    private Lazy<IScopedProvider> RootScopedProvider { get; set; } = null!;

    public Lazy<FrozenSet<Type>> DisposableRecords { get; private set; } = new(static () => FrozenSet<Type>.Empty);
    public Lazy<FrozenSet<Type>> AsyncDisposableRecords { get; private set; } = new(static () => FrozenSet<Type>.Empty);
    
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
        container.RootScopedProvider = new Lazy<IScopedProvider>(() => new ScopedProvider(container));
        if (collection.HasDisposalRecords) container.DisposableRecords = new Lazy<FrozenSet<Type>>(() => container.ServiceRecords.Values.Where(record => record.Disposal is FrozenServiceRecord.DisposalType.Disposable).Select(record => record.ServiceType).ToFrozenSet());
        if (collection.HasAsyncDisposalRecords) container.AsyncDisposableRecords = new Lazy<FrozenSet<Type>>(() => container.ServiceRecords.Values.Where(record => record.Disposal is FrozenServiceRecord.DisposalType.AsyncDisposable).Select(record => record.ServiceType).ToFrozenSet());

        // ReSharper disable once InvertIf
        // Log discarded records only if collection contains some and logging is enabled
        if (!collection.DiscardedRecords.IsEmpty && container.GetRootScopedProvider().GetService<ILogger>() is {} logger) {
            logger = logger.ForContext<ServiceContainer>();

            logger.Debug("Discarded services count: {@DiscardedRecords}", collection.DiscardedRecords.Count);
            foreach (IServiceRecord discardedRecord in collection.DiscardedRecords) {
                logger.Debug("Discarded service: {@DiscardedRecord}", discardedRecord);
            }
        }

        return container;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public IScopedProvider GetRootScopedProvider() 
        => RootScopedProvider.Value;

    public TService GetSingletonService<TService>(FrozenServiceRecord record) where TService : class
        => (TService)SingletonInstances.GetOrAdd(record.ServiceType,
            valueFactory: static (_, box) => box.Item1.CreateInstance<TService>(box.Item2, box.Item1.RootScopedProvider.Value),
            new ValueTuple<ServiceContainer, FrozenServiceRecord>(this, record)
        );

    public TService GetTransientService<TService>(FrozenServiceRecord record) where TService : class
        => GetFactory<TService>(record) switch {
            Func<IScopedProvider, TService> directFactory => directFactory(RootScopedProvider.Value),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(RootScopedProvider.Value),
            _ => throw new InvalidOperationException("Could not resolve factory for generic service.")
        };

    public TService CreateInstance<TService>(FrozenServiceRecord record, IScopedProvider serviceProvider) where TService : class
        => GetFactory<TService>(record) switch {
            Func<IScopedProvider, TService> directFactory => directFactory(serviceProvider),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(serviceProvider),
            _ => throw new InvalidOperationException("Could not resolve factory for generic service.")
        };

    private Delegate GetFactory<TService>(FrozenServiceRecord record) where TService : class
        => FactoriesCache.GetOrAdd(
            record.ServiceType,
            valueFactory: static (_, box) => {
                if (box.Item2.GenericService is FrozenServiceRecord.GenericServiceState.None) return box.Item2.GetFactory<TService>();
                FrozenServiceRecord genericRecord = box.Item1.ServiceRecords[typeof(TService).GetGenericTypeDefinition()];
                if (genericRecord.GenericService is FrozenServiceRecord.GenericServiceState.ClosedGeneric) return genericRecord.ImplementationFactory;
                return box.Item1.GetGenericRecord(typeof(TService)).ImplementationFactory;
            },
            new ValueTuple<ServiceContainer, FrozenServiceRecord>(this, record)
        );

    private FrozenServiceRecord GetGenericRecord(Type typeOfService)
        => ClosedGenericServiceRecords.GetOrAdd(typeOfService,
            valueFactory: static (type, container) => FrozenServiceRecordHelper.CreateClosedGenericRecord(
                type,
                container.ServiceRecords[type.GetGenericTypeDefinition()]
            ),
            this
        );

    internal bool TryResolveRecord<TService>([NotNullWhen(true)] out FrozenServiceRecord? record) where TService : class {
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
