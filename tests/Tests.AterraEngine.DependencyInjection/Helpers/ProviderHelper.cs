// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Tests.AterraEngine.DependencyInjection.Helpers;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ProviderHelper {
    public static IServiceProvider CreateEmptyServiceProvider() {
        // Populate collection
        var collection = new ServiceCollection();

        // Create provider
        IServiceProvider provider = collection.Build();
        return provider;
    }

    public static IServiceProvider CreateServiceProviderWithGeneratedServices(int count = 100, Action<IServiceCollection>? configureServices = null) {
        // Populate collection
        var collection = new ServiceCollection();

        foreach ((Type? serviceType, Type? implementationType) in ServiceHelper.GenerateServices(count)) {
            collection.AddSingleton(serviceType, implementationType);
        }

        configureServices?.Invoke(collection);

        // Create provider
        IServiceProvider provider = collection.Build();
        return provider;
    }
}
