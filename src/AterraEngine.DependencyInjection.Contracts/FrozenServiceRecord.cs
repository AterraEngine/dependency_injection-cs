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
    FrozenServiceRecord.GenericServiceState GenericService
) {

    public enum DisposalType : byte {
        None,
        Disposable,
        AsyncDisposable
    }

    public enum GenericServiceState : byte {
        None,
        OpenGeneric,
        ClosedGeneric
    }

    public enum KnownScopeDepth : byte {
        Transient,
        Singleton,
        ProviderScoped,
        CustomScoped
    }

    public static FrozenServiceRecord Empty { get; } = new(Guid.Empty, null!, null!, null!, 0, default!, default!, default!);

    public bool TryGetFactory<T>([NotNullWhen(true)] out Func<IScopedProvider, T>? factory) {
        if (ImplementationFactory is Func<IScopedProvider, T> casted) {
            factory = casted;
            return true;
        }

        factory = null;
        return false;
    }
}
