// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class FrozenServiceRecordHelper {
    public static FrozenServiceRecord CreateClosedGenericRecord(Type closedType, FrozenServiceRecord openRecord) {
        // Validate that the requested type is constructible from an open generic
        if (!closedType.IsGenericType || closedType.GetGenericTypeDefinition() != openRecord.ServiceType.GetGenericTypeDefinition()) {
            throw new InvalidOperationException($"Unable to resolve generic type '{closedType}' from open generic '{openRecord.ServiceType}'.");
        }

        // Generate the closed implementation type
        Type closedImplementationType = openRecord.ImplementationType
            .MakeGenericType(closedType.GetGenericArguments());// Close the generic type

        return new FrozenServiceRecord {
            ServiceType = closedType,
            ImplementationType = closedImplementationType,// Pass the fully closed implementation type
            ImplementationFactory = ConstructorReflectionFactory.CreateFunc<object>(closedImplementationType),// Factory for the closed generic
            ScopeDepth = openRecord.ScopeDepth,
            Depth = openRecord.Depth,
            Disposal = openRecord.Disposal,
            GenericService = FrozenServiceRecord.GenericServiceState.ClosedGeneric// It's no longer open, unresolved, generic
        };
    }
}
