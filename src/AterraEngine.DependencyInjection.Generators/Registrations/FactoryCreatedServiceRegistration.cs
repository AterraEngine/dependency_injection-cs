// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators.Registrations;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record struct FactoryCreatedServiceRegistration(
    INamedTypeSymbol ServiceTypeName,
    INamedTypeSymbol ImplementationTypeName,
    INamedTypeSymbol FactoryTypeName,
    string LifeTime
) : IServiceRegistration {
    public string TextFormat { get; set; } = $"services.Add{LifeTime}<{ServiceTypeName.ToDisplayString()}>((provider) => provider.GetRequiredService<{FactoryTypeName}>().Create());";
}
