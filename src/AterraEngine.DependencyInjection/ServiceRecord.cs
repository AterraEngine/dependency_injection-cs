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
    Func<IScopedProvider, TService>? ImplementationFactory,
    int ScopeDepth
) : IServiceRecord {

    public Guid Id { get; } = Guid.CreateVersion7();
    public bool IsTransient { get; } = ScopeDepth == (int)DefaultScopeDepth.Transient;
    public bool IsSingleton { get; } = ScopeDepth == (int)DefaultScopeDepth.Singleton;
    public bool IsProviderScoped { get; } = ScopeDepth == (int)DefaultScopeDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);
    public bool HasFactory => ImplementationFactory is not null; // ImplementationFactory can be updated, so this needs to be computed on every call.

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public ServiceRecord(
        Type ServiceType,
        Type ImplementationType,
        Func<IScopedProvider, TService>? ImplementationFactory,
        DefaultScopeDepth ScopeDepth
    ) : this(ServiceType, ImplementationType, ImplementationFactory, (int)ScopeDepth) {}
    
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public bool TryGetFactory<T>([NotNullWhen(true)] out Func<IScopedProvider, T>? factory) {
        factory = null;
        // TODO The absence of a factory at this point should raise a lot of red flags and not simply return a false.
        if (!HasFactory) return false; // No need to do complex checks if we don't have a factory.
        
        if (typeof(T) != typeof(TService)) return false;
        if (ImplementationFactory is not Func<IScopedProvider, T> casted) return false;

        return (factory = casted) is not null;
    }
}
