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

    internal ConcurrentDictionary<Guid, object> Instances { get; } = new();

    internal ConcurrentBag<IScopedProvider> ChildScopes { get; } = [];

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------=
    #region GetService by Type argument
    private readonly ConcurrentDictionary<Type, MethodInfo> _getServiceMethodCache = new();

    private readonly Lazy<MethodInfo> _getServiceMethod = new(static () => typeof(ScopedProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(GetService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1));

    public object? GetService(Type service) =>
        _getServiceMethodCache
            .GetOrAdd(service, valueFactory: _ => _getServiceMethod.Value.MakeGenericMethod(service))// get or store to cache
            .Invoke(this, null);

    public object GetRequiredService(Type service) =>
        GetService(service) ?? throw new CouldNotBeResolvedException($"The required service of type '{service}' could not be resolved.");
    #endregion

    #region GetServices by Generic Type argument
    public TService? GetService<TService>() where TService : class {
        Type typeOfService = typeof(TService);
        // Resolve the record and try and create the instance
        if (serviceContainer.ServiceRecords.TryGetValue(typeOfService, out FrozenServiceRecord record)) {
            record.ThrowIfDeeperScopeRequired(ScopeDepth);
            return ResolveServiceInstance<TService>(record);
        }

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeOfService == typeof(IScopedProvider) || typeOfService == typeof(IScopedProvider)) return (TService)(object)this;
        if (typeOfService == typeof(IServiceContainer)) return serviceContainer as TService;

        return null;// if all fails, return null
    }

    private TService? ResolveServiceInstance<TService>(FrozenServiceRecord record)
        where TService : class {

        switch (record.Depth) {
            case FrozenServiceRecord.KnownScopeDepth.ProviderScoped: {
                if (Instances.TryGetValue(record.Id, out object? cachedInstance)) return cachedInstance as TService;
                if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? scopedFactory)) return null;

                TService instance = scopedFactory(this);
                if (!Instances.TryAdd(record.Id, instance)) return null;

                return instance;
            }

            case FrozenServiceRecord.KnownScopeDepth.Singleton: {
                return serviceContainer.GetSingletonService<TService>(record, this);
            }

            case FrozenServiceRecord.KnownScopeDepth.Transient: {
                if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? transientFactory)) return null;

                return transientFactory(this);
            }

            case FrozenServiceRecord.KnownScopeDepth.CustomScoped: {
                // Scope deeper than current one is handled first
                if (record.ScopeDepth != ScopeDepth) return ParentScope?.ResolveServiceInstance<TService>(record);

                goto case FrozenServiceRecord.KnownScopeDepth.ProviderScoped;
            }

            default: return null;
        }
    }

    public TService GetRequiredService<TService>() where TService : class {
        Type typeOfService = typeof(TService);

        if (serviceContainer.ServiceRecords.TryGetValue(typeof(TService), out FrozenServiceRecord record)) {
            record.ThrowIfDeeperScopeRequired(ScopeDepth);
            return ResolveRequiredServiceInstance<TService>(record);
        }

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeOfService == typeof(IScopedProvider) || typeOfService == typeof(IScopedProvider)) return (TService)(object)this;
        if (typeOfService == typeof(IServiceContainer)) return (serviceContainer as TService)!;

        throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.");
    }

    private TService ResolveRequiredServiceInstance<TService>(FrozenServiceRecord record) where TService : class {
        switch (record.Depth) {
            case FrozenServiceRecord.KnownScopeDepth.ProviderScoped:
                if (Instances.TryGetValue(record.Id, out object? cachedInstance)) return (cachedInstance as TService)!;
                if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? scopedFactory)) goto default;

                TService instance = scopedFactory(this);
                Instances.TryAdd(record.Id, instance);
                return instance;

            case FrozenServiceRecord.KnownScopeDepth.Singleton:
                return serviceContainer.GetRequiredSingletonService<TService>(record, this);

            case FrozenServiceRecord.KnownScopeDepth.Transient:
                if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? transientFactory)) goto default;
                return transientFactory(this);

            case FrozenServiceRecord.KnownScopeDepth.CustomScoped:
                if (record.ScopeDepth > ScopeDepth) {
                    if (ParentScope is null) goto default;
                    return ParentScope.ResolveRequiredServiceInstance<TService>(record);
                }

                goto case FrozenServiceRecord.KnownScopeDepth.ProviderScoped;

            default:
                throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.");
        }
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

    private ScopedProvider NewScopeProvider(int scopeLevel) => new(serviceContainer) {
        ParentScope = this,
        ScopeDepth = scopeLevel
    };
    #endregion

    #region IEnumerable<FrozenServiceRecord>
    public IEnumerator<FrozenServiceRecord> GetEnumerator() => serviceContainer.ServiceRecords.Values.ToBuilder().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => serviceContainer.ServiceRecords.Count;
    #endregion

    #region Dispose
    public void Dispose() {
        try {
            foreach ((Guid recordId, object instance) in Instances) {
                if (serviceContainer.AsyncDisposableRecords.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }

                if (serviceContainer.DisposableRecords.Contains(recordId) && instance is IDisposable disposable) {
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

            foreach ((Guid recordId, object instance) in Instances) {
                if (serviceContainer.AsyncDisposableRecords.Contains(recordId) && instance is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }

                if (serviceContainer.DisposableRecords.Contains(recordId) && instance is IDisposable disposable) {
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
