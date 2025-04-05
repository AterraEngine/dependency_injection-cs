// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ActivatorHelper {

    [RequiresDynamicCode("This method uses reflection to create an underlying service record which is used to create the instance.")]
    public static T CreateInstance<T>(ITieredServiceProvider serviceProvider) where T : class =>
        // Very "easy" approach in doing this, might need a lot more lifting in the future for edge cases ike creating structs, etc...
        // TODO: This requires a lot of testing to make valid code.
        ConstructorReflectionFactory.CreateFunc<T>(typeof(T)).Invoke(serviceProvider);
}
