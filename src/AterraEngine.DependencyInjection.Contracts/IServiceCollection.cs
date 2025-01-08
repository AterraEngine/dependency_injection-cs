// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceCollection : ICollection<IServiceRecord> {
    IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class;
    IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService;
    IServiceCollection AddService(Type implementation, int scopeLevel);
    IServiceCollection AddService(Type service, Type implementation, int scopeLevel);

    IServiceCollection AddSingleton<TImplementation>() where TImplementation : class;
    IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService;
    IServiceCollection AddSingleton(Type implementation);
    IServiceCollection AddSingleton(Type service, Type implementation);
    
    IServiceCollection AddTransient<TImplementation>() where TImplementation : class;
    IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService;
    IServiceCollection AddTransient(Type implementation);
    IServiceCollection AddTransient(Type service, Type implementation);
    
    IServiceProvider Build();
}
