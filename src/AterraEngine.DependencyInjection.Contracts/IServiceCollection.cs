// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.Services;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceCollection : ICollection<IServiceRecord> {
    #region AddService
    IServiceCollection AddService(IServiceRecord record);
    
    IServiceCollection AddService<TImplementation>(int scopeLevel) where TImplementation : class;
    
    IServiceCollection AddService<TService, TImplementation>(int scopeLevel) where TImplementation : class, TService;
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddService(Type implementation, int scopeLevel);
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddService(Type service, Type implementation, int scopeLevel);
    
    IServiceCollection AddServiceFromFactory<TService>(Func<IScopedProvider, TService> factory, int scopeLevel) where TService : class;
    
    IServiceCollection AddServiceFromFactory<TService, TFactory>(int scopeLevel, int? scopeLevelFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion
    
    #region AddSingleton
    IServiceCollection AddSingleton<TImplementation>() where TImplementation : class;
    
    IServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService;
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddSingleton(Type implementation);
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddSingleton(Type service, Type implementation);
    
    IServiceCollection AddSingletonFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    
    IServiceCollection AddSingletonFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion
    
    #region AddTransient
    IServiceCollection AddTransient<TImplementation>() where TImplementation : class;
    
    IServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService;
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddTransient(Type implementation);
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddTransient(Type service, Type implementation);
    
    IServiceCollection AddTransientFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    
    IServiceCollection AddTransientFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion
    
    #region AddScoped
    IServiceCollection AddScoped<TImplementation>() where TImplementation : class;
    
    IServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService;
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddScoped(Type implementation);
    
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    IServiceCollection AddScoped(Type service, Type implementation);
    
    IServiceCollection AddScopedFromFactory<TService>(Func<IScopedProvider, TService> factory) where TService : class;
    
    IServiceCollection AddScopedFromFactory<TService, TFactory>(int? scopeLevelFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion
    IScopedProvider Build();
}
