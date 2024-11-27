// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
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
    public const string GeneratedFileName = "ServiceRegistration.g.cs";
    private const string InjectableServiceAttributeMetadataName = "AterraEngine.DependencyInjection.InjectableServiceAttribute`1";
    private const string FactoryCreatedServiceAttributeMetadataName = "AterraEngine.DependencyInjection.FactoryCreatedServiceAttribute`2";

    private static readonly ImmutableHashSet<string> ValidLifeTimes = ImmutableHashSet.Create("Singleton", "Scoped", "Transient");

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        IncrementalValueProvider<ImmutableArray<ClassDeclarationSyntax>> syntaxProvider = context.SyntaxProvider
            .CreateSyntaxProvider(ProviderPredicate, ProviderTransform)
            .Collect();

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

        List<IServiceRegistration> registrations = GetRegistrations(context, compilation, classDeclarations);

        context.AddSource(
            GeneratedFileName,
            SourceText.From(GenerateSourceText(
                context,
                assemblyName,
                registrations
            ), Encoding.UTF8)
        );
    }

    private static List<IServiceRegistration> GetRegistrations(SourceProductionContext context, Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classDeclarations) {
        if (compilation.GetTypeByMetadataName(InjectableServiceAttributeMetadataName) is not {} injectableServiceAttributeType) {
            ReportDiagnostic(context, Rules.NoAttributesFound);
            return [];
        }

        if (compilation.GetTypeByMetadataName(FactoryCreatedServiceAttributeMetadataName) is not {} factoryCreateServiceAttributeType) {
            ReportDiagnostic(context, Rules.NoAttributesFound);
            return [];
        }

        List<IServiceRegistration> registrations = [];
        foreach (ClassDeclarationSyntax candidate in classDeclarations) {
            SemanticModel model = compilation.GetSemanticModel(candidate.SyntaxTree);
            if (model.GetDeclaredSymbol(candidate) is not {} implementationTypeSymbol) continue;

            foreach (AttributeSyntax attribute in candidate.AttributeLists.SelectMany(attrList => attrList.Attributes)) {
                if (model.GetTypeInfo(attribute).Type is not INamedTypeSymbol attributeTypeInfo) continue;

                if (SymbolEqualityComparer.Default.Equals(attributeTypeInfo.ConstructedFrom, factoryCreateServiceAttributeType)) {
                    if (!TryGetFactoryCreatedServiceRegistration(implementationTypeSymbol, attribute, model, out FactoryCreatedServiceRegistration factoryCreated)) continue;
                    registrations.Add(factoryCreated);
                    continue;
                }
                
                if (!SymbolEqualityComparer.Default.Equals(attributeTypeInfo.ConstructedFrom, injectableServiceAttributeType)) continue;
                if (!TryGetInjectableServiceRegistration(implementationTypeSymbol, attribute, model, out InjectableServiceRegistration injectable)) continue;
                registrations.Add(injectable);
            }
        }

        return registrations;
    }

    private static bool TryGetLifeTime(MemberAccessExpressionSyntax lifetimeExpr, out string lifetimeName) {
        lifetimeName = lifetimeExpr.Name.Identifier.Text;
        return ValidLifeTimes.Contains(lifetimeName);
    }

    private static bool TryGetInjectableServiceRegistration(
        INamedTypeSymbol implementationTypeSymbol,
        AttributeSyntax attribute,
        SemanticModel model,
        out InjectableServiceRegistration registration
    ) {
        registration = default;

        if (attribute is not { Name: GenericNameSyntax genericNameSyntax }) return false;
        if (genericNameSyntax.TypeArgumentList.Arguments.FirstOrDefault() is not {} serviceTypeSyntax) return false;
        if (model.GetSymbolInfo(serviceTypeSyntax).Symbol is not INamedTypeSymbol serviceNamedTypeSymbol) return false;
        if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not MemberAccessExpressionSyntax memberAccess) return false;
        if (!TryGetLifeTime(memberAccess, out string lifeTime)) return false;

        registration = new InjectableServiceRegistration(
            serviceNamedTypeSymbol,
            implementationTypeSymbol,
            lifeTime
        );

        return true;
    }

    private static bool TryGetFactoryCreatedServiceRegistration(
        INamedTypeSymbol implementationTypeSymbol,
        AttributeSyntax attribute,
        SemanticModel model,
        out FactoryCreatedServiceRegistration registration
    ) {
        registration = default;

        if (attribute is not { Name: GenericNameSyntax genericNameSyntax }) return false;
        if (genericNameSyntax.TypeArgumentList.Arguments is not { Count: 2 } typeArgumentsList) return false;

        // order depends on the way it is defined in the attribute
        if (typeArgumentsList.FirstOrDefault() is not {} factoryTypeSyntax) return false;
        if (model.GetSymbolInfo(factoryTypeSyntax).Symbol is not INamedTypeSymbol factoryNamedTypeSymbol) return false;
        
        if (typeArgumentsList.LastOrDefault() is not {} serviceTypeSyntax) return false;
        if (model.GetSymbolInfo(serviceTypeSyntax).Symbol is not INamedTypeSymbol serviceNamedTypeSymbol) return false;
        
        if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is not MemberAccessExpressionSyntax memberAccess) return false;
        if (!TryGetLifeTime(memberAccess, out string lifeTime)) return false;

        registration = new FactoryCreatedServiceRegistration(
            serviceNamedTypeSymbol,
            implementationTypeSymbol,
            factoryNamedTypeSymbol,
            lifeTime
        );

        return true;
    }

    private static string GenerateSourceText(SourceProductionContext _, string assemblyName, IEnumerable<IServiceRegistration> registrations) {
        StringBuilder sourceBuilder = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("using Microsoft.Extensions.DependencyInjection;")
            .AppendLine($"namespace {assemblyName};")
            .AppendLine()
            .AppendLine("public static class ServiceRegistration {")
            .AppendLine($"    public static IServiceCollection RegisterServicesFrom{Sanitize(assemblyName)}(this IServiceCollection services) {{");

        foreach (IServiceRegistration registration in registrations) {
            sourceBuilder.AppendLine($"        {registration.TextFormat}");
        }

        return sourceBuilder.AppendLine("        return services;")
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();
    }
    #endregion

    #region Helper Methods
    private static void ReportDiagnostic(SourceProductionContext context, DiagnosticDescriptor rule) => context.ReportDiagnostic(Diagnostic.Create(rule, Location.None));
    private static string Sanitize(string input) => new(input.Where(char.IsLetterOrDigit).ToArray());
    #endregion
}
