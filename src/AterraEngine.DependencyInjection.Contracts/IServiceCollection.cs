// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceTypes;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceCollection : ICollection<IServiceRecord> {
    IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class;
    IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService;
    IServiceCollection AddService(Type implementation, int scopeLevel);
    IServiceCollection AddService(Type service, Type implementation, int scopeLevel);
    IServiceCollection AddServiceFromFactory<TService>(Func<IScopedProvider, TService> factory, int scopeLevel) where TService : class;
    IServiceCollection AddServiceFromFactory<TService, TFactoryService>(int scopeLevel) where TFactoryService : class, IFactoryService<TService> where TService : class;
    IServiceCollection AddServiceFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory, int scopeLevel) where TService : class;
    IServiceCollection AddServiceFromAsyncFactory<TService, TAsyncFactoryService>(int scopeLevel) where TAsyncFactoryService : class, IAsyncFactoryService<TService> where TService : class;

    IServiceCollection AddSingleton<TImplementation>() where TImplementation : class;
    IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService;
    IServiceCollection AddSingleton(Type implementation);
    IServiceCollection AddSingleton(Type service, Type implementation);
    IServiceCollection AddSingletonFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    IServiceCollection AddSingletonFromFactory<TService, TFactoryService>() where TFactoryService : class, IFactoryService<TService> where TService : class;
    IServiceCollection AddSingletonFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class;
    IServiceCollection AddSingletonFromAsyncFactory<TService, TAsyncFactoryService>() where TAsyncFactoryService : class, IAsyncFactoryService<TService> where TService : class;

    IServiceCollection AddTransient<TImplementation>() where TImplementation : class;
    IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService;
    IServiceCollection AddTransient(Type implementation);
    IServiceCollection AddTransient(Type service, Type implementation);
    IServiceCollection AddTransientFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    IServiceCollection AddTransientFromFactory<TService, TFactoryService>() where TFactoryService : class, IFactoryService<TService> where TService : class;
    IServiceCollection AddTransientFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class;
    IServiceCollection AddTransientFromAsyncFactory<TService, TAsyncFactoryService>() where TAsyncFactoryService : class, IAsyncFactoryService<TService> where TService : class;

    IServiceCollection AddScoped<TImplementation>() where TImplementation : class;
    IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService;
    IServiceCollection AddScoped(Type implementation);
    IServiceCollection AddScoped(Type service, Type implementation);
    IServiceCollection AddScopedFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    IServiceCollection AddScopedFromFactory<TService, TFactoryService>() where TFactoryService : class, IFactoryService<TService> where TService : class;
    IServiceCollection AddScopedFromAsyncFactory<TService>(Func<IScopedProvider, ValueTask<TService>> factory) where TService : class;
    IServiceCollection AddScopedFromAsyncFactory<TService, TAsyncFactoryService>() where TAsyncFactoryService : class, IAsyncFactoryService<TService> where TService : class;

    IScopedProvider Build();
}
