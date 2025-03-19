// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection.Bridges.Microsoft;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class MsBridgeServiceCollection: ServiceCollection {
    public global::Microsoft.Extensions.DependencyInjection.ServiceCollection MsServiceCollection { get; } = new();

    public override IScopedProvider Build() {
        var container = (ServiceContainer)ServiceContainer.FromCollection(this);
        return new MsBridgeScopedProvider(container, MsServiceCollection.BuildServiceProvider());
    }
}
