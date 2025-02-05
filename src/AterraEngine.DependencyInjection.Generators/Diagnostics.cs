// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Diagnostics {
    #region InterfaceNotImplemented
    private static readonly DiagnosticDescriptor InterfaceNotImplementedDescriptor = new(
        "DI001",
        "Class does not inherit required service type",
        "The class '{0}' is decorated with '{1}' but does not implement or inherit from '{2}'",
        "DependencyInjection",
        DiagnosticSeverity.Warning,
        true);
    
    public static Diagnostic InterfaceNotImplemented(Location location, string className, string attributeName, string serviceType) {
        return Diagnostic.Create(
            InterfaceNotImplementedDescriptor,
            location,
            className,
            attributeName,
            serviceType
        );
    }
    #endregion
}
