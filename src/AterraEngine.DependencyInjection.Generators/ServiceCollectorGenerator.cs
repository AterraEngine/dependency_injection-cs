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
using System.Threading;

namespace AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[Generator(LanguageNames.CSharp)]
public class ServiceCollectorGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Register for class declarations with attributes in the syntax tree
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform);

        // Combine with compilation to validate types
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(classDeclarations.Collect()),
            GenerateSources
        );
    }

    // ReSharper disable once ConvertIfStatementToReturnStatement
    private static bool Predicate(SyntaxNode node, CancellationToken _) {
        if (node is not ClassDeclarationSyntax { AttributeLists: { Count: > 0 } attributeLists }) return false;

        return attributeLists.Any(static l => l.Attributes.Any());
    }

    private static ClassDeclarationSyntax Transform(GeneratorSyntaxContext context, CancellationToken _) => (ClassDeclarationSyntax)context.Node;

    private static void GenerateSources(SourceProductionContext context, (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes) source) {
        (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes) = source;

        // Get the ServiceAttribute base type
        INamedTypeSymbol? serviceAttributeSymbol = compilation.GetTypeByMetadataName("AterraEngine.DependencyInjection.ServiceAttribute");
        if (serviceAttributeSymbol is null) {
            return;
        }

        var validClasses = new List<ServiceCollectorRecord>();

        foreach (ClassDeclarationSyntax? classDeclaration in classes) {
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (ModelExtensions.GetDeclaredSymbol(semanticModel, classDeclaration) is not INamedTypeSymbol classSymbol) continue;

            // Check if any attribute on the class matches or derives from ServiceAttribute
            foreach (AttributeData? attributeData in classSymbol.GetAttributes()) {
                switch (attributeData) {
                    // Handle non-generic attributes
                    case { AttributeClass.IsGenericType: false, ConstructorArguments : { Length: > 0 } constructorArguments }
                        when constructorArguments[0].Value is INamedTypeSymbol serviceTypeSymbol: {

                        // Check if the class inherits from the service type
                        if (classSymbol.InheritsFrom(serviceTypeSymbol)) {

                            int scopeLevel = ResolveBaseConstructorArguments(context, attributeData.AttributeClass, attributeData, classDeclaration);

                            validClasses.Add(new ServiceCollectorRecord(classSymbol, attributeData.AttributeClass, serviceTypeSymbol, scopeLevel));
                            continue;
                        }

                        // Report diagnostic if inheritance fails
                        context.ReportDiagnostic(Diagnostics.InterfaceNotImplemented(
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
                            validClasses.Add(new ServiceCollectorRecord(classSymbol, attributeData.AttributeClass, serviceTypeSymbol, 0));
                            continue;
                        }

                        // Report diagnostic if inheritance fails
                        context.ReportDiagnostic(Diagnostics.InterfaceNotImplemented(
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
        foreach (ServiceCollectorRecord? collectorRecord in validClasses) {
            string className = collectorRecord.classSymbol.Name;
            string attributeName = collectorRecord.attributeSymbol.Name;
            string serviceTypeName = collectorRecord.serviceTypeSymbol.Name;

            context.AddSource(
                $"{className}_Generated.g.cs",
                SourceText.From($$"""

                    // <auto-generated />
                    namespace {{collectorRecord.classSymbol.ContainingNamespace}}
                    {
                        public static class {{className}}ServiceMetadata
                        {
                            public const string ServiceName = "{{className}}";
                            public const string Attribute = "{{attributeName}}";
                            public const string ServiceType = "{{serviceTypeName}}";
                            public const int ScopeLevel = {{collectorRecord.scopeLevel}};
                        }
                    }
                    """,
                    Encoding.UTF8));
        }
    }

    private static int ResolveBaseConstructorArguments(SourceProductionContext context, INamedTypeSymbol attributeSymbol, AttributeData attributeData, ClassDeclarationSyntax attributedClass) {
        // Inspect the constructors of the derived attribute (e.g., TransientServiceAttribute)
        foreach (IMethodSymbol? constructor in attributeSymbol.Constructors) {
            // If the constructor calls the base class, inspect the arguments passed up the chain
            foreach (SyntaxReference? syntaxReference in constructor.DeclaringSyntaxReferences) {
                if (syntaxReference.GetSyntax() is not ConstructorDeclarationSyntax constructorSyntax || constructorSyntax.Initializer == null) {
                    continue;
                }

                // Look at the base constructor arguments
                SeparatedSyntaxList<ArgumentSyntax> baseArguments = constructorSyntax.Initializer.ArgumentList.Arguments;

                if (baseArguments.Count <= 1) continue;

                // Resolve the value of the second argument
                string secondArgument = baseArguments[1].ToString();// You can refine this step with semantic analysis
                int scopeLevel = int.TryParse(secondArgument, out int l) ? l : 0;
                return scopeLevel;
            }
        }

        return 0;
    }

}
