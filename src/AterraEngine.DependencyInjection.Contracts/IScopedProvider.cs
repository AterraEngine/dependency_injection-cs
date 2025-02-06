// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IScopedProvider : IDisposable, IAsyncDisposable {
    TService? GetService<TService>() where TService : class;
    object? GetService(Type serviceType);

    TService GetRequiredService<TService>() where TService : class;
    object GetRequiredService(Type serviceType);

    IScopedProvider CreateNewScope();
    IScopedProvider CreateNewDeeperScope();
}
