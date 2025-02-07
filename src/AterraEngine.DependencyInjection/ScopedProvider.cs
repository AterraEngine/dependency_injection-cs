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
        FrozenServiceRecord record = ResolveRecord<TService>(throwOnNull: false);
        if (record == FrozenServiceRecord.Empty) return null;
        if (ResolveServiceInstance<TService>(record, false) is {} instance) return instance;

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeof(TService) == typeof(IScopedProvider)) return this as TService;
        if (typeof(TService) == typeof(IServiceContainer)) return serviceContainer as TService;

        return null;
    }

    public TService GetRequiredService<TService>() where TService : class {
        FrozenServiceRecord record = ResolveRecord<TService>(throwOnNull: false);
        if (record == FrozenServiceRecord.Empty) throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.");

        if (ResolveServiceInstance<TService>(record, false) is {} instance) return instance;

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (typeof(TService) == typeof(IScopedProvider)) return (this as TService)!;
        if (typeof(TService) == typeof(IServiceContainer)) return (serviceContainer as TService)!;

        throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.");
    }

    private FrozenServiceRecord ResolveRecord<TService>(bool throwOnNull) where TService : class {
        Type typeOfService = typeof(TService);
        // Resolve the record and try and create the instance :  IService(...args)
        if (serviceContainer.ServiceRecords.TryGetValue(typeOfService, out FrozenServiceRecord record)) {
            if (record.ScopeDepth <= ScopeDepth) return record;

            if (throwOnNull) throw new DeeperScopeRequiredException($"Required scope's depth {ScopeDepth} is deeper than the current scope's depth of {record.ScopeDepth}");

            return FrozenServiceRecord.Empty;
        }

        // ReSharper disable once InvertIf
        // Check for open generic services : IService<T0,.. generic args>(...args)
        if (typeOfService.IsGenericType && serviceContainer.TryGetGenericRecord(typeOfService, out FrozenServiceRecord closedRecord)) {
            if (closedRecord.ScopeDepth <= ScopeDepth) return closedRecord;

            if (throwOnNull) throw new DeeperScopeRequiredException($"Required scope's depth {ScopeDepth} is deeper than the current scope's depth of {closedRecord.ScopeDepth}");

            return FrozenServiceRecord.Empty;
        }

        // If all else has failed
        return FrozenServiceRecord.Empty;
    }

    private TService? ResolveServiceInstance<TService>(FrozenServiceRecord record, bool throwOnNull) where TService : class {
        return record.Depth switch {
            FrozenServiceRecord.KnownScopeDepth.ProviderScoped => ResolveProviderScoped<TService>(record, throwOnNull),
            FrozenServiceRecord.KnownScopeDepth.Singleton => ResolveSingleton<TService>(record, throwOnNull),
            FrozenServiceRecord.KnownScopeDepth.Transient => ResolveTransient<TService>(record, throwOnNull),
            FrozenServiceRecord.KnownScopeDepth.CustomScoped => ResolveCustomScoped<TService>(record, throwOnNull),
            _ => null
        };
    }

    private TService? ResolveProviderScoped<TService>(FrozenServiceRecord record, bool throwOnNull) where TService : class {
        if (Instances.TryGetValue(record.Id, out object? cachedInstance)) {
            if (cachedInstance is null && throwOnNull) throw new CouldNotBeResolvedException($"The service of type '{typeof(TService)}' was cached as null.");

            return cachedInstance as TService;
        }

        if (!record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? scopedFactory)) {
            if (throwOnNull) throw CouldNotBeResolvedException.Create<TService>();

            return null;
        }

        // Re-check if the instance was added, if not, then it was already added by another thread;
        TService instance = scopedFactory(this);
        if (Instances.TryAdd(record.Id, instance)) return instance;

        return Instances[record.Id] as TService;
    }

    private TService? ResolveSingleton<TService>(FrozenServiceRecord record, bool throwOnNull) where TService : class {
        if (serviceContainer.GetSingletonService<TService>(record, this) is {} singleton) return singleton;

        if (throwOnNull) throw CouldNotBeResolvedException.Create<TService>();

        return null;
    }

    private TService? ResolveTransient<TService>(FrozenServiceRecord record, bool throwOnNull) where TService : class {
        if (record.TryGetFactory<TService>(out Func<IScopedProvider, TService>? transientFactory)) return transientFactory(this);

        if (throwOnNull) throw CouldNotBeResolvedException.Create<TService>();

        return null;

    }

    private TService? ResolveCustomScoped<TService>(FrozenServiceRecord record, bool throwOnNull) where TService : class {
        if (record.ScopeDepth == ScopeDepth) return ResolveProviderScoped<TService>(record, throwOnNull);

        if (record.ScopeDepth > ScopeDepth) throw new DeeperScopeRequiredException($"Required scope's depth {ScopeDepth} is deeper than the current scope's depth of {record.ScopeDepth}");

        if (ParentScope?.ResolveServiceInstance<TService>(record, throwOnNull) is {} resolvedFromParent) return resolvedFromParent;

        if (throwOnNull) throw new CouldNotBeResolvedException($"The service of type '{typeof(TService)}' could not be resolved in custom scope.");

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
