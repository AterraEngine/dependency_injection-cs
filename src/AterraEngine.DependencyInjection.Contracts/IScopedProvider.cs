// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IScopedProvider : IReadOnlyCollection<IServiceRecord>, IDisposable, IAsyncDisposable {
    ValueTask<TService?> GetServiceAsync<TService>() where TService : class;
    ValueTask<object?> GetServiceAsync(Type service);
    
    ValueTask<TService> GetRequiredServiceAsync<TService>() where TService : class;
    ValueTask<object> GetRequiredServiceAsync(Type service);

    IScopedProvider CreateNewScope();
    IScopedProvider CreateDeeperScope();
}
