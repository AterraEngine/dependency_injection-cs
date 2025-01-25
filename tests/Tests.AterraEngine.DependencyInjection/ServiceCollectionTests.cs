// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using Tests.AterraEngine.DependencyInjection.Helpers;
using Tests.AterraEngine.DependencyInjection.Services;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Tests.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceCollectionTests {
    [Test]
    public async Task Collection_Should_Return_Service_Provider() {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddSingleton<IEmptyService, EmptyService>();

        // Act
        IServiceProvider provider = collection.Build();
        var service = provider.GetService<IEmptyService>();

        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(1);

        await Assert.That(service)
            .IsNotNull();
    }

    [Test]
    public async Task Collection_Should_Return_Service_Provider_With_Multiple_Services() {
        // Arrange
        var collection = new ServiceCollection();

        // Add multiple services
        collection.AddSingleton<IEmptyService, EmptyService>();
        collection.AddSingleton<ISampleService, SampleService>();

        // Act
        IServiceProvider provider = collection.Build();

        var emptyService = provider.GetService<IEmptyService>();
        var sampleService = provider.GetService<ISampleService>();

        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(2);// Expecting two services registered

        await Assert.That(emptyService)
            .IsNotNull();

        await Assert.That(sampleService)
            .IsNotNull();
    }

    [Test]
    public async Task Collection_Should_Handle_ServiceProvider_Service() {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddSingleton<IServiceProviderRequiredService, ServiceProviderRequiredService>();

        // Act
        IServiceProvider provider = collection.Build();
        var service = provider.GetService<IServiceProviderRequiredService>();

        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(1);

        await Assert.That(service)
            .IsTypeOf<ServiceProviderRequiredService>()
            .And.HasMember(p => p!.ServiceProvider).EqualTo(provider);
    }

    [Test]
    public async Task Collection_Should_Throw_ServicesWithGenerics() {
        // Arrange
        var collection = new ServiceCollection();

        // Act & Assert
        await Assert.ThrowsAsync(() => Task.FromResult(collection.AddSingleton(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics))));
        // TODO find a way to make services with generics be auto created through a factory system?
    }

    [Test]
    public async Task Collection_Should_Handle_Scopes() {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddSingleton<IEmptyService, EmptyService>();
        collection.AddService<IIdService, IdService>(scopeLevel: 1);
        IServiceProvider globalProvider = collection.Build();

        // Act
        var singletonService = globalProvider.GetService<IEmptyService>();
        IServiceProvider scope0 = globalProvider.CreateDeeperScope();
        var scope0Service = scope0.GetService<IIdService>();
        var scope0SingletonService = scope0.GetService<IEmptyService>();

        IServiceProvider scope1 = globalProvider.CreateDeeperScope();
        var scope1Service = scope1.GetService<IIdService>();
        var scope1SingletonService = scope0.GetService<IEmptyService>();

        // Assert
        await Assert.That(singletonService).IsNotNull()
            .And.IsEqualTo(scope0SingletonService)
            .And.IsEqualTo(scope1SingletonService);

        await Assert.That(scope0Service).IsNotNull()
            .And.IsNotEqualTo(scope1Service)
            .And.HasMember(s => s!.Id).NotEqualTo(scope1Service!.Id);

        await Assert.That(scope0SingletonService).IsNotNull()
            .And.IsEqualTo(singletonService)
            .And.IsEqualTo(scope1SingletonService);

        await Assert.That(scope1Service).IsNotNull()
            .And!.IsNotEqualTo(scope0Service)
            .And.HasMember(s => s!.Id).NotEqualTo(scope0Service!.Id);

        await Assert.That(scope1SingletonService).IsNotNull()
            .And.IsEqualTo(scope0SingletonService)
            .And.IsEqualTo(singletonService);
    }

    [Test]
    public async Task Collection_Should_Handle_Multiple_Services() {
        // Arrange
        var collection = new ServiceCollection();

        const int serviceCount = 1000;// Number of services to generate
        Dictionary<Type, Type> generatedServices = ServiceHelper.GenerateServices(serviceCount);

        // Register each dynamically created service in the collection
        foreach ((Type interfaceType, Type implementationType) in generatedServices) {
            // Here, adapt this to match your service registration method
            collection.AddSingleton(interfaceType, implementationType);
        }

        // Act
        IServiceProvider provider = collection.Build();

        // Assert
        foreach ((Type interfaceType, Type implementationType) in generatedServices) {
            object? service = provider.GetService(interfaceType);
            await Assert.That(service)
                .IsNotNull()
                .And.IsTypeOf(implementationType);
        }
    }
}
