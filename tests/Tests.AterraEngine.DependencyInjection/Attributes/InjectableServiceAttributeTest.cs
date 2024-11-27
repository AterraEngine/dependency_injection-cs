// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Attributes;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[TestSubject(typeof(InjectableServiceAttribute<>))]
public class InjectableServiceAttributeTests {
    [Fact]
    public void InjectableServiceAttribute_PropertiesAreSetCorrectly() {
        // Arrange
        var attribute = new InjectableServiceAttribute<IMyService>(ServiceLifetime.Scoped);

        // Act
        ServiceLifetime lifetime = attribute.Lifetime;
        Type serviceType = attribute.ServiceType;

        // Assert
        Assert.Equal(ServiceLifetime.Scoped, lifetime);
        Assert.Equal(typeof(IMyService), serviceType);
    }

    [Fact]
    public void InjectableServiceAttribute_CanBeAppliedToClass() {
        // Act
        object[] attributes = typeof(SampleServiceWithAttribute).GetCustomAttributes(typeof(InjectableServiceAttribute<IMyService>), false);

        // Assert
        Assert.Single(attributes);
        var attribute = (InjectableServiceAttribute<IMyService>)attributes.First();
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    // Dummy class to test attribute application
    [InjectableServiceAttribute<IMyService>(ServiceLifetime.Singleton)]
    public class SampleServiceWithAttribute : IMyService;

    public interface IMyService;
}
