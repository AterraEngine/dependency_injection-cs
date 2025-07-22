// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class RootTieredServiceProvider(TieredServiceContainer serviceContainer) : TieredServiceProvider(serviceContainer) {
    protected override TService ResolveServiceByScope<TService>(FrozenServiceRecord record) {
        if (record.Depth is FrozenServiceRecord.KnownServiceDepth.CustomTier or FrozenServiceRecord.KnownServiceDepth.ProviderScoped) {
            throw new InvalidOperationException(
                "Custom scoped services can only be resolved in a child scope, not in the root scope. This happens when you have a Transient or Singleton service that depends on a custom scoped service."
            );
        }
        return base.ResolveServiceByScope<TService>(record);
    }

    protected override ITieredServiceProvider NewProvider(int serviceDepth) =>  throw new InvalidOperationException("Cannot create a new scope from the container root scope.");
}
