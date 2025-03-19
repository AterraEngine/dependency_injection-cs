// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainerScopedProvider(ServiceContainer serviceContainer) : ScopedProvider(serviceContainer) {
    protected override TService ResolveServiceByScope<TService>(FrozenServiceRecord record) {
        if (record.Depth is FrozenServiceRecord.KnownScopeDepth.CustomScoped or FrozenServiceRecord.KnownScopeDepth.ProviderScoped) {
            throw new InvalidOperationException(
                "Custom scoped services can only be resolved in a child scope, not in the root scope. This happens when you have a Transient or Singleton service that depends on a custom scoped service."
            );
        }
        return base.ResolveServiceByScope<TService>(record);
    }

    protected override IScopedProvider NewScopeProvider(int scopeLevel) =>  throw new InvalidOperationException("Cannot create a new scope from the container root scope.");
}
