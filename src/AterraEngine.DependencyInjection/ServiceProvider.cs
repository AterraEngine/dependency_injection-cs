﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProvider(IServiceContainer serviceContainer) : IServiceProvider {
    private ServiceProvider? ParentScope { get; set; }
    private int ScopeLevel { get; init; }

    private ConcurrentDictionary<Guid, object> Instances { get; } = new();
    private ConcurrentBag<Guid> DisposableInstances { get; } = [];
    private ConcurrentBag<Guid> AsyncDisposableInstances { get; } = [];

    private ConcurrentBag<IServiceProvider> ChildScopes { get; } = [];

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------=
    #region GetService by Type argument
    private readonly ConcurrentDictionary<Type, MethodInfo> _getServiceMethodCache = new();

    private readonly Lazy<MethodInfo> _getServiceMethod = new(static () => typeof(ServiceProvider)
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
        // Do some basic checks first before we try and actually use the record.
        Type typeOfService = typeof(TService);
        if (!serviceContainer.ServiceRecords.TryGetValue(typeOfService, out IServiceRecord? record)) {
            // Record could not be established, so try and see if we are looking in calling some specific types which aren't in the container
            if (typeOfService == typeof(IServiceProvider)) return (TService)(object)this;
            if (typeOfService == typeof(IServiceContainer)) return (TService)serviceContainer;

            return null;// if all fails, return null
        }

        // Resolve the type and create an instance        
        TService? instance = null;
        switch (record) {
            // Transient, we don't track the instance, but we do track disposing patterns.
            case { IsTransient: true }:
                if (!record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? transientFactory)) break;

                instance = transientFactory(this);
                break;

            // Singleton, but we have a parent, so we should ask it because it could exist there;
            case { IsSingleton: true }: {
                instance = serviceContainer.GetSingletonService<TService>(record, this);
                break;
            }

            // Lifetime is the current scope level
            case { Lifetime: var level } when level == ScopeLevel: {
                // Check if the instance already exists, if so, return it
                if (Instances.TryGetValue(record.Id, out object? alreadyCreatedInstance)) {
                    instance = alreadyCreatedInstance as TService;
                    break;
                }

                // instance hasn't been created yet, so we need to create it
                if (!record.TryGetFactory<TService>(out Func<IServiceProvider, TService>? factory)) break;

                instance = factory(this);

                // If the instance was created, add it to the instances collection
                //      If we can't add it, then it means it was already created by something else, and thus we can just forget it ( not the best option)
                if (!Instances.TryAdd(record.Id, instance)) instance = null;
                break;
            }

            // Lifetime was shallower than the current scope level
            case { Lifetime: var level } when level < ScopeLevel: {
                return ParentScope?.GetService<TService>();

                // Okay I know you see the `RegisterDisposePatternIfApplicable` below and think "hey don't we need to
                // register the dispose pattern for this instance as well? And why aren't we assigning the instance?"
                // The main reason for this is that the ParentScope handles the new instance's lifetime, not the current one!
            }

            // Level could not be determined, or scope was deeper than the current scope, and thus cannot be resolved
            default: {
                throw new DeeperScopeRequiredException($"Required scope level {record.Lifetime} is higher than the current scope level of {ScopeLevel}", typeOfService, ScopeLevel);
            }
        }

        // If for some reason something went wrong, we can just return null
        //      TODO add some logging to this when this fails.
        if (instance is null) return null;

        // After a possible instance has been found do some more registers if needed
        RegisterDisposePatternIfApplicable(record);
        return instance;
    }

    public TService GetRequiredService<TService>() where TService : class {
        try {
            if (GetService<TService>() is not {} service) throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.");

            return service;
        }
        catch (DeeperScopeRequiredException ex) when (ex.TypeToResolve == typeof(TService)) {
            throw new CouldNotBeResolvedException($"The required service of type '{typeof(TService)}' could not be resolved.", ex);
        }
        catch (DeeperScopeRequiredException ex) when (ex.TypeToResolve != typeof(TService)) {
            throw new CouldNotBeResolvedException($"While trying to resolve {typeof(TService)} another service of type '{ex.TypeToResolve}' could not be resolved due to a scope conflict", ex);
        }
    }

    private void RegisterDisposePatternIfApplicable(IServiceRecord record) {
        switch (record) {
            case { IsAsyncDisposable: true }: AsyncDisposableInstances.Add(record.Id); break;// Async is always preferred over sync
            case { IsDisposable: true }: DisposableInstances.Add(record.Id); break;
        }
    }
    #endregion

    #region Scope Creation
    public IServiceProvider CreateScope() {
        IServiceProvider scopedProvider = NewScopeProvider(ScopeLevel);
        ChildScopes.Add(scopedProvider);
        return scopedProvider;
    }

    public IServiceProvider CreateDeeperScope() {
        IServiceProvider scopedProvider = NewScopeProvider(ScopeLevel + 1);
        ChildScopes.Add(scopedProvider);
        return scopedProvider;
    }

    private ServiceProvider NewScopeProvider(int scopeLevel) => new(serviceContainer) {
        ParentScope = this,
        ScopeLevel = scopeLevel
    };
    #endregion

    #region IEnumerable<IServiceRecord>
    public IEnumerator<IServiceRecord> GetEnumerator() => serviceContainer.ServiceRecords.Values.ToBuilder().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => serviceContainer.ServiceRecords.Count;
    #endregion

    #region Dispose
    public void Dispose() {
        try {
            // Dispose all objects marked as async disposable synchronously, if needed
            while (AsyncDisposableInstances.TryTake(out Guid instanceId)) {
                if (!Instances.TryRemove(instanceId, out object? instance)) continue;

                switch (instance) {
                    case IAsyncDisposable asyncDisposableInstance: {
                        // wait for the task to complete. I know this is bad, but it's the only way I know of
                        asyncDisposableInstance.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        break;
                    }

                    // yes we are also looking for a fallback, because errors can happen when a game-dev does something unexpected
                    case IDisposable disposableFallback: {
                        disposableFallback.Dispose();
                        break;
                    }
                }
            }

            // Dispose all objects marked as disposable
            while (DisposableInstances.TryTake(out Guid instanceId)) {
                if (!Instances.TryRemove(instanceId, out object? instance) || instance is not IDisposable disposableInstance) continue;

                disposableInstance.Dispose();
            }

            // Only ChildScopes should be disposed
            while (ChildScopes.TryTake(out IServiceProvider? childScope)) {
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

            // Dispose all async-disposable instances
            while (AsyncDisposableInstances.TryTake(out Guid instanceId)) {
                if (!Instances.TryGetValue(instanceId, out object? instance) || instance is not IAsyncDisposable asyncDisposable) continue;

                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }

            // Dispose synchronously any disposable objects if applicable
            while (DisposableInstances.TryTake(out Guid instanceId)) {
                if (!Instances.TryGetValue(instanceId, out object? instance) || instance is not IDisposable disposable) continue;

                disposable.Dispose();
            }

            // Don't forget about ChildScopes!
            while (ChildScopes.TryTake(out IServiceProvider? childScope)) {
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
        //      It just means that the references are no longer valid.
        DisposableInstances.Clear();
        AsyncDisposableInstances.Clear();
        Instances.Clear();
        ChildScopes.Clear();
        ParentScope?.RemoveItemFromBag(this);// Remove this instance from the parent scope's bag, else we will leak memory
        ParentScope = null;// Do not dispose the parent scope, only remove the reference
    }

    private void RemoveItemFromBag(ServiceProvider child) {
        if (ChildScopes.IsEmpty) return;

        var itemsToKeep = new List<IServiceProvider>(ChildScopes.Count - 1);

        // Remove specific item, keeping others in memory
        while (ChildScopes.TryTake(out IServiceProvider? item)) {
            if (item.Equals(child)) continue;

            itemsToKeep.Add(item);
        }

        // Re-add only the items we want to keep
        foreach (IServiceProvider item in itemsToKeep) {
            ChildScopes.Add(item);
        }
    }
    #endregion
}
