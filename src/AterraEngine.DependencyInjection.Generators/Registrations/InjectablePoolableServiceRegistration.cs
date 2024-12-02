﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Generators.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace AterraEngine.DependencyInjection.Generators.Registrations;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record struct InjectablePoolableServiceRegistration(
    INamedTypeSymbol ServiceTypeName,
    INamedTypeSymbol ImplementationTypeName
) : IServiceRegistration {
    public string LifeTime { get; set; } = "Transient";

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void FormatText(StringBuilder builder, string assemblyName) {
        builder.IndentLine(2, $"services.Add{LifeTime}<{ServiceTypeName.ToDisplayString()}>("
            + $"provider => provider.GetRequiredService<{assemblyName}.AutoPooledServices>().{ImplementationTypeName.Name}Pool.Get()"
            + $");");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static bool TryCreateFromModel(
        AttributeSyntax attribute,
        SemanticModel model,
        out InjectablePoolableServiceRegistration registration
    ) {
        registration = default;

        GenericNameSyntax? genericNameSyntax = attribute switch {
            { Name: QualifiedNameSyntax { Right: GenericNameSyntax genericInQualifiedNameSyntax } } => genericInQualifiedNameSyntax,
            { Name: GenericNameSyntax genericNameSyntaxByItself } => genericNameSyntaxByItself,
            _ => null
        };

        if (genericNameSyntax?.TypeArgumentList.Arguments is not { Count: 2 } typeArgumentsList) return false;
        if (typeArgumentsList.FirstOrDefault() is not {} serviceTypeSyntax) return false;
        if (model.GetSymbolInfo(serviceTypeSyntax).Symbol is not INamedTypeSymbol serviceNamedTypeSymbol) return false;

        if (typeArgumentsList.LastOrDefault() is not {} implementationTypeSyntax) return false;
        if (model.GetSymbolInfo(implementationTypeSyntax).Symbol is not INamedTypeSymbol implementationTypeSymbol) return false;

        registration = new InjectablePoolableServiceRegistration(
            serviceNamedTypeSymbol,
            implementationTypeSymbol
        );

        return true;
    }

    public void FormatPoolText(StringBuilder builder) {
        builder.IndentLine(1, $"public ObjectPool<{ImplementationTypeName.ToDisplayString()}> {ImplementationTypeName.Name}Pool {{ get; }} "
            + $"= _objectPoolProvider.Create(new AterraEngine.DependencyInjection.PooledInjectableServiceObjectPolicy<{ImplementationTypeName.ToDisplayString()}>());");
    }
}
