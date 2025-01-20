// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using CodeOfChaos.Types;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProvider(IServiceContainer serviceContainer) : IServiceProvider {
    private ServiceProvider? ParentScope { get; init; }
    private int ScopeLevel { get; init; }
    private TypedValueStore<Type> Instances { get; } = new();
     
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetService<TService>() where TService : class {
        if (typeof(TService) == typeof(IServiceProvider)) return (TService) (object) this; // workaround to make sure we can inject the service provider
        if (!serviceContainer.Records.TryGetValue(typeof(TService), out IServiceRecord? record)) return null;

        switch (record) {
            // check if it is a transient
            case {IsTransient: true}:
                record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? factory);
                return factory?.Invoke(this);
            
            // Singleton, but we have a parent, so we should ask it because it could exist there;
            case {IsSingleton: true} when ParentScope is not null: return ParentScope.GetService<TService>();
            
            // Singleton, but we are the top of the chain and we already have the instance
            case {IsSingleton: true} when Instances.TryGetValue(record.ServiceType, out TService? instance): return instance;
            
            // Singleton, or anything that requires us to create the instance
            case {IsSingleton: true}:
            case {Lifetime: var level} when level == ScopeLevel: return CreateInstanceFromFactory<TService>(record); // EngineScope
            case {Lifetime: var level} when level < ScopeLevel: return ParentScope?.GetService<TService>(); // EngineScope
            
            // Level could not be determined, or scope was deeper than the current scope, and thus cannot be resolved
            default: throw new Exception($"Required scope level {record.Lifetime} is higher than the current scope level of {ScopeLevel}");
        }
    }

    #region GetService by Type argument
    private readonly ConcurrentDictionary<Type, MethodInfo> _getServiceMethodCache = new();
    private readonly Lazy<MethodInfo> _getServiceMethod = new(static () => typeof(ServiceProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(GetService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1));
    
    public object? GetService(Type service) =>
        _getServiceMethodCache
            .GetOrAdd(service, _ => _getServiceMethod.Value.MakeGenericMethod(service)) // get or store to cache
            .Invoke(this, null);
    #endregion

    private TService? CreateInstanceFromFactory<TService>(IServiceRecord record) where TService : class {
        record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? factory);
        if (factory?.Invoke(this) is not {} newInstance) return null;
        if (!Instances.TryAdd(record.ServiceType, newInstance)) return null; // This feels wrong, we should throw an exception
        return newInstance; // Don't need to store to the parent here, because if there is a parent, it will be stored there.
    }

    public TService GetRequiredService<TService>() where TService : class {
        return GetService<TService>() ?? throw new InvalidOperationException($"The required service of type '{typeof(TService)}' could not be resolved.");
    }

    public IServiceProvider CreateScope() => new ServiceProvider(serviceContainer) {
        ParentScope = this,
        ScopeLevel = ScopeLevel + 1
    };
    
    #region IEnumerable<IServiceRecord>
    public IEnumerator<IServiceRecord> GetEnumerator() => serviceContainer.Records.Values.ToBuilder().GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => serviceContainer.Records.Count;
    #endregion
}
