// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators.Registrations;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record struct InjectableServiceRegistration(
    INamedTypeSymbol ServiceTypeName,
    INamedTypeSymbol ImplementationTypeName,
    string LifeTime
) : IServiceRegistration {
    public string TextFormat { get; set; } = $"services.Add{LifeTime}<{ServiceTypeName.ToDisplayString()}, {ImplementationTypeName.ToDisplayString()}>();";
}
