// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection.Bridges.Microsoft;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class MsBridgeScopedProvider(ServiceContainer serviceContainer, IServiceProvider msServiceProvider) : ScopedProvider(serviceContainer) {
    public override TService? GetService<TService>() where TService : class 
        => base.GetService<TService>() ?? msServiceProvider.GetService<TService>();

    public override TService GetRequiredService<TService>() 
        // yes I know this is not RequiredService, but the issue is that we need a way to check
        //      if the services exist in the ms container or ours.
        //      The Required needs to fail at some point to resolve an incorrect service, and this way we are not making multiple try catch blocks.
        => msServiceProvider.GetService<TService>() ?? base.GetRequiredService<TService>();

    protected override IScopedProvider NewScopeProvider(int scopeLevel) {
        IServiceProvider scopedProvider = msServiceProvider.CreateScope().ServiceProvider;
        
        return new MsBridgeScopedProvider(ServiceContainer, scopedProvider) {
            ParentScope = this,
            ScopeDepth = scopeLevel
        };
    }
}
