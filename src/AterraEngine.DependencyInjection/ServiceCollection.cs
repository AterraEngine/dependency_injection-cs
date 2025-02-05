// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceTypes;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceCollection : IServiceCollection {
    private ConcurrentDictionary<Type, IServiceRecord> ServiceRecords { get; } = new();

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    #region AddService
    public IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class => AddService<TImplementation, TImplementation>(scopeLevel);
    public IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService {
        ServiceRecords.AddOrUpdate(
            typeof(TService),
            addValueFactory: _ => ServiceRecordReflectionFactory.CreateWithFactory<TService, TImplementation>(scopeLevel),
            updateValueFactory: (_, _) => ServiceRecordReflectionFactory.CreateWithFactory<TService, TImplementation>(scopeLevel)
        );
        return this;
    }
    
    public IServiceCollection AddService(IServiceRecord record) {
        ServiceRecords.AddOrUpdate(record.ServiceType, record, updateValueFactory: (_, _) => record);
        return this;
    }
    
    public IServiceCollection AddServiceFromFactory<TService>(Func<IScopedProvider, TService> factory, int scopeLevel) where TService : class
        => AddService(new ServiceRecord<TService>(typeof(TService), typeof(TService), factory, scopeLevel));

    public IServiceCollection AddServiceFromFactory<TService, TFactoryService>(int scopeLevel) where TService : class where TFactoryService : class, IFactoryService<TService> {
        if (ServiceRecords.ContainsKey(typeof(TFactoryService))) AddService<TFactoryService>(scopeLevel);
        return AddService(new ServiceRecord<TService>(
            typeof(TService),
            typeof(TService),
            async static provider => (await provider.GetRequiredServiceAsync<TFactoryService>()).Create(), scopeLevel)
        );
    }
    public IServiceCollection AddServiceFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory, int scopeLevel) where TService : class
        => AddService(new ServiceRecord<TService>(typeof(TService), typeof(TService), factory, scopeLevel));

    public IServiceCollection AddServiceFromAsyncFactory<TService, TAsyncFactoryService>(int scopeLevel) where TService : class where TAsyncFactoryService : class, IAsyncFactoryService<TService> {
        if (ServiceRecords.ContainsKey(typeof(TAsyncFactoryService))) AddService<TAsyncFactoryService>(scopeLevel);
        return AddService(new ServiceRecord<TService>(
            typeof(TService),
            typeof(TService),
            async static provider => await (await provider.GetRequiredServiceAsync<TAsyncFactoryService>()).CreateAsync(),
            scopeLevel)
        );
    }

    #region AddService by Type argument
    private readonly Lazy<MethodInfo> _addServiceMethod1 = new(static () => typeof(ServiceCollection)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(AddService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1));

    public IServiceCollection AddService(Type implementation, int scopeLevel) =>
        _addServiceMethod1.Value
            .MakeGenericMethod(implementation)
            .Invoke(this, [scopeLevel]) as IServiceCollection
        ?? throw new InvalidOperationException();

    private readonly Lazy<MethodInfo> _addServiceMethod2 = new(static () => typeof(ServiceCollection)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(AddService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 2));

    public IServiceCollection AddService(Type service, Type implementation, int scopeLevel) =>
        _addServiceMethod2.Value
            .MakeGenericMethod(service, implementation)
            .Invoke(this, [scopeLevel]) as IServiceCollection
        ?? throw new InvalidOperationException();
    #endregion
    #endregion

    #region AddSingleton
    public IServiceCollection AddSingleton<TImplementation>() where TImplementation : class
        => AddSingleton<TImplementation, TImplementation>();
    
    public IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService 
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.Singleton);
    
    public IServiceCollection AddSingleton(Type implementation)
        => AddService(implementation, (int)DefaultScopeDepth.Singleton);
    
    public IServiceCollection AddSingleton(Type service, Type implementation) 
        => AddService(service, implementation, (int)DefaultScopeDepth.Singleton);

    public IServiceCollection AddSingletonFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.Singleton);
    
    public IServiceCollection AddSingletonFromFactoryy<TService, TFactoryService>() where TService : class where TFactoryService : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactoryService>((int)DefaultScopeDepth.Singleton);
    public IServiceCollection AddSingletonFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class => throw new NotImplementedException();
    public IServiceCollection AddSingletonFromAsyncFactoryy<TService, TAsyncFactoryService>() where TService : class where TAsyncFactoryService : class, IAsyncFactoryService<TService> => throw new NotImplementedException();
    #endregion

    #region AddTransient
    public IServiceCollection AddTransient<TImplementation>() where TImplementation : class 
        => AddTransient<TImplementation, TImplementation>();
    
    public IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService 
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.Transient);
    
    public IServiceCollection AddTransient(Type implementation) 
        => AddService(implementation, (int)DefaultScopeDepth.Transient);
    
    public IServiceCollection AddTransient(Type service, Type implementation) 
        => AddService(service, implementation, (int)DefaultScopeDepth.Transient);
    
    public IServiceCollection AddTransientFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class 
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.Transient);
    
    public IServiceCollection AddTransientFromFactory<TService, TFactoryService>() where TService : class where TFactoryService : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactoryService>((int)DefaultScopeDepth.Transient);
    public IServiceCollection AddTransientFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class => throw new NotImplementedException();
    public IServiceCollection AddTransientFromAsyncFactory<TService, TAsyncFactoryService>() where TService : class where TAsyncFactoryService : class, IAsyncFactoryService<TService> => throw new NotImplementedException();
    #endregion

    #region AddScoped
    public IServiceCollection AddScoped<TImplementation>() where TImplementation : class 
        => AddScoped<TImplementation, TImplementation>();
    
    public IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService
        => AddService<TService, TImplementation>((int)DefaultScopeDepth.ProviderScoped);
    
    public IServiceCollection AddScoped(Type implementation) 
        => AddService(implementation, (int)DefaultScopeDepth.ProviderScoped);
    
    public IServiceCollection AddScoped(Type service, Type implementation) 
        => AddService(service, implementation, (int)DefaultScopeDepth.ProviderScoped);
    
    public IServiceCollection AddScopedFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class 
        => AddServiceFromFactory(factory, (int)DefaultScopeDepth.ProviderScoped);
    
    public IServiceCollection AddScopedFromFactory<TService, TFactoryService>() where TService : class where TFactoryService : class, IFactoryService<TService>
        => AddServiceFromFactory<TService, TFactoryService>((int)DefaultScopeDepth.ProviderScoped);
    public IServiceCollection AddScopedFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class => throw new NotImplementedException();
    public IServiceCollection AddScopedFromAsyncFactory<TService, TAsyncFactoryService>() where TService : class where TAsyncFactoryService : class, IAsyncFactoryService<TService> => throw new NotImplementedException();
    #endregion

    #region ICollection<IServiceRecord>
    public IEnumerator<IServiceRecord> GetEnumerator() => ServiceRecords.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(IServiceRecord item) {
        if (ServiceRecords.TryAdd(item.ServiceType, item)) return;

        throw new InvalidOperationException("Service already exists");
    }

    public void Clear() => ServiceRecords.Clear();

    public bool Contains(IServiceRecord item) {
        if (!ServiceRecords.TryGetValue(item.ServiceType, out IServiceRecord? record)) return false;

        return record == item;
    }

    public void CopyTo(IServiceRecord[] array, int arrayIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex + ServiceRecords.Count > array.Length) throw new ArgumentException("The array does not have enough space to copy the elements.");

        ServiceRecords.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(IServiceRecord item) {
        if (!ServiceRecords.TryGetValue(item.ServiceType, out IServiceRecord? record)) return false;
        if (record != item) return false;

        return !ServiceRecords.TryRemove(item.ServiceType, out IServiceRecord? _);
    }

    public int Count => ServiceRecords.Count;
    public bool IsReadOnly => false;
    #endregion

    public IScopedProvider Build() => new ScopedProvider(ServiceContainer.FromCollection(ServiceRecords));
}
