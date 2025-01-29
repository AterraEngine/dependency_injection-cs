// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Services;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ScopedProviderRequiredService(IScopedProvider serviceProvider) : IScopedProviderRequiredService {
    public IScopedProvider ScopedProvider { get; set; } = serviceProvider;
}

public interface IScopedProviderRequiredService {
    public IScopedProvider ScopedProvider { get; }
}
