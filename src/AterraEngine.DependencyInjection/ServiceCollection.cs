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
public class ServiceCollection : IServiceCollection {

    private static readonly Lazy<MethodInfo[]> ServiceCollectionMethods = new(() => typeof(ServiceCollection)
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
    public IScopedProvider Build() {
        IServiceContainer container = ServiceContainer.FromCollection(this);
        IsReadOnly = true;
        
        // If we just return "provider" this will be the container's ROOT provider
        //      This is something we don't want as that one should only be used to resolve transients and singletons
        return container.GetRootScopedProvider();
    }

    private void ThrowIfReadOnly() {
        if (IsReadOnly) throw new InvalidOperationException("Collection is read only");
    }

    #region AddService
    public IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class => AddService<TImplementation, TImplementation>(scopeLevel);
    public IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService {
        Add(new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            ConstructorReflectionFactory.CreateFunc<TService>(typeof(TImplementation)),
            scopeLevel
        ));

        return this;
    }

    public IServiceCollection AddService(IServiceRecord record) {
        Add(record);
        return this;
    }

    public IServiceCollection AddServiceFromFactory<TService>(Func<IScopedProvider, TService> factory, int scopeLevel) where TService : class
        => AddService(new ServiceRecord<TService>(typeof(TService), typeof(TService), factory, scopeLevel));

    public IServiceCollection AddServiceFromFactory<TService, TFactory>(int scopeLevel) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService>(
            factory: static provider => provider.GetRequiredService<TFactory>().Create(provider),
            scopeLevel
        );

    public IServiceCollection AddService<TService>(TService instance, int scopeLevel) where TService : class {
        if (scopeLevel is not (int)DefaultScopeDepth.Singleton) throw new InvalidOperationException("Scope level must be Singleton for it to be registered from an object instance.");
        return AddService(new InstanceServiceRecord<TService>(instance, scopeLevel));
    }

    #region AddService by Type argument
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddService(Type implementation, int scopeLevel) {
        // Handle open generic type registration
        if (implementation.IsGenericTypeDefinition) {
            Add(new GenericServiceRecord(implementation, implementation, scopeLevel));
            return this;
        }
        
        // Handle normal registration for closed types
        var result = _addServiceMethodByServiceAndImplementationTypes.Value
            .MakeGenericMethod(implementation)
            .Invoke(this, [scopeLevel]) as IServiceCollection;

        return result ?? throw new InvalidOperationException();
    }

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddService(Type service, Type implementation, int scopeLevel) {
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
            Add(new GenericServiceRecord(service, implementation, scopeLevel));
            return this;
        }

        // Handle normal registration for closed types
        var result = _addServiceMethodByServiceAndImplementationTypes.Value
            .MakeGenericMethod(service, implementation)
            .Invoke(this, [scopeLevel]) as IServiceCollection;

        return result ?? throw new InvalidOperationException();
    }
    #endregion
    #endregion

    #region AddSingleton
    public IServiceCollection AddSingleton<TImplementation>() where TImplementation : class
        => AddSingleton<TImplementation, TImplementation>();

    public IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.Singleton);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddSingleton(Type implementation)
        => AddService(implementation, (int)DefaultScopeDepth.Singleton);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddSingleton(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultScopeDepth.Singleton);

    public IServiceCollection AddSingletonFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.Singleton);

    public IServiceCollection AddSingletonFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultScopeDepth.Singleton);
    
    public IServiceCollection AddSingleton<TService>(TService instance) where TService : class 
        => AddService(instance, (int)DefaultScopeDepth.Singleton);
    #endregion

    #region AddTransient
    public IServiceCollection AddTransient<TImplementation>() where TImplementation : class
        => AddTransient<TImplementation, TImplementation>();

    public IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.Transient);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddTransient(Type implementation)
        => AddService(implementation, (int)DefaultScopeDepth.Transient);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddTransient(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultScopeDepth.Transient);

    public IServiceCollection AddTransientFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.Transient);

    public IServiceCollection AddTransientFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultScopeDepth.Transient);
    #endregion

    #region AddScoped
    public IServiceCollection AddScoped<TImplementation>() where TImplementation : class
        => AddScoped<TImplementation, TImplementation>();

    public IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.ProviderScoped);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddScoped(Type implementation)
        => AddService(implementation, (int)DefaultScopeDepth.ProviderScoped);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public IServiceCollection AddScoped(Type service, Type implementation)
        => AddService(service, implementation, (int)DefaultScopeDepth.ProviderScoped);

    public IServiceCollection AddScopedFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.ProviderScoped);

    public IServiceCollection AddScopedFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TService : class where TFactory : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactory>((int)DefaultScopeDepth.ProviderScoped);
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
