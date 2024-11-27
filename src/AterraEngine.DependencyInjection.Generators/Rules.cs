// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Rules {
    public static readonly DiagnosticDescriptor NoAttributesFound = new(
        id: "ATRDI001",
        title: "InjectableServiceAttribute not found",
        messageFormat: "InjectableServiceAttribute not found",
        category: "InjectableServices",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor NoAssemblyNameFound = new(
        id: "ATRDI002",
        title: "No assembly name was found",
        messageFormat: "No assembly name was found",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}