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
}