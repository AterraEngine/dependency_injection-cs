// ---------------------------------------------------------------------------------------------------------------------
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
    int ScopeDepth
) : IServiceRecord {

    public Guid Id { get; } = Guid.CreateVersion7();
    public bool IsTransient { get; } = ScopeDepth == (int)DefaultScopeDepth.Transient;
    public bool IsSingleton { get; } = ScopeDepth == (int)DefaultScopeDepth.Singleton;
    public bool IsProviderScoped { get; } = ScopeDepth == (int)DefaultScopeDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public bool TryGetFactory<T>([NotNullWhen(true)] out Func<IServiceProvider, T>? factory) {
        factory = null;
        if (typeof(T) != typeof(TService)) return false;
        if (ImplementationFactory is not Func<IServiceProvider, T> casted) return false;

        return (factory = casted) is not null;
    }
}
