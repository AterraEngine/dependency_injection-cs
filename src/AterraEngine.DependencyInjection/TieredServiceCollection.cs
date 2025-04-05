// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using AterraEngine.DependencyInjection.Services;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class TieredServiceCollection : ITieredServiceCollection {

    private static readonly Lazy<MethodInfo[]> ServiceCollectionMethods = new(() => typeof(TieredServiceCollection)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public));
    
    private readonly Lazy<MethodInfo> _addServiceMethodByServiceAndImplementationTypes = new(static () => ServiceCollectionMethods.Value
        .Single(m => m is { Name: nameof(AddService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 2));

    internal ConcurrentDictionary<Type, IServiceRecord> Records { get; } = new();

    #if DEBUG
    [UsedImplicitly] internal ConcurrentStack<IServiceRecord> DiscardedRecords { get; } = new();
    #endif
    
    internal bool HasDisposalRecords { get; private set; }
    internal bool HasAsyncDisposalRecords { get; private set; }

    public int Count => Records.Count;
    public bool IsReadOnly { get; private set; }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public virtual ITieredServiceProvider Build() {
        ITieredServiceContainer container = TieredServiceContainer.FromCollection(this);
        IsReadOnly = true;
        
        // If we just return "provider" this will be the container's ROOT provider
        //      This is something we don't want as that one should only be used to resolve transients and singletons
        return container.GetRootProvider();
    }

    private void ThrowIfReadOnly() {
        if (IsReadOnly) throw new InvalidOperationException("Collection is read only");
    }

    #region AddService
    public ITieredServiceCollection AddService<TImplementation>(int ServiceDepth) where TImplementation : class => AddService<TImplementation, TImplementation>(ServiceDepth);
    public ITieredServiceCollection AddService<TService, TImplementation>(int ServiceDepth) where TImplementation : class, TService {
        Add(new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            ConstructorReflectionFactory.CreateFunc<TService>(typeof(TImplementation)),
            ServiceDepth
        ));

        return this;
    }

    public ITieredServiceCollection AddService(IServiceRecord record) {
        Add(record);
        return this;
    }

    public ITieredServiceCollection AddServiceFromFactory<TService>(Func<ITieredServiceProvider, TService> factory, int ServiceDepth) where TService : class
        => AddService(new ServiceRecord<TService>(typeof(TService), typeof(TService), factory, ServiceDepth));

    public ITieredServiceCollection AddServiceFromFactory<TService, TFactory>(int ServiceDepth) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService>(
            factory: static provider => provider.GetRequiredService<TFactory>().Create(provider),
            ServiceDepth
        );

    public ITieredServiceCollection AddService<TService>(TService instance, int ServiceDepth) where TService : class {
        if (ServiceDepth is not (int)DefaultServiceDepth.Singleton) throw new InvalidOperationException("Scope level must be Singleton for it to be registered from an object instance.");
        return AddService(new InstanceServiceRecord<TService>(instance, ServiceDepth));
    }

    #region AddService by Type argument
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddService(Type implementation, int ServiceDepth) {
        // Handle open generic type registration
        if (implementation.IsGenericTypeDefinition) {
            Add(new GenericServiceRecord(implementation, implementation, ServiceDepth));
            return this;
        }
        
        // Handle normal registration for closed types
        var result = _addServiceMethodByServiceAndImplementationTypes.Value
            .MakeGenericMethod(implementation)
            .Invoke(this, [ServiceDepth]) as ITieredServiceCollection;

        return result ?? throw new InvalidOperationException();
    }

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddService(Type service, Type implementation, int ServiceDepth) {
        ThrowIfReadOnly();

        // Verify the type arguments are either both open or closed
        if (implementation.IsGenericTypeDefinition != service.IsGenericTypeDefinition) {
            throw new InvalidOperationException(
                "Cannot register a closed implementation type with an open service type, or vice versa."
            );
        }

        // Handle open generic type registration
        if (service.IsGenericTypeDefinition && implementation.IsGenericTypeDefinition) {
            if (!service.IsInterface) throw new InvalidOperationException($"Open generic service type '{service}' must be an interface.");
            Add(new GenericServiceRecord(service, implementation, ServiceDepth));
            return this;
        }

        // Handle normal registration for closed types
        var result = _addServiceMethodByServiceAndImplementationTypes.Value
            .MakeGenericMethod(service, implementation)
            .Invoke(this, [ServiceDepth]) as ITieredServiceCollection;

        return result ?? throw new InvalidOperationException();
    }
    #endregion
    #endregion

    #region AddSingleton
    public ITieredServiceCollection AddSingleton<TImplementation>() where TImplementation : class
        => AddSingleton<TImplementation, TImplementation>();

    public ITieredServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultServiceDepth.Singleton);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddSingleton(Type implementation)
        => AddService(implementation, (int)DefaultServiceDepth.Singleton);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddSingleton(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultServiceDepth.Singleton);

    public ITieredServiceCollection AddSingletonFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultServiceDepth.Singleton);

    public ITieredServiceCollection AddSingletonFromFactory<TService, TFactory>(int? ServiceDepthFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultServiceDepth.Singleton);
    
    public ITieredServiceCollection AddSingleton<TService>(TService instance) where TService : class 
        => AddService(instance, (int)DefaultServiceDepth.Singleton);
    #endregion

    #region AddTransient
    public ITieredServiceCollection AddTransient<TImplementation>() where TImplementation : class
        => AddTransient<TImplementation, TImplementation>();

    public ITieredServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultServiceDepth.Transient);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddTransient(Type implementation)
        => AddService(implementation, (int)DefaultServiceDepth.Transient);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddTransient(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultServiceDepth.Transient);

    public ITieredServiceCollection AddTransientFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultServiceDepth.Transient);

    public ITieredServiceCollection AddTransientFromFactory<TService, TFactory>(int? ServiceDepthFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultServiceDepth.Transient);
    #endregion

    #region AddScoped
    public ITieredServiceCollection AddScoped<TImplementation>() where TImplementation : class
        => AddScoped<TImplementation, TImplementation>();

    public ITieredServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultServiceDepth.ProviderScoped);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddScoped(Type implementation)
        => AddService(implementation, (int)DefaultServiceDepth.ProviderScoped);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public ITieredServiceCollection AddScoped(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultServiceDepth.ProviderScoped);

    public ITieredServiceCollection AddScopedFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultServiceDepth.ProviderScoped);

    public ITieredServiceCollection AddScopedFromFactory<TService, TFactory>(int? ServiceDepthFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultServiceDepth.ProviderScoped);
    #endregion

    #region AddEnumerableService
    private void AddOrUpdateEnumerableServiceRecord<TService, TImplementation>(int ServiceDepth) where TImplementation : class, TService {
        Records.AddOrUpdate(
            typeof(IEnumerable<TService>),
            // Add case: when the key does not exist, insert the new item
            _ => {
                var record = new EnumerableServiceRecord<TService>(ServiceDepth);
                record.AddService<TImplementation>();
                return record;
            },

            // Update case: when the key already exists, handle the old value
            (_, record) => {
                if (record is not EnumerableServiceRecord<TService> enumerableRecord) throw new InvalidOperationException("The record is not an enumerable record.");
                if (record.ServiceDepth != ServiceDepth) throw new InvalidOperationException("The record's scope depth does not match the partial record's scope depth.");
                enumerableRecord.AddService<TImplementation>();
                return record;
            }
        );
    }
    
    public ITieredServiceCollection AddEnumerableService<TService, TImplementation>(int ServiceDepth) where TImplementation : class, TService {
        // The partial record can be added directly
        Add(new PartialEnumerableServiceRecord<TImplementation>(
            ConstructorReflectionFactory.CreateFunc<TImplementation>(typeof(TImplementation)),
            ServiceDepth
        ));
        
        // The actual enumerable service is a little bit more complicated
        //      Instead of relying on Add() method, this implements its own
        AddOrUpdateEnumerableServiceRecord<TService, TImplementation>(ServiceDepth);

        return this;
    }
    
    public ITieredServiceCollection AddEnumerableService<TService, TImplementation>(TImplementation instance, int ServiceDepth) where TImplementation : class, TService {
        // Register the service as an instance
        AddService(instance, ServiceDepth);
        AddOrUpdateEnumerableServiceRecord<TService, TImplementation>(ServiceDepth);
        return this;
    }

    public ITieredServiceCollection AddEnumerableSingleton<TService, TImplementation>() where TImplementation : class, TService
        => AddEnumerableService<TService, TImplementation>((int)DefaultServiceDepth.Singleton);
    
    public ITieredServiceCollection AddEnumerableSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : class, TService
        => AddEnumerableService<TService, TImplementation>(instance, (int)DefaultServiceDepth.Singleton);
    
    public ITieredServiceCollection AddEnumerableTransient<TService, TImplementation>() where TImplementation : class, TService
        => AddEnumerableService<TService, TImplementation>((int)DefaultServiceDepth.Transient);
    
    public ITieredServiceCollection AddEnumerableScoped<TService, TImplementation>() where TImplementation : class, TService
        => AddEnumerableService<TService, TImplementation>((int)DefaultServiceDepth.ProviderScoped);
    #endregion
    
    #region ICollection<IServiceRecord>
    public IEnumerator<IServiceRecord> GetEnumerator() => Records.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(IServiceRecord item) {
        ThrowIfReadOnly();

        Records.AddOrUpdate(item.ServiceType, 
            // Add case: when the key does not exist, insert the new item
            _ => item, 

            // Update case: when the key already exists, handle the old value
            (_, [UsedImplicitly] oldServiceRecord) => {
                #if DEBUG
                DiscardedRecords.Push(oldServiceRecord);
                #endif
                return item; // Replace with the new item
            });

        // Update the disposal flags
        HasDisposalRecords |= item.IsDisposable;
        HasAsyncDisposalRecords |= item.IsAsyncDisposable;

    }

    public void Clear() {
        ThrowIfReadOnly();
        Records.Clear();
    }

    public bool Contains(IServiceRecord item) => Records.TryGetValue(item.ServiceType, out IServiceRecord? record) && record == item;

    public void CopyTo(IServiceRecord[] array, int arrayIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex + Records.Count > array.Length) throw new ArgumentException("The array does not have enough space to copy the elements.");

        Records.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(IServiceRecord item) {
        ThrowIfReadOnly();
        if (!Records.TryGetValue(item.ServiceType, out IServiceRecord? record)) return false;
        if (record != item) return false;

        return !Records.TryRemove(item.ServiceType, out IServiceRecord? _);
    }
    #endregion
}
