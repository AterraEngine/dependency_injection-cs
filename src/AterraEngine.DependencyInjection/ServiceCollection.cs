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
public class ServiceCollection : IServiceCollection {
    private ConcurrentDictionary<Type, IServiceRecord> Records { get; } = new();

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class => AddService<TImplementation, TImplementation>(scopeLevel);
    public IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService {
        Records.AddOrUpdate(
            typeof(TService),
            addValueFactory: _ => ServiceRecordReflectionFactory.CreateWithFactory<TService, TImplementation>(scopeLevel),
            updateValueFactory: (_, _) => ServiceRecordReflectionFactory.CreateWithFactory<TService, TImplementation>(scopeLevel)
        );

        return this;
    }

    public IScopedProvider Build() => new ScopedProvider(ServiceContainer.FromCollection(Records));

    #region AddService by Record
    public IServiceCollection AddService(IServiceRecord record) {
        Records.AddOrUpdate(record.ServiceType, record, updateValueFactory: (_, _) => record);
        return this;
    }
    #endregion

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

    #region AddSingleton
    public IServiceCollection AddSingleton<TImplementation>() where TImplementation : class => AddSingleton<TImplementation, TImplementation>();
    public IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService => AddService<TService, TImplementation>((int)DefaultScopeDepth.Singleton);
    public IServiceCollection AddSingleton(Type implementation) => AddService(implementation, (int)DefaultScopeDepth.Singleton);
    public IServiceCollection AddSingleton(Type service, Type implementation) => AddService(service, implementation, (int)DefaultScopeDepth.Singleton);
    #endregion

    #region AddTransient
    public IServiceCollection AddTransient<TImplementation>() where TImplementation : class => AddTransient<TImplementation, TImplementation>();
    public IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService => AddService<TService, TImplementation>((int)DefaultScopeDepth.Transient);
    public IServiceCollection AddTransient(Type implementation) => AddService(implementation, (int)DefaultScopeDepth.Transient);
    public IServiceCollection AddTransient(Type service, Type implementation) => AddService(service, implementation, (int)DefaultScopeDepth.Transient);
    #endregion

    #region AddTransient
    public IServiceCollection AddScoped<TImplementation>() where TImplementation : class => AddScoped<TImplementation, TImplementation>();
    public IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService => AddService<TService, TImplementation>((int)DefaultScopeDepth.ProviderScoped);
    public IServiceCollection AddScoped(Type implementation) => AddService(implementation, (int)DefaultScopeDepth.ProviderScoped);
    public IServiceCollection AddScoped(Type service, Type implementation) => AddService(service, implementation, (int)DefaultScopeDepth.ProviderScoped);
    #endregion

    #region ICollection<IServiceRecord>
    public IEnumerator<IServiceRecord> GetEnumerator() => Records.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(IServiceRecord item) {
        if (Records.TryAdd(item.ServiceType, item)) return;

        throw new InvalidOperationException("Service already exists");
    }

    public void Clear() => Records.Clear();

    public bool Contains(IServiceRecord item) {
        if (!Records.TryGetValue(item.ServiceType, out IServiceRecord? record)) return false;

        return record == item;
    }

    public void CopyTo(IServiceRecord[] array, int arrayIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex + Records.Count > array.Length) throw new ArgumentException("The array does not have enough space to copy the elements.");

        Records.Values.CopyTo(array, arrayIndex);
    }

    public bool Remove(IServiceRecord item) {
        if (!Records.TryGetValue(item.ServiceType, out IServiceRecord? record)) return false;
        if (record != item) return false;

        return !Records.TryRemove(item.ServiceType, out IServiceRecord? _);
    }

    public int Count => Records.Count;
    public bool IsReadOnly => false;
    #endregion
}
