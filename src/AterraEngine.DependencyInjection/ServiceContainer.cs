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
    public FrozenDictionary<Type, FrozenServiceRecord> ServiceRecords { get; private init; } = FrozenDictionary<Type, FrozenServiceRecord>.Empty;
    private ConcurrentDictionary<Type, FrozenServiceRecord> ClosedGenericRecords { get; } = new();

    private ConcurrentDictionary<Type, object> SingletonInstances { get; } = [];
    public Lazy<FrozenSet<Type>> DisposableRecords { get; private set; } = null!;
    public Lazy<FrozenSet<Type>> AsyncDisposableRecords { get; private set; } = null!;

    private Lazy<IScopedProvider> RootScopedProvider { get; set; } = null!; // set in FromCollection

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
        
        // The following lazy's need the container to work correctly
        container.RootScopedProvider = new Lazy<IScopedProvider>(() => new ScopedProvider(container));
        container.DisposableRecords = new Lazy<FrozenSet<Type>>(() => container.ServiceRecords.Values.Where(record => record.Disposal == FrozenServiceRecord.DisposalType.Disposable).Select(record => record.ServiceType).ToFrozenSet());
        container.AsyncDisposableRecords = new Lazy<FrozenSet<Type>>(() => container.ServiceRecords.Values.Where(record => record.Disposal == FrozenServiceRecord.DisposalType.AsyncDisposable).Select(record => record.ServiceType).ToFrozenSet());

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
    public IScopedProvider GetRootScopedProvider() => RootScopedProvider.Value;
    
    public TService GetSingletonService<TService>(FrozenServiceRecord record) where TService : class {
        return (TService)SingletonInstances.GetOrAdd(record.ServiceType,
            valueFactory: static (type, container) => {
                // Logic for non-generic service
                if (container.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? record))
                    return record.GetFactory<TService>().Invoke(container.RootScopedProvider.Value);
                
                // Logic for generic service
                FrozenServiceRecord genericRecord = container.ServiceRecords[type.GetGenericTypeDefinition()];
                return container.ResolveGenericService<TService>(genericRecord, container.RootScopedProvider.Value);
            },
            factoryArgument: this
        );
    }

    internal TService ResolveGenericService<TService>(FrozenServiceRecord record, IScopedProvider serviceProvider) where TService : class 
        => ResolveGenericServiceFactory<TService>(record) switch {
            Func<IScopedProvider, TService> directFactory => directFactory(serviceProvider),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(serviceProvider),
            _ => throw new InvalidOperationException("Could not resolve factory for generic service.")
        };

    internal Delegate ResolveGenericServiceFactory<TService>(FrozenServiceRecord record) {
        if (record.GenericService == FrozenServiceRecord.GenericServiceState.ClosedGeneric) return record.ImplementationFactory;
        return GetGenericRecord(typeof(TService)).ImplementationFactory;
    }
    
    private FrozenServiceRecord GetGenericRecord(Type typeOfService) {
        return ClosedGenericRecords.GetOrAdd(typeOfService,
            valueFactory: static (type, container) => FrozenServiceRecordHelper.CreateClosedGenericRecord(
                type, 
                container.ServiceRecords[type.GetGenericTypeDefinition()]
            ),
            factoryArgument: this
        );
    }

    internal bool TryResolveRecord<TService>([NotNullWhen(true)] out FrozenServiceRecord? record) where TService : class {
        // Resolve the record and try and create the instance :  IService(...args)
        if (ServiceRecords.TryGetValue(typeof(TService), out record)) return true;

        // ReSharper disable once InvertIf
        // Check for open generic services : IService<T0,.. generic args>(...args)
        if (typeof(TService).IsGenericType) {
            record = GetGenericRecord(typeof(TService));
            return true;
        }

        // If all else has failed
        record = null;
        return false;
    }
    
    

    #region IEnumerable<FrozenServiceRecord>
    public IEnumerator<FrozenServiceRecord> GetEnumerator() => ServiceRecords.Values.ToBuilder().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => ServiceRecords.Count;
    #endregion
}
