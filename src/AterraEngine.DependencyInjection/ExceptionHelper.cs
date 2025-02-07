// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ExceptionHelper {
    public static void CheckAndCouldNotBeResolvedException<T>(T? value, out T result) where T : class {
        result = value ?? throw new CouldNotBeResolvedException($"The required service of type '{typeof(T)}' could not be resolved.");
    }
}
