// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public readonly record struct FrozenServiceRecord(
    Guid Id,
    Type ServiceType,
    Type ImplementationType,
    object ImplementationFactory,
    int ScopeDepth,
    FrozenServiceRecord.KnownScopeDepth Depth,
    FrozenServiceRecord.DisposalType Disposal,
    bool IsGenericService = false
) {

    public enum DisposalType : byte {
        None,
        Disposable,
        AsyncDisposable
    }

    public enum KnownScopeDepth : byte {
        Transient,
        Singleton,
        ProviderScoped,
        CustomScoped
    }

    public bool TryGetFactory<T>([NotNullWhen(true)] out Func<IScopedProvider, T>? factory) {
        if (ImplementationFactory is Func<IScopedProvider, T> casted) {
            factory = casted;
            return true;
        }

        factory = null;
        return false;
    }

    public void ThrowIfDeeperScopeRequired(int targetDepth) {
        if (ScopeDepth <= targetDepth) return;

        throw new DeeperScopeRequiredException($"Required scope's depth {ScopeDepth} is deeper than the current scope's depth of {targetDepth}");
    }
}
