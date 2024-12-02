// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Generators.Helpers;
using AterraEngine.DependencyInjection.Generators.Registrations;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using System.Text;
using Xunit;

namespace Tests.AterraEngine.DependencyInjection.Generators.Registrations;

// ---------------------------------------------------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------------------------------------------------
[TestSubject(typeof(InjectablePoolableServiceRegistration))]
public class InjectablePoolableServiceRegistrationTests {
    [Fact]
    public void TryCreateFromModel_ValidInput_ReturnsTrueAndCorrectRegistration() {
        // Arrange
        var mockResolver = new Mock<ISymbolResolver>();

        // Mock service type and implementation type symbols
        var mockServiceTypeSymbol = new Mock<INamedTypeSymbol>();
        var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();

        mockServiceTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("IMyService");
        mockImplementationTypeSymbol.Setup(x => x.Name).Returns("MyServiceImplementation");
        mockImplementationTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceImplementation");

        // Mock resolver behavior
        mockResolver.Setup(r => r.ResolveSymbol(It.Is<SyntaxNode>(n => n.ToString() == "IMyService")))
            .Returns(mockServiceTypeSymbol.Object);
        mockResolver.Setup(r => r.ResolveSymbol(It.Is<SyntaxNode>(n => n.ToString() == "MyServiceImplementation")))
            .Returns(mockImplementationTypeSymbol.Object);

        // Prepare a valid attribute syntax
        var attributeSyntax = CreateValidAttributeSyntax();

        // Act
        var result = InjectablePoolableServiceRegistration.TryCreateFromModel(attributeSyntax, mockResolver.Object, out InjectablePoolableServiceRegistration registration);

        // Assert
        Assert.True(result);
        Assert.Equal("IMyService", registration.ServiceTypeName.ToDisplayString());
        Assert.Equal("MyNamespace.MyServiceImplementation", registration.ImplementationTypeName.ToDisplayString());
    }

    [Fact]
    public void TryCreateFromModel_InvalidInput_ReturnsFalse() {
        // Arrange
        var mockResolver = new Mock<ISymbolResolver>();
        var invalidAttributeSyntax = CreateInvalidAttributeSyntax();

        // Act
        var result = InjectablePoolableServiceRegistration.TryCreateFromModel(invalidAttributeSyntax, mockResolver.Object, out var registration);

        // Assert
        Assert.False(result);
        Assert.Equal(default, registration);
    }

    [Fact]
    public void FormatText_GeneratesCorrectOutput() {
        // Arrange
        var mockServiceTypeSymbol = new Mock<INamedTypeSymbol>();
        var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();

        mockServiceTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("IMyService");
        mockImplementationTypeSymbol.Setup(x => x.Name).Returns("MyServiceImplementation");
        mockImplementationTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceImplementation");

        var registration = new InjectablePoolableServiceRegistration(
            mockServiceTypeSymbol.Object,
            mockImplementationTypeSymbol.Object
        ) {
            LifeTime = "Scoped"
        };

        var builder = new StringBuilder();
        const string assemblyName = "TestAssembly";

        // Act
        registration.FormatText(builder, assemblyName);

        // Assert
        string expected = """
            services.AddScoped<IMyService>(
                        (provider) => provider.GetRequiredService<TestAssembly.AutoPooledServices>().MyServiceImplementationPool.Get()
                    );
            """.TrimStart();
        Assert.Equal(expected, builder.ToString().Trim());
    }

    [Fact]
    public void FormatPoolText_GeneratesCorrectOutput() {
        // Arrange
        var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();
        mockImplementationTypeSymbol.Setup(x => x.Name).Returns("MyServiceImplementation");
        mockImplementationTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceImplementation");

        var registration = new InjectablePoolableServiceRegistration(
            new Mock<INamedTypeSymbol>().Object,
            mockImplementationTypeSymbol.Object
        );

        var builder = new StringBuilder();

        // Act
        registration.FormatPoolText(builder);

        // Assert
        string expected = """
            public ObjectPool<MyNamespace.MyServiceImplementation> MyServiceImplementationPool { get; } = _objectPoolProvider
                    .Create(new AterraEngine.DependencyInjection.PooledInjectableServiceObjectPolicy<MyNamespace.MyServiceImplementation>());
            """.Trim();

        string actual = builder.ToString().Trim();

        Assert.Equal(expected, actual);
    }

    private static AttributeSyntax CreateValidAttributeSyntax() {
        // Generate a valid attribute syntax with two type arguments
        return SyntaxFactory.Attribute(
            SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("InjectablePoolableService")
            ).WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[] {
                        SyntaxFactory.ParseTypeName("IMyService"),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.ParseTypeName("MyServiceImplementation")
                    })
                )
            )
        );
    }


    private static AttributeSyntax CreateInvalidAttributeSyntax() {
        // Generate an invalid attribute syntax (e.g., with no arguments)
        return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("InjectablePoolableService"));
    }
}