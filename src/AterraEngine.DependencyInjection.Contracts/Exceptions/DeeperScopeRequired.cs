// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class DeeperScopeRequiredException(string? message = null, Exception? innerException = null, Type? typeToResolve = null, int? scopeLevel = null) : Exception(message, innerException) {

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public DeeperScopeRequiredException(string? message = null, Type? typeToResolve = null, int? scopeLevel = null) : this(message, null, typeToResolve, scopeLevel) {}
    public Type? TypeToResolve => typeToResolve;
    public int? RequiredScopeLevel => scopeLevel;
}
