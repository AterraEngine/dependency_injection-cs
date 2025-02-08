// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ScopedProvider(ServiceContainer serviceContainer) : IScopedProvider {

    private static readonly FrozenDictionary<Type, Func<ScopedProvider, object>> SpecialTypeResolvers = new Dictionary<Type, Func<ScopedProvider, object>> {
        { typeof(IScopedProvider), static provider => provider },
        { typeof(IServiceContainer), static provider => provider.ServiceContainer }
    }.ToFrozenDictionary();

    internal ScopedProvider? ParentScope { get; private set; }
    private int ScopeDepth { get; init; }

    internal ConcurrentDictionary<Guid, object> Instances { get; } = new();
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
        if (SpecialTypeResolvers.TryGetValue(typeof(TService), out Func<ScopedProvider, object>? func)) return (TService)func(this);
        return null;
    }

    public TService GetRequiredService<TService>() where TService : class {
        if (ServiceContainer.TryResolveRecord<TService>(out FrozenServiceRecord? record)) {
            return ResolveServiceByScope<TService>(record);
        }

        // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
        if (SpecialTypeResolvers.TryGetValue(typeof(TService), out Func<ScopedProvider, object>? func)) return (TService)func(this);
        throw CouldNotBeResolvedException.Create<TService>();
    }

    private TService ResolveServiceByScope<TService>(FrozenServiceRecord record) where TService : class
        => record.Depth switch {
            FrozenServiceRecord.KnownScopeDepth.ProviderScoped => ResolveProviderScoped<TService>(record),
            FrozenServiceRecord.KnownScopeDepth.Singleton => ServiceContainer.GetSingletonService<TService>(record.Id),
            FrozenServiceRecord.KnownScopeDepth.Transient => ServiceContainer.GetTransientService<TService>(record.Id),
            FrozenServiceRecord.KnownScopeDepth.CustomScoped => ResolveCustomScoped<TService>(record),
            _ => throw new ArgumentOutOfRangeException(nameof(record))
        };

    private TService ResolveProviderScoped<TService>(FrozenServiceRecord record) where TService : class
        => (TService)Instances.GetOrAdd(
            record.Id,
            valueFactory: static (id, provider) => provider.ServiceContainer.CreateInstance<TService>(id, provider),
            this
        );

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
            foreach ((Guid recordId, object? instance) in Instances) {
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

            foreach ((Guid recordId, object? instance) in Instances) {
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
