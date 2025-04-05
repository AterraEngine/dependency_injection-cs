// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface ITieredServiceProvider : IDisposable, IAsyncDisposable {
    TService? GetService<TService>() where TService : class;

    [RequiresDynamicCode("This method uses reflection to get the service.")]
    object? GetService(Type serviceType);

    TService GetRequiredService<TService>() where TService : class;

    [RequiresDynamicCode("This method uses reflection to get the service.")]
    object GetRequiredService(Type serviceType);

    ITieredServiceProvider CreateNewScope();
    ITieredServiceProvider CreateNewTier();
}
