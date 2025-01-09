// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Tests.AterraEngine.DependencyInjection.Services;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProviderRequiredService(IServiceProvider serviceProvider) : IServiceProviderRequiredService {
    public IServiceProvider ServiceProvider { get; set; } = serviceProvider;
}

public interface IServiceProviderRequiredService {
    public IServiceProvider ServiceProvider { get; }
}
