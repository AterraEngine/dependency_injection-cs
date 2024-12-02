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
[TestSubject(typeof(FactoryCreatedServiceAttribute<,>))]
public class FactoryCreatedServiceAttributeTest {
    [Fact]
    public void FactoryCreatedServiceAttribute_PropertiesAreSetCorrectly() {
        // Arrange
        var attribute = new FactoryCreatedServiceAttribute<MyServiceFactory, IMyService>(ServiceLifetime.Scoped);

        // Act
        ServiceLifetime lifetime = attribute.Lifetime;
        Type factoryType = attribute.FactoryType;
        Type serviceType = attribute.ServiceType;

        // Assert
        Assert.Equal(ServiceLifetime.Scoped, lifetime);
        Assert.Equal(typeof(MyServiceFactory), factoryType);
        Assert.Equal(typeof(IMyService), serviceType);
    }

    [Fact]
    public void FactoryCreatedServiceAttribute_CanBeAppliedToClass() {
        // Act
        object[] attributes = typeof(SampleServiceWithAttribute).GetCustomAttributes(typeof(FactoryCreatedServiceAttribute<MyServiceFactory, IMyService>), false);

        // Assert
        Assert.Single(attributes);
        var attribute = (FactoryCreatedServiceAttribute<MyServiceFactory, IMyService>)attributes.First();
        Assert.Equal(ServiceLifetime.Singleton, attribute.Lifetime);
    }

    // Dummy class to test attribute application
    [FactoryCreatedService<MyServiceFactory, IMyService>(ServiceLifetime.Singleton)]
    private class SampleServiceWithAttribute;

    public interface IMyService;

    public class MyService : IMyService;

    public class MyServiceFactory : IFactoryService<IMyService> {
        public IMyService Create() => new MyService();
    }
}
