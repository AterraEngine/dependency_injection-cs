﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Generators.Helpers;
using AterraEngine.DependencyInjection.Generators.Registrations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[Generator(LanguageNames.CSharp)]
public class ServiceRegistrationGenerator : IIncrementalGenerator {
    public const string ServiceRegistrationFileName = "ServiceRegistration.g.cs";
    public const string PooledServicesFileName = "PooledServices.g.cs";
    private const string InjectableServiceAttributeMetadataName = "AterraEngine.DependencyInjection.InjectableServiceAttribute`1";
    private const string FactoryCreatedServiceAttributeMetadataName = "AterraEngine.DependencyInjection.FactoryCreatedServiceAttribute`2";
    private const string PooledInjectableServiceAttributeMetadataName = "AterraEngine.DependencyInjection.PooledInjectableServiceAttribute`2";

    private static readonly string[] MetaDataNames = [
        InjectableServiceAttributeMetadataName,
        FactoryCreatedServiceAttributeMetadataName,
        PooledInjectableServiceAttributeMetadataName
    ];

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        IncrementalValueProvider<ImmutableArray<ClassDeclarationSyntax>> syntaxProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                ProviderPredicate,
                ProviderTransform
            ).Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(syntaxProvider), GenerateSources);
    }

    #region ClassDeclarationsProvider
    private static bool ProviderPredicate(SyntaxNode node, CancellationToken _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };
    private static ClassDeclarationSyntax ProviderTransform(GeneratorSyntaxContext ctx, CancellationToken _) => (ClassDeclarationSyntax)ctx.Node;
    #endregion

    #region SourceGenerator
    private static void GenerateSources(SourceProductionContext context, (Compilation, ImmutableArray<ClassDeclarationSyntax>) source) {
        (Compilation? compilation, ImmutableArray<ClassDeclarationSyntax> classDeclarations) = source;

        if (compilation.AssemblyName is not {} assemblyName) {
            ReportDiagnostic(context, Rules.NoAssemblyNameFound);
            return;
        }

        IServiceRegistration[] registrations = GetRegistrations(context, compilation, classDeclarations)
                .OrderBy(registration => registration.LifeTime)
                .ThenBy(registration => registration.ServiceTypeName.ToDisplayString())
                .ToArray()
            ;

        string assemblyNameSanitized = assemblyName
            .Replace(".dll", "")
            .Replace("-", "_");

        context.AddSource(
            PooledServicesFileName,
            SourceText.From(GeneratePooledServicesFile(
                context,
                assemblyNameSanitized,
                registrations
            ), Encoding.UTF8)
        );

        context.AddSource(
            ServiceRegistrationFileName,
            SourceText.From(GenerateServiceRegistrationFile(
                context,
                assemblyNameSanitized,
                registrations
            ), Encoding.UTF8)
        );
    }

    private static List<IServiceRegistration> GetRegistrations(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classDeclarations) {
        Dictionary<string, INamedTypeSymbol?> types = MetaDataNames.ToDictionary(keySelector: name => name, compilation.GetTypeByMetadataName);
        if (types.Values.Any(t => t is null)) {
            ReportDiagnostic(context, Rules.NoAttributesFound);
            return [];
        }

        // See above if check to know why we ! for nullablity
        INamedTypeSymbol injectableServiceAttributeType = types[InjectableServiceAttributeMetadataName]!;
        INamedTypeSymbol factoryCreateServiceAttributeType = types[FactoryCreatedServiceAttributeMetadataName]!;
        INamedTypeSymbol injectablePooledServiceAttributeType = types[PooledInjectableServiceAttributeMetadataName]!;

        List<IServiceRegistration> registrations = [];
        foreach (ClassDeclarationSyntax candidate in classDeclarations) {
            SemanticModel model = compilation.GetSemanticModel(candidate.SyntaxTree);
            if (model.GetDeclaredSymbol(candidate) is not {} implementationTypeSymbol) continue;

            foreach (AttributeSyntax attribute in candidate.AttributeLists.SelectMany(attrList => attrList.Attributes)) {
                if (model.GetTypeInfo(attribute).Type is not INamedTypeSymbol attributeTypeInfo) continue;

                if (SymbolEqualityComparer.Default.Equals(attributeTypeInfo.ConstructedFrom, factoryCreateServiceAttributeType)
                    && FactoryCreatedServiceRegistration.TryCreateFromModel(implementationTypeSymbol, attribute, model, out FactoryCreatedServiceRegistration factoryCreated)) {
                    registrations.Add(factoryCreated);
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(attributeTypeInfo.ConstructedFrom, injectableServiceAttributeType)
                    && InjectableServiceRegistration.TryCreateFromModel(implementationTypeSymbol, attribute, model, out InjectableServiceRegistration injectable)) {
                    registrations.Add(injectable);
                    continue;
                }

                // ReSharper disable once InvertIf
                // ReSharper disable once RedundantJumpStatement
                if (SymbolEqualityComparer.Default.Equals(attributeTypeInfo.ConstructedFrom, injectablePooledServiceAttributeType)
                    && InjectablePoolableServiceRegistration.TryCreateFromModel(attribute, model, out InjectablePoolableServiceRegistration pooledInjectable)) {
                    registrations.Add(pooledInjectable);
                    continue;
                }
            }
        }

        return registrations;
    }

    private static string GenerateServiceRegistrationFile(SourceProductionContext _, string assemblyName, IServiceRegistration[] registrations) {
        StringBuilder sourceBuilder = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("using Microsoft.Extensions.DependencyInjection;")
            .AppendLine($"namespace {assemblyName};")
            .AppendLine()
            .AppendLine("public static class ServiceRegistration {")
            .AppendLine($"    public static IServiceCollection RegisterServicesFrom{Sanitize(assemblyName)}(this IServiceCollection services) {{");

        if (registrations.Any(r => r is InjectablePoolableServiceRegistration)) {
            sourceBuilder.IndentLine(2, $"services.AddSingleton<{assemblyName}.AutoPooledServices>();");
        }

        foreach (IServiceRegistration registration in registrations) {
            registration.FormatText(sourceBuilder, assemblyName);
        }

        return sourceBuilder.AppendLine("        return services;")
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();
    }

    private static string GeneratePooledServicesFile(SourceProductionContext _, string assemblyName, IServiceRegistration[] registrations) {
        StringBuilder sourceBuilder = new StringBuilder()
                .AppendLine("// <auto-generated />")
                .AppendLine("using Microsoft.Extensions.ObjectPool;")
                .AppendLine($"namespace {assemblyName};")
                .AppendLine()
                .AppendLine("public partial class AutoPooledServices {")
                .IndentLine(1, "private static readonly DefaultObjectPoolProvider _objectPoolProvider = new();")
                .AppendLine()
            ;

        foreach (IServiceRegistration serviceRegistration in registrations) {
            if (serviceRegistration is not InjectablePoolableServiceRegistration poolable) continue;

            poolable.FormatPoolText(sourceBuilder);
            sourceBuilder.AppendLine();// Add an empty line for ease of use
        }

        return sourceBuilder
            .AppendLine("}")
            .ToString();
    }
    #endregion

    #region Helper Methods
    private static void ReportDiagnostic(SourceProductionContext context, DiagnosticDescriptor rule) => context.ReportDiagnostic(Diagnostic.Create(rule, Location.None));
    private static string Sanitize(string input) => new(input.Where(char.IsLetterOrDigit).ToArray());
    #endregion
}
