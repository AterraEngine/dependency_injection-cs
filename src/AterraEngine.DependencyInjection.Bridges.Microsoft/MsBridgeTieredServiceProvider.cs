// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection.Bridges.Microsoft;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class MsBridgeTieredServiceProvider(TieredServiceContainer serviceContainer, IServiceProvider msServiceProvider) : TieredServiceProvider(serviceContainer) {
    public override TService? GetService<TService>() where TService : class 
        => base.GetService<TService>() ?? msServiceProvider.GetService<TService>();

    public override TService GetRequiredService<TService>() 
        // yes I know this is not RequiredService, but the issue is that we need a way to check
        //      if the services exist in the ms container or ours.
        //      The Required needs to fail at some point to resolve an incorrect service, and this way we are not making multiple try catch blocks.
        => msServiceProvider.GetService<TService>() ?? base.GetRequiredService<TService>();

    protected override ITieredServiceProvider NewProvider(int serviceDepth) {
        IServiceProvider serviceProvider = msServiceProvider.CreateScope().ServiceProvider;
        
        return new MsBridgeTieredServiceProvider(ServiceContainer, serviceProvider) {
            ParentScope = this,
            ServiceDepth = serviceDepth
        };
    }
}
