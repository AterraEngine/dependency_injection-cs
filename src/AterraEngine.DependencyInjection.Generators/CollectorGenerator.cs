// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using CodeOfChaos.GeneratorTools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[Generator(LanguageNames.CSharp)]
public class CollectorGenerator : IIncrementalGenerator {
    private static readonly DiagnosticDescriptor DoesNotInheritDiagnostic = new(
        "DI001",
        "Class does not inherit required service type",
        "The class '{0}' is decorated with '{1}' but does not implement or inherit from '{2}'",
        "DependencyInjection",
        DiagnosticSeverity.Warning,
        true);

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Register for class declarations with attributes in the syntax tree
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists: { Count: > 0 } list } && list.Any(static l => l.Attributes.Any()),
                transform: static (context, _) => (ClassDeclarationSyntax)context.Node)
            .Where(static c => c is not null);

        // Combine with compilation to validate types
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(classDeclarations.Collect()),
            GenerateSources
        );
    }

    private static void GenerateSources(SourceProductionContext context, (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes) source) {
        (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes) = source;

        // Get the ServiceAttribute base type
        INamedTypeSymbol? serviceAttributeSymbol = compilation.GetTypeByMetadataName("AterraEngine.DependencyInjection.ServiceAttribute");
        if (serviceAttributeSymbol is null) {
            return;
        }

        var validClasses = new List<(INamedTypeSymbol classSymbol, INamedTypeSymbol attributeSymbol, INamedTypeSymbol serviceTypeSymbol, int scopeLevel)>();

        foreach (ClassDeclarationSyntax? classDeclaration in classes) {
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol) continue;

            // Check if any attribute on the class matches or derives from ServiceAttribute
            foreach (AttributeData? attributeData in classSymbol.GetAttributes()) {
                switch (attributeData) {
                    // Handle non-generic attributes
                    case { AttributeClass.IsGenericType: false, ConstructorArguments : { Length: > 0 } constructorArguments }
                        when constructorArguments[0].Value is INamedTypeSymbol serviceTypeSymbol: {
                        // Check if the class inherits from the service type
                        if (classSymbol.InheritsFrom(serviceTypeSymbol)) {
                            validClasses.Add((classSymbol, attributeData.AttributeClass, serviceTypeSymbol, 0));
                            continue;
                        }

                        // Report diagnostic if inheritance fails
                        context.ReportDiagnostic(Diagnostic.Create(
                            DoesNotInheritDiagnostic,
                            classDeclaration.Identifier.GetLocation(),
                            classSymbol.Name,
                            attributeData.AttributeClass.Name,
                            serviceTypeSymbol.ToDisplayString()
                        ));

                        break;
                    }

                    // Handle generic attributes
                    case { AttributeClass : { IsGenericType: true, TypeArguments: { Length: > 0 } typeArguments } }
                        when typeArguments[0] is INamedTypeSymbol serviceTypeSymbol: {
                        // Check if the class inherits from the service type
                        if (classSymbol.InheritsFrom(serviceTypeSymbol)) {
                            validClasses.Add((classSymbol, attributeData.AttributeClass, serviceTypeSymbol, 0));
                            continue;
                        }

                        // Report diagnostic if inheritance fails
                        context.ReportDiagnostic(Diagnostic.Create(
                            DoesNotInheritDiagnostic,
                            classDeclaration.Identifier.GetLocation(),
                            classSymbol.Name,
                            attributeData.AttributeClass.Name,
                            serviceTypeSymbol.ToDisplayString()
                        ));

                        break;
                    }
                }
            }
        }

        // Generate source for the valid classes
        foreach ((INamedTypeSymbol classSymbol, INamedTypeSymbol attributeSymbol, INamedTypeSymbol serviceTypeSymbol, int scopeLevel) in validClasses) {
            string className = classSymbol.Name;
            string attributeName = attributeSymbol.Name;
            string serviceTypeName = serviceTypeSymbol.Name;

            context.AddSource(
                $"{className}_Generated.g.cs",
                SourceText.From($$"""

                    // <auto-generated />
                    namespace {{classSymbol.ContainingNamespace}}
                    {
                        public static class {{className}}ServiceMetadata
                        {
                            public const string ServiceName = "{{className}}";
                            public const string Attribute = "{{attributeName}}";
                            public const string ServiceType = "{{serviceTypeName}}";
                            public const int ScopeLevel = {{scopeLevel}};
                        }
                    }
                    """,
                    Encoding.UTF8));
        }
    }
}
