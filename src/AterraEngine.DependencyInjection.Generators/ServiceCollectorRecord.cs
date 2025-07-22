// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record ServiceCollectorRecord(
    INamedTypeSymbol ClassSymbol,
    INamedTypeSymbol AttributeSymbol,
    INamedTypeSymbol ServiceTypeSymbol,
    int ServiceDepth
) {}
