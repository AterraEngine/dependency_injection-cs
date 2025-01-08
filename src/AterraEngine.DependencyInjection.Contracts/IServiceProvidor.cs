// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceProvider : IReadOnlyCollection<IServiceRecord> {
    TService? GetService<TService>() where TService : class;
    object? GetService(Type service);
    
    TService GetRequiredService<TService>() where TService : class;
}
