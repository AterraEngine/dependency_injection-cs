// // ---------------------------------------------------------------------------------------------------------------------
// // Imports
// // ---------------------------------------------------------------------------------------------------------------------
// using AterraEngine.DependencyInjection.Generators.Registrations;
// using JetBrains.Annotations;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Moq;
// using System.Text;
// using Xunit;
//
// namespace Tests.AterraEngine.DependencyInjection.Generators.Registrations;
//
// // ---------------------------------------------------------------------------------------------------------------------
// // Code
// // ---------------------------------------------------------------------------------------------------------------------
// [TestSubject(typeof(FactoryCreatedServiceRegistration))]
// public class FactoryCreatedServiceRegistrationTest {
//     [Fact]
//     public void FormatText_GeneratesCorrectOutput() {
//         // Arrange
//         var mockServiceType = new Mock<INamedTypeSymbol>();
//         var mockImplementationTypeName = new Mock<INamedTypeSymbol>();
//         var mockFactoryType = new Mock<INamedTypeSymbol>();
//         mockServiceType.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.IMyService");
//         mockFactoryType.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns("MyNamespace.MyFactory");
//         
//         var registration = new FactoryCreatedServiceRegistration(
//             ServiceTypeName: mockServiceType.Object,
//             ImplementationTypeName: mockImplementationTypeName.Object, // Not relevant for this test
//             FactoryTypeName: mockFactoryType.Object,
//             LifeTime: "Singleton"
//         );
//
//         var stringBuilder = new StringBuilder();
//
//         // Act
//         registration.FormatText(stringBuilder, string.Empty);
//
//         // Assert
//         const string expected = """
//                     services.AddSingleton<MyNamespace.IMyService>(
//                         (provider) => provider.GetRequiredService<MyNamespace.MyFactory>().Create()
//                     );
//             """;
//         Assert.Equal(expected.Trim(), stringBuilder.ToString().Trim());
//     }
//
//     [Fact]
//     public void TryCreateFromModel_ValidInput_ReturnsTrueAndCorrectRegistration() {
//         // Arrange
//         var mockModel = new Mock<SemanticModel>();
//         var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();
//
//         // Create an empty AttributeSyntax to simulate invalid input
//         var attributeSyntax = SyntaxFactory.Attribute(
//             SyntaxFactory.IdentifierName("FactoryAttribute"),
//             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>())
//         );
//
//         // Act
//         bool result = FactoryCreatedServiceRegistration.TryCreateFromModel(
//             implementationTypeSymbol: mockImplementationTypeSymbol.Object,
//             attribute: attributeSyntax,
//             model: mockModel.Object,
//             out FactoryCreatedServiceRegistration registration
//         );
//
//         // Assert
//         Assert.False(result);
//         Assert.Equal(default, registration);
//     }
//
//     [Fact]
//     public void TryCreateFromModel_InvalidInput_ReturnsFalse() {
//         // Arrange
//         var mockModel = new Mock<SemanticModel>();
//         var mockImplementationTypeSymbol = new Mock<INamedTypeSymbol>();
//
//         // Create an AttributeSyntax with no arguments to simulate invalid input
//         AttributeSyntax attributeSyntax = SyntaxFactory.Attribute(
//             SyntaxFactory.IdentifierName("FactoryAttribute"),
//             SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>())
//         );
//
//         // Act
//         bool result = FactoryCreatedServiceRegistration.TryCreateFromModel(
//             implementationTypeSymbol: mockImplementationTypeSymbol.Object,
//             attribute: attributeSyntax,
//             model: mockModel.Object,
//             out FactoryCreatedServiceRegistration registration
//         );
//
//         // Assert
//         Assert.False(result);
//         Assert.Equal(default, registration);
//     }
//
// }
