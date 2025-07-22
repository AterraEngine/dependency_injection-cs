// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection.ServiceRecords;
using AterraEngine.DependencyInjection.Services;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface ITieredServiceCollection : ICollection<IServiceRecord> {
    ITieredServiceProvider Build();
    #region AddService
    ITieredServiceCollection AddService(IServiceRecord record);

    ITieredServiceCollection AddService<TImplementation>(int serviceDepth) where TImplementation : class;

    ITieredServiceCollection AddService<TService, TImplementation>(int serviceDepth) where TImplementation : class, TService;

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddService(Type implementation, int serviceDepth);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddService(Type service, Type implementation, int serviceDepth);

    ITieredServiceCollection AddServiceFromFactory<TService>(Func<ITieredServiceProvider, TService> factory, int serviceDepth) where TService : class;

    ITieredServiceCollection AddServiceFromFactory<TService, TFactory>(int serviceDepth) where TFactory : class, IFactoryService<TService> where TService : class;

    ITieredServiceCollection AddService<TService>(TService instance, int serviceDepth) where TService : class;
    #endregion

    #region AddSingleton
    ITieredServiceCollection AddSingleton<TImplementation>() where TImplementation : class;

    ITieredServiceCollection AddSingleton<TService, TImplementation>() where TImplementation : class, TService;

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddSingleton(Type implementation);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddSingleton(Type service, Type implementation);

    ITieredServiceCollection AddSingletonFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class;

    ITieredServiceCollection AddSingletonFromFactory<TService, TFactory>(int? serviceDepthFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;

    ITieredServiceCollection AddSingleton<TService>(TService instance) where TService : class;
    #endregion

    #region AddTransient
    ITieredServiceCollection AddTransient<TImplementation>() where TImplementation : class;

    ITieredServiceCollection AddTransient<TService, TImplementation>() where TImplementation : class, TService;

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddTransient(Type implementation);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddTransient(Type service, Type implementation);

    ITieredServiceCollection AddTransientFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class;

    ITieredServiceCollection AddTransientFromFactory<TService, TFactory>(int? serviceDepthFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion

    #region AddScoped
    ITieredServiceCollection AddScoped<TImplementation>() where TImplementation : class;

    ITieredServiceCollection AddScoped<TService, TImplementation>() where TImplementation : class, TService;

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddScoped(Type implementation);

    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    ITieredServiceCollection AddScoped(Type service, Type implementation);

    ITieredServiceCollection AddScopedFromFactory<TService>(Func<ITieredServiceProvider, TService> factory) where TService : class;

    ITieredServiceCollection AddScopedFromFactory<TService, TFactory>(int? serviceDepthFactory = null) where TFactory : class, IFactoryService<TService> where TService : class;
    #endregion
    
    #region AddEnumerableService
    ITieredServiceCollection AddEnumerableService<TService, TImplementation>(int serviceDepth) where TImplementation : class, TService;

    ITieredServiceCollection AddEnumerableService<TService, TImplementation>(TImplementation instance, int serviceDepth) where TImplementation : class, TService;
    
    ITieredServiceCollection AddEnumerableSingleton<TService, TImplementation>() where TImplementation : class, TService;
    
    ITieredServiceCollection AddEnumerableSingleton<TService, TImplementation>(TImplementation instance) where TImplementation : class, TService;

    ITieredServiceCollection AddEnumerableTransient<TService, TImplementation>() where TImplementation : class, TService;

    ITieredServiceCollection AddEnumerableScoped<TService, TImplementation>() where TImplementation : class, TService;
    #endregion
}
