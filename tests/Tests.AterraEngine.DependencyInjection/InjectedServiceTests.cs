// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;
using Tests.AterraEngine.DependencyInjection.Data;

namespace Tests.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class InjectedServicesTest {
    [Test]
    public async Task Test_DuckyService() {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterServicesFromTestsAterraEngineDependencyInjection();
        ServiceProvider provider = services.BuildServiceProvider();
        
        // Act
        var duckyService = provider.GetService<IDuckyService>();
        var duckyFactory = provider.GetService<IDuckyFactory>();
        var ducky = provider.GetService<IDucky>();

        // Assert
        await Assert.That(duckyService).IsNotNull().And.IsTypeOf<DuckyService>();
        await Assert.That(duckyFactory).IsNotNull().And.IsTypeOf<DuckyFactory>();
        await Assert.That(ducky).IsNotNull().And.IsTypeOf<Ducky>();
    }
}
