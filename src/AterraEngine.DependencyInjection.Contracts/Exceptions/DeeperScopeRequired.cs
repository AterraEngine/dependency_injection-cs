// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class DeeperScopeRequiredException(string? message = null, Exception? innerException = null, Type? typeToResolve = null, int? scopeLevel = null) : Exception(message, innerException) {
    public Type? TypeToResolve => typeToResolve;
    public int? RequiredScopeLevel => scopeLevel;

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public DeeperScopeRequiredException(string? message = null, Type? typeToResolve = null, int? scopeLevel = null) : this(message, null, typeToResolve, scopeLevel) {} 
}
