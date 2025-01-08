// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using CodeOfChaos.Types;
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProvider : IServiceProvider {
    public FrozenDictionary<Type, IServiceRecord> Records { get; internal init; } = FrozenDictionary<Type, IServiceRecord>.Empty;

    private ServiceProvider? Parent { get; init; }
    private int ScopeLevel { get; init; }
    private TypedValueStore<Type> Instances { get; } = new();
    // public TypedValueStore<Type> TransientsInstances { get; internal init; } = new();
     
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public TService? GetService<TService>() where TService : class {
        if (!Records.TryGetValue(typeof(TService), out IServiceRecord? record)) return null;

        switch (record.Lifetime) {
            // check if it is a transient
            case -1:
                record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? factory);
                return factory?.Invoke(this);
            
            // Singleton, but we have a parent, so we should ask it because it could exist there;
            case 0 when Parent is not null: return Parent.GetService<TService>();
            
            // Singleton, but we are the top of the chain and we already have the instance
            case 0 when Instances.TryGetValue(record.ServiceType, out TService? instance): return instance;
            
            // Singleton, or anything that requires us to create the instance
            case 0:
            case var level when level == ScopeLevel: return CreateInstanceFromFactory<TService>(record); // EngineScope
            case var level when level < ScopeLevel: return Parent?.GetService<TService>(); // EngineScope
            
            default: throw new Exception("Scope level is higher than the current scope level");
        }
    }
    
    private TService? CreateInstanceFromFactory<TService>(IServiceRecord record) where TService : class {
        record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? factory);
        if (factory?.Invoke(this) is not {} newInstance) return null;
        if (!Instances.TryAdd(record.ServiceType, newInstance)) return null; // This feels wrong, we should throw an exception
        return newInstance; // Don't need to store to the parent here, because if there is a parent, it will be stored there.
    }

    public TService GetRequiredService<TService>() where TService : class {
        return GetService<TService>() ?? throw new InvalidOperationException($"The required service of type '{typeof(TService)}' could not be resolved.");
    }

    public IServiceProvider CreateScope() => new ServiceProvider {
        Parent = this,
        ScopeLevel = ScopeLevel + 1
    };
}
