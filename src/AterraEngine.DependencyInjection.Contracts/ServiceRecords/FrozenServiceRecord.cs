// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class FrozenServiceRecord {

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

    public required Type ServiceType { get; init; }
    public required Type ImplementationType { get; init; }
    public required Delegate ImplementationFactory { get; init; }
    public required int ScopeDepth { get; init; }
    public required KnownScopeDepth Depth { get; init; }
    public required DisposalType Disposal { get; init; }
    public required GenericServiceState GenericService { get; init; }

    public Func<IScopedProvider, T> GetFactory<T>() => (Func<IScopedProvider, T>)ImplementationFactory;
}
