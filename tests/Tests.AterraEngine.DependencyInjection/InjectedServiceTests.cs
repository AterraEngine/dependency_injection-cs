// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using Tests.AterraEngine.DependencyInjection.Data;

namespace Tests.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class InjectedServicesTest(ServiceProviderFixture fixture) : IClassFixture<ServiceProviderFixture> {
    [Fact]
    public void Test_DuckyService() {
        ServiceProvider provider = fixture.ServiceProvider;

        Assert.IsType<Ducky>(provider.GetRequiredService<IDucky>());
        Assert.IsType<DuckyFactory>(provider.GetRequiredService<IDuckyFactory>());
        Assert.IsType<DuckyService>(provider.GetRequiredService<IDuckyService>());
    }
}
