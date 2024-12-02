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
public record struct FactoryCreatedServiceRegistration(
    INamedTypeSymbol ServiceTypeName,
    INamedTypeSymbol ImplementationTypeName,
    INamedTypeSymbol FactoryTypeName,
    string LifeTime
) : IServiceRegistration {


    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void FormatText(StringBuilder builder, string _) {
        builder.IndentLine(2, $"services.Add{LifeTime}<{ServiceTypeName.ToDisplayString()}>((provider) => provider.GetRequiredService<{FactoryTypeName.ToDisplayString()}>().Create());");
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static bool TryCreateFromModel(
        INamedTypeSymbol implementationTypeSymbol,
        AttributeSyntax attribute,
        SemanticModel model,
        out FactoryCreatedServiceRegistration registration
    ) {
        registration = default;

        GenericNameSyntax? genericNameSyntax = attribute switch {
            { Name: QualifiedNameSyntax { Right: GenericNameSyntax genericInQualifiedNameSyntax } } => genericInQualifiedNameSyntax,
            { Name: GenericNameSyntax genericNameSyntaxByItself } => genericNameSyntaxByItself,
            _ => null
        };

        if (genericNameSyntax?.TypeArgumentList.Arguments is not { Count: 2 } typeArgumentsList) return false;

        // order depends on the way it is defined in the attribute
        if (typeArgumentsList.FirstOrDefault() is not {} factoryTypeSyntax) return false;
        if (model.GetSymbolInfo(factoryTypeSyntax).Symbol is not INamedTypeSymbol factoryNamedTypeSymbol) return false;

        if (typeArgumentsList.LastOrDefault() is not {} serviceTypeSyntax) return false;
        if (model.GetSymbolInfo(serviceTypeSyntax).Symbol is not INamedTypeSymbol serviceNamedTypeSymbol) return false;

        if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not MemberAccessExpressionSyntax memberAccess) return false;
        if (!memberAccess.TryGetAsServiceLifetimeString(out string? lifeTime)) return false;

        registration = new FactoryCreatedServiceRegistration(
            serviceNamedTypeSymbol,
            implementationTypeSymbol,
            factoryNamedTypeSymbol,
            lifeTime
        );

        return true;
    }
}
