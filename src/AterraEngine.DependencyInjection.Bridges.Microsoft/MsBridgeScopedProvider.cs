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
        => msServiceProvider.GetService<TService>() ?? base.GetRequiredService<TService>();

    protected override IScopedProvider NewScopeProvider(int scopeLevel) {
        IServiceProvider scopedProvider = msServiceProvider.CreateScope().ServiceProvider;
        
        return new MsBridgeScopedProvider(ServiceContainer, scopedProvider) {
            ParentScope = this,
            ScopeDepth = scopeLevel
        };
    }
}
