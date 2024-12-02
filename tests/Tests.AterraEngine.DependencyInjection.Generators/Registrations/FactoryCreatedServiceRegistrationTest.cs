﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Generators.Registrations;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Moq;
using System.Text;
using Xunit;
namespace Tests.AterraEngine.DependencyInjection.Generators.Registrations;

// ---------------------------------------------------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------------------------------------------------
[TestSubject(typeof(FactoryCreatedServiceRegistration))]
public class FactoryCreatedServiceRegistrationTests {
    [Fact]
    public void FormatText_GeneratesCorrectOutput() {
        // Arrange
        var mockServiceTypeSymbol = new Mock<INamedTypeSymbol>();
        var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();
        var mockFactoryTypeSymbol = new Mock<INamedTypeSymbol>();

        mockServiceTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("IMyService");
        mockImplementationTypeSymbol.Setup(x => x.Name).Returns("MyServiceImplementation");
        mockImplementationTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceImplementation");
        mockFactoryTypeSymbol.Setup(x => x.Name).Returns("MyServiceFactory");
        mockFactoryTypeSymbol.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceFactory");

        var registration = new FactoryCreatedServiceRegistration(
            mockServiceTypeSymbol.Object,
            mockImplementationTypeSymbol.Object,
            mockFactoryTypeSymbol.Object,
            "Scoped"
        );

        var builder = new StringBuilder();

        // Act
        registration.FormatText(builder, "TestAssembly");

        // Assert
        string expected = """
            services.AddScoped<IMyService>(
                        (provider) => provider.GetRequiredService<MyNamespace.MyServiceFactory>().Create()
                    );
            """.TrimStart();
        Assert.Equal(expected, builder.ToString().Trim());
    }
}