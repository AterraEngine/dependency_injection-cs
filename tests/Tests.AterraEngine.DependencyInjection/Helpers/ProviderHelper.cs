// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Helpers;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ProviderHelper {
    public static ITieredServiceProvider CreateEmptyServiceProvider() {
        // Populate collection
        var collection = new TieredServiceCollection();

        // Create provider
        ITieredServiceProvider provider = collection.Build();
        return provider;
    }

    public static ITieredServiceProvider CreateServiceProviderWithGeneratedServices(int count = 100, Action<ITieredServiceCollection>? configureServices = null) {
        // Populate collection
        var collection = new TieredServiceCollection();

        foreach ((Type? serviceType, Type? implementationType) in ServiceHelper.GenerateServices(count)) {
            collection.AddSingleton(serviceType, implementationType);
        }

        configureServices?.Invoke(collection);

        // Create provider
        ITieredServiceProvider provider = collection.Build();
        return provider;
    }
}
