// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using AterraEngine.DependencyInjection.Services;

namespace Tests.AterraEngine.DependencyInjection.EdgeCases;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceFromInterfaceFactoryTests {
    [Test]
    public async Task Should_Resolve_Service_From_Interface_Factory() {
        // Arrange
        var collection = new TieredServiceCollection();
        collection.AddTransientFromFactory<IService, IFactory>();
        collection.AddSingletonFromFactory<IFactory>(static provider => {
            var factory = new Factory(provider);
            return factory;
        });

        ITieredServiceProvider provider = collection.Build();

        // Act
        var service = provider.GetRequiredService<IService>();

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<Service>();
    }

    #pragma warning disable CS9113// Parameter is unread.
    public interface IService;

    public interface IFactory : IFactoryService<IService>;

    public class Service : IService;

    public class Factory(ITieredServiceProvider provider) : IFactory {
        public IService Create(ITieredServiceProvider _) => new Service();
    }
    #pragma warning restore CS9113// Parameter is unread.
}
