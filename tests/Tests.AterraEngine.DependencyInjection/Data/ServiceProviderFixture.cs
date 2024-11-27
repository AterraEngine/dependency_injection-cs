// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Data;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProviderFixture : IDisposable {
    public ServiceProvider ServiceProvider { get; }

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public ServiceProviderFixture() {
        var services = new ServiceCollection();
        services.RegisterServicesFromTestsAterraEngineDependencyInjection();
        ServiceProvider = services.BuildServiceProvider();
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void Dispose() {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}