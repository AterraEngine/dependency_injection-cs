// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Data;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProviderFixture : IDisposable {

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public ServiceProviderFixture() {
        var services = new ServiceCollection();
        services.RegisterServicesFromTestsAterraEngineDependencyInjection();
        ServiceProvider = services.BuildServiceProvider();
    }
    public ServiceProvider ServiceProvider { get; }

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void Dispose() {
        ServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
