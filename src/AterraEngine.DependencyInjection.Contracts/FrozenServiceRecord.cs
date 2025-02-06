// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public readonly record struct FrozenServiceRecord(
    // Type ServiceType,
    // Type ImplementationType,
    Guid Id,
    object ImplementationFactory,
    int ScopeDepth,
    FrozenServiceRecord.KnownScopeDepth Depth,
    FrozenServiceRecord.DisposalType Disposal
) {
    public bool TryGetFactory<T>([NotNullWhen(true)] out Func<IScopedProvider, T>? factory) {
        if (ImplementationFactory is Func<IScopedProvider, T> casted) {
            factory = casted;
            return true;
        }

        factory = null;
        return false;
    }
    
    public enum KnownScopeDepth : byte {
        Transient,
        Singleton,
        ProviderScoped,
        CustomScoped,
    } 
    
    public enum DisposalType : byte {
        None,           
        Disposable,     
        AsyncDisposable 
    }
}
