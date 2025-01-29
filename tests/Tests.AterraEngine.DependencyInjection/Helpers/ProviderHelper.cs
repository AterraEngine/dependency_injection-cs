// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Helpers;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ProviderHelper {
    public static IScopedProvider CreateEmptyServiceProvider() {
        // Populate collection
        var collection = new ServiceCollection();

        // Create provider
        IScopedProvider provider = collection.Build();
        return provider;
    }

    public static IScopedProvider CreateServiceProviderWithGeneratedServices(int count = 100, Action<IServiceCollection>? configureServices = null) {
        // Populate collection
        var collection = new ServiceCollection();

        foreach ((Type? serviceType, Type? implementationType) in ServiceHelper.GenerateServices(count)) {
            collection.AddSingleton(serviceType, implementationType);
        }

        configureServices?.Invoke(collection);

        // Create provider
        IScopedProvider provider = collection.Build();
        return provider;
    }
}
