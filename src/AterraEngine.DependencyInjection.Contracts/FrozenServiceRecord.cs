// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record FrozenServiceRecord(
    Type ServiceType,
    Type ImplementationType,
    Delegate ImplementationFactory,
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
    
    public Func<IScopedProvider, T> GetFactory<T>() => (Func<IScopedProvider, T>)ImplementationFactory;
}
