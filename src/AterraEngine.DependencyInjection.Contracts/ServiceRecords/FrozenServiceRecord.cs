// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record FrozenServiceRecord(
    Type ServiceType,
    Type ImplementationType,
    Delegate ImplementationFactory,
    int ServiceDepth,
    FrozenServiceRecord.KnownServiceDepth Depth,
    FrozenServiceRecord.DisposalType Disposal,
    FrozenServiceRecord.GenericServiceState GenericService
) {
    
    public Guid Id { get; set; } = Guid.CreateVersion7();

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

    public enum KnownServiceDepth : byte {
        Transient,
        Singleton,
        ProviderScoped,
        CustomTier
    }

    public Func<ITieredServiceProvider, T> GetFactory<T>() => (Func<ITieredServiceProvider, T>)ImplementationFactory;
}
