// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators.Registrations;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceRegistration {
    public INamedTypeSymbol ServiceTypeName { get; }
    public INamedTypeSymbol ImplementationTypeName { get; }
    public string LifeTime { get; }

    public string TextFormat { get; set; }
}
