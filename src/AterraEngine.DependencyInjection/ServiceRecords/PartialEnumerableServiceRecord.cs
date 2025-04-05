// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record PartialEnumerableServiceRecord<TImplementation> (
    Func<ITieredServiceProvider, TImplementation> ImplementationFactory,
    int ServiceDepth
) : ServiceRecord<TImplementation> (
    typeof(TImplementation),
    typeof(TImplementation),
    ImplementationFactory,
    ServiceDepth
) where TImplementation : class ;
