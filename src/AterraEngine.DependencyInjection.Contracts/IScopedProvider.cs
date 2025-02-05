// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IScopedProvider : IReadOnlyCollection<IServiceRecord>, IDisposable, IAsyncDisposable, IServiceProvider {
    TService? GetService<TService>() where TService : class;

    TService GetRequiredService<TService>() where TService : class;
    object GetRequiredService(Type service);

    IScopedProvider CreateNewScope();
    IScopedProvider CreateDeeperScope();
}
