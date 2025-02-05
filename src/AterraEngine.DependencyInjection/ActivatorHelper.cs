// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ActivatorHelper {
    public static T CreateInstance<T>(IScopedProvider scopedProvider) where T : class {
        // Very "easy" approach in doing this, might need a lot more lifting in the future for edge cases ike creating structs, etc...
        ServiceRecord<T> record = ServiceRecordReflectionFactory.CreateWithFactory<T,T>((int)DefaultScopeDepth.Transient);
        if (record.ImplementationType is null) throw new Exception("No implementation type");
        
        return record.ImplementationFactory?.Invoke(scopedProvider) ?? Activator.CreateInstance<T>();
    }
}
