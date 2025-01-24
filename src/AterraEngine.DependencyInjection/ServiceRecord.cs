﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record ServiceRecord<TService>(
    Type ServiceType,
    Type ImplementationType,
    Func<IServiceProvider, TService>? ImplementationFactory,
    int Lifetime
) : IServiceRecord {

    public Guid Id { get; } = Guid.CreateVersion7();
    public bool IsSingleton { get; } = Lifetime == 0;
    public bool IsTransient { get; } = Lifetime == -1;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);
    
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public bool TryGetFactory<T>([NotNullWhen(true)]  out Func<IServiceProvider, T>? factory) {
        factory = null;
        if (typeof(T) != typeof(TService)) return false;
        if (ImplementationFactory is not Func<IServiceProvider, T> casted) return false;
        return (factory = casted) is not null;
    }
}
