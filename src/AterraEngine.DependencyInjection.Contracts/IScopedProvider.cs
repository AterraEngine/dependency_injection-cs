// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IScopedProvider : IReadOnlyCollection<IServiceRecord>, IDisposable, IAsyncDisposable {
    TService? GetService<TService>() where TService : class;
    object? GetService(Type serviceType);

    TService GetRequiredService<TService>() where TService : class;
    object GetRequiredService(Type serviceType);

    IScopedProvider CreateNewScope();
    IScopedProvider CreateNewDeeperScope();
}
