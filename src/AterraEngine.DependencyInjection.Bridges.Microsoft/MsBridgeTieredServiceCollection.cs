// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection.Bridges.Microsoft;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class MsBridgeTieredServiceCollection: TieredServiceCollection {
    public ServiceCollection MsServiceCollection { get; } = new();

    public override ITieredServiceProvider Build() {
        var container = (TieredServiceContainer)TieredServiceContainer.FromCollection(this);
        return new MsBridgeTieredServiceProvider(container, MsServiceCollection.BuildServiceProvider());
    }
}
