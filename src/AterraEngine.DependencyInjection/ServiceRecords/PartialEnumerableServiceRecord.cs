// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record PartialEnumerableServiceRecord<TImplementation> (
    Func<IScopedProvider, TImplementation> ImplementationFactory,
    int ScopeDepth
) : ServiceRecord<TImplementation> (
    typeof(TImplementation),
    typeof(TImplementation),
    ImplementationFactory,
    ScopeDepth
) where TImplementation : class ;
