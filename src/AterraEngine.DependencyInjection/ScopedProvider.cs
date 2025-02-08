// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Concurrent;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ScopedProvider(ServiceContainer serviceContainer) : IScopedProvider {
    private readonly ConcurrentDictionary<Type, Delegate> _transientFactoriesCache = new();
    internal ScopedProvider? ParentScope { get; private set; }
    private int ScopeDepth { get; init; }

    internal ConcurrentDictionary<Type, object> Instances { get; } = new();
    internal ConcurrentBag<IScopedProvider> ChildScopes { get; } = [];

    private ServiceContainer ServiceContainer { get; } = serviceContainer;

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------=
    #region GetService by Type argument
    private ConcurrentDictionary<Type, MethodInfo> GetServiceMethodCache { get; } = [];

    private static readonly Lazy<MethodInfo> GetServiceMethod = new(static () => typeof(ScopedProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(GetService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1));

    public object? GetService(Type service) {
        MethodInfo method = GetServiceMethodCache.GetOrAdd(service, valueFactory: static type => GetServiceMethod.Value.MakeGenericMethod(type));
        return method.Invoke(this, null);
    }

    public object GetRequiredService(Type service) =>
        GetService(service) ?? throw new CouldNotBeResolvedException($"The required service of type '{service}' could not be resolved.");
    #endregion

    #region GetServices by Generic Type argument
    public TService? GetService<TService>() where TService : class {
        if (ServiceContainer.TryResolveRecord<TService>(out FrozenServiceRecord? record)) {
            return ResolveServiceByScope<TService>(record);
        }

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeof(TService) == typeof(IScopedProvider)) return this as TService;
        if (typeof(TService) == typeof(IServiceContainer)) return ServiceContainer as TService;
        return null;
    }
    
    private TService ResolveServiceByScope<TService>(FrozenServiceRecord record) where TService : class {
        return record.Depth switch {
            FrozenServiceRecord.KnownScopeDepth.ProviderScoped => ResolveProviderScoped<TService>(record),
            FrozenServiceRecord.KnownScopeDepth.Singleton => ServiceContainer.GetSingletonService<TService>(record),
            FrozenServiceRecord.KnownScopeDepth.Transient => ResolveTransient<TService>(),
            FrozenServiceRecord.KnownScopeDepth.CustomScoped => ResolveCustomScoped<TService>(record),
            _ => throw new ArgumentOutOfRangeException(nameof(record))
        };
    }

    public TService GetRequiredService<TService>() where TService : class {
        if (ServiceContainer.TryResolveRecord<TService>(out FrozenServiceRecord? record)) {
            return ResolveServiceByScope<TService>(record);
        }

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeof(TService) == typeof(IScopedProvider)) return (TService)(object)this;
        if (typeof(TService) == typeof(IServiceContainer)) return (TService)(object)ServiceContainer;
        throw CouldNotBeResolvedException.Create<TService>();
    }

    private TService ResolveProviderScoped<TService>(FrozenServiceRecord record) where TService : class {
        return (TService)Instances.GetOrAdd(record.ServiceType,
            valueFactory: static (type, provider) => {
                // Logic for non-generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? r)) {
                    return r.GetFactory<TService>().Invoke(provider);
                }

                // Logic for generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out r) && r.GenericService != FrozenServiceRecord.GenericServiceState.None) {
                    return provider.ServiceContainer.ResolveGenericService<TService>(r, provider);
                }

                throw new InvalidOperationException("Could not resolve provider scoped service.");
            },
            this);
    }

    private TService ResolveTransient<TService>() where TService : class {
        Delegate factory = _transientFactoriesCache.GetOrAdd(typeof(TService),
            valueFactory: static (type, container) => {
                // Logic for non-generic service
                if (container.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? r)) {
                    return r.GetFactory<TService>();
                }

                // Logic for generic service
                if (container.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out FrozenServiceRecord? genericRecord) && genericRecord.GenericService != FrozenServiceRecord.GenericServiceState.None) {
                    return container.ResolveGenericServiceFactory<TService>(genericRecord);
                }

                throw new InvalidOperationException("Could not resolve transient service.");
            },
            ServiceContainer);

        return factory switch {
            Func<IScopedProvider, TService> directFactory => directFactory(this),
            Func<IScopedProvider, object> objectFactory => (TService)objectFactory(this),
            _ => throw new InvalidOperationException("Could not resolve transient service.")
        };
    }

    private TService ResolveCustomScoped<TService>(FrozenServiceRecord record) where TService : class {
        ScopedProvider? currentScope = this;

        while (currentScope != null) {
            if (record.ScopeDepth == currentScope.ScopeDepth) return currentScope.ResolveProviderScoped<TService>(record);
            if (record.ScopeDepth > currentScope.ScopeDepth) break;

            currentScope = currentScope.ParentScope;
        }

        throw new InvalidOperationException("Could not resolve custom scoped service.");
    }
    #endregion

    #region Scope Creation
    public IScopedProvider CreateNewScope() {
        ScopedProvider scopedProvider = NewScopeProvider(ScopeDepth);
        ChildScopes.Add(scopedProvider);
        return scopedProvider;
    }

    public IScopedProvider CreateNewDeeperScope() {
        ScopedProvider scopedProvider = NewScopeProvider(ScopeDepth + 1);
        ChildScopes.Add(scopedProvider);
        return scopedProvider;
    }

    private ScopedProvider NewScopeProvider(int scopeLevel) => new(ServiceContainer) {
        ParentScope = this,
        ScopeDepth = scopeLevel
    };
    #endregion

    #region Dispose
    public void Dispose() {
        try {
            foreach ((Type recordId, object? instance) in Instances) {
                if (ServiceContainer.AsyncDisposableRecords.Value.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }

                if (ServiceContainer.DisposableRecords.Value.Contains(recordId) && instance is IDisposable disposable) {
                    disposable.Dispose();
                }
            }

            Instances.Clear();

            // Only ChildScopes should be disposed
            while (ChildScopes.TryTake(out IScopedProvider? childScope)) {
                childScope.Dispose();
            }
        }
        finally {
            CommonCleanup();
            GC.SuppressFinalize(this);
        }
    }

    public async ValueTask DisposeAsync() {
        try {
            // Yes I know we could do some sort of task collection and then do a Task.WhenAll()
            //      But I don't think it's worth it, because we don't expect this to be a performance bottleneck (at the moment)

            foreach ((Type recordId, object? instance) in Instances) {
                if (ServiceContainer.AsyncDisposableRecords.Value.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }

                if (ServiceContainer.DisposableRecords.Value.Contains(recordId) && instance is IDisposable disposable) {
                    disposable.Dispose();
                }
            }

            Instances.Clear();

            // Don't forget about ChildScopes!
            while (ChildScopes.TryTake(out IScopedProvider? childScope)) {
                await childScope.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally {
            CommonCleanup();
            GC.SuppressFinalize(this);
        }
    }

    private void CommonCleanup() {
        // Clear collections of all references
        //      Do know that this doesn't necessarily mark all the objects to be garbage collected.
        //      It just means that the references we hold are no longer valid.
        Instances.Clear();
        ChildScopes.Clear();
        ParentScope?.RemoveItemFromBag(this);// Remove this instance from the parent scope's bag, else we will leak memory
        ParentScope = null;// Do not dispose the parent scope, only remove the reference
    }

    private void RemoveItemFromBag(ScopedProvider child) {
        if (ChildScopes.IsEmpty) return;

        var itemsToKeep = new List<IScopedProvider>(ChildScopes.Count - 1);

        // Remove specific item, keeping others in memory
        while (ChildScopes.TryTake(out IScopedProvider? item)) {
            if (item.Equals(child)) continue;

            itemsToKeep.Add(item);
        }

        // Re-add only the items we want to keep
        foreach (IScopedProvider item in itemsToKeep) {
            ChildScopes.Add(item);
        }
    }
    #endregion
}
