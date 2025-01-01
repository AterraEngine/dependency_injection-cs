﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Generators.Registrations;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Moq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.AterraEngine.DependencyInjection.Generators.Registrations;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[TestSubject(typeof(InjectableServiceRegistration))]
public class InjectableServiceRegistrationTest {
    [Test]
    public async Task FormatText_ValidInput_GeneratesCorrectFormattedText()
    {
        // Arrange
        var serviceTypeMock = new Mock<INamedTypeSymbol>();
        var implementationTypeMock = new Mock<INamedTypeSymbol>();

        // Set up mock behavior for the ToDisplayString method
        serviceTypeMock.Setup(s => s.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("IMyService");
        implementationTypeMock.Setup(i => i.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyServiceImplementation");

        // Mock the lifetime
        string lifeTime = "Scoped";

        // Create the InjectableServiceRegistration object
        var registration = new InjectableServiceRegistration(
            serviceTypeMock.Object,
            implementationTypeMock.Object,
            lifeTime
        );

        // Act
        var builder = new StringBuilder();
        registration.FormatText(builder, string.Empty);  // The second parameter is unused

        // Assert
        string expected = "services.AddScoped<IMyService, MyNamespace.MyServiceImplementation>();";
        await Assert.That(builder.ToString().Trim()).IsEqualTo(expected);
    }
}
