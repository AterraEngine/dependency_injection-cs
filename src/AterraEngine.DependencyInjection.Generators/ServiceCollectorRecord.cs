// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record ServiceCollectorRecord(
    INamedTypeSymbol classSymbol,
    INamedTypeSymbol attributeSymbol, 
    INamedTypeSymbol serviceTypeSymbol, 
    int scopeLevel) {
}
