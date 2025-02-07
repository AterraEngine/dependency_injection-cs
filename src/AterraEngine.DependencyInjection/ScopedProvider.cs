// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ScopedProvider(ServiceContainer serviceContainer) : IScopedProvider, IReadOnlyCollection<FrozenServiceRecord> {
    internal ScopedProvider? ParentScope { get; private set; }
    private int ScopeDepth { get; init; }

    internal ConcurrentDictionary<Type, object?> Instances { get; } = new();
    internal ConcurrentBag<IScopedProvider> ChildScopes { get; } = [];
    private readonly ConcurrentDictionary<Type, Delegate?> _transientFactoriesCache = new();
    private (Type ServiceType, Delegate? factory)? LastUsedTransientService { get; set; }
    
    internal ServiceContainer ServiceContainer { get; } = serviceContainer;

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
        FrozenServiceRecord? record = ServiceContainer.ResolveRecord<TService>();
        if (record is null) return null;

        // Record could be established, so try and create the instance :  IService(...args)
        if (ResolveServiceInstance<TService>(record) is {} instance) return instance;

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeof(TService) == typeof(IScopedProvider)) return this as TService;
        if (typeof(TService) == typeof(IServiceContainer)) return ServiceContainer as TService;

        return null;
    }

    public TService GetRequiredService<TService>() where TService : class {
        if (GetService<TService>() is {} instance) return instance;
        throw CouldNotBeResolvedException.Create<TService>();
    }
    
    private TService? ResolveServiceInstance<TService>(FrozenServiceRecord record) where TService : class {
        return record.Depth switch {
            FrozenServiceRecord.KnownScopeDepth.ProviderScoped => ResolveProviderScoped<TService>(record),
            FrozenServiceRecord.KnownScopeDepth.Singleton => ServiceContainer.GetSingletonService<TService>(record, this),
            FrozenServiceRecord.KnownScopeDepth.Transient => ResolveTransient<TService>(),
            FrozenServiceRecord.KnownScopeDepth.CustomScoped => ResolveCustomScoped<TService>(record),
            _ => null
        };
    }

    private TService? ResolveProviderScoped<TService>(FrozenServiceRecord record) where TService : class {
        return Instances.GetOrAdd(record.ServiceType,
            valueFactory: static (type, provider) => {
                // Logic for non-generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? r)) {
                    return  r.GetFactory<TService>().Invoke(provider);
                }

                // Logic for generic service
                if (provider.ServiceContainer.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out r) && r.GenericService != FrozenServiceRecord.GenericServiceState.None) {
                    return provider.ServiceContainer.ResolveGenericService<TService>(r, provider);
                }
                
                return null;
            },
            this) as TService;
    }

    private TService? ResolveTransient<TService>() where TService : class {
        if (LastUsedTransientService is {ServiceType: TService, factory: {} @delegate} ) {
            // Reuse the last used transient service
            return @delegate.DynamicInvoke(this) as TService;
        }
        
        Delegate? factory = _transientFactoriesCache.GetOrAdd(typeof(TService),
            valueFactory: static (type, container) => {
                // Logic for non-generic service
                if (container.ServiceRecords.TryGetValue(type, out FrozenServiceRecord? r)) {
                    return  r.GetFactory<TService>();
                }
                
                // Logic for generic service
                if (container.ServiceRecords.TryGetValue(type.GetGenericTypeDefinition(), out FrozenServiceRecord? genericRecord) && genericRecord.GenericService != FrozenServiceRecord.GenericServiceState.None) {
                    return container.ResolveGenericServiceFactory<TService>(genericRecord);
                }
                
                return null;
            },
            ServiceContainer);
        
        LastUsedTransientService = (typeof(TService), factory);
        return factory switch {
            Func<IScopedProvider, TService> directFactory => directFactory(this),
            Func<IScopedProvider, object> objectFactory => objectFactory(this) as TService,
            _ => null
        };
    }

    private TService? ResolveCustomScoped<TService>(FrozenServiceRecord record) where TService : class {
        ScopedProvider? currentScope = this;

        while (currentScope != null) {
            if (record.ScopeDepth == currentScope.ScopeDepth) return currentScope.ResolveProviderScoped<TService>(record);
            if (record.ScopeDepth > currentScope.ScopeDepth) break;

            currentScope = currentScope.ParentScope;
        }

        return null;
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

    #region IEnumerable<FrozenServiceRecord>
    public IEnumerator<FrozenServiceRecord> GetEnumerator() => ServiceContainer.ServiceRecords.Values.ToBuilder().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => ServiceContainer.ServiceRecords.Count;
    #endregion

    #region Dispose
    public void Dispose() {
        try {
            foreach ((Type recordId, object? instance) in Instances) {
                if (ServiceContainer.AsyncDisposableRecords.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }

                if (ServiceContainer.DisposableRecords.Contains(recordId) && instance is IDisposable disposable) {
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
                if (ServiceContainer.AsyncDisposableRecords.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }

                if (ServiceContainer.DisposableRecords.Contains(recordId) && instance is IDisposable disposable) {
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
