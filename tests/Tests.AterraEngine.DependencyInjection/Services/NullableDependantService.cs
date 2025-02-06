// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class NullableDependantService(IEmptyService? service = null) : INullableDependantService {
    public IEmptyService? Service { get; set; } = service;
}

public interface INullableDependantService {
    IEmptyService? Service { get; }
}
