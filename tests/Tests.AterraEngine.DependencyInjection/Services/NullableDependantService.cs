// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
#nullable enable
public class NullableDependantService(IEmptyService? service = null) : INullableDependantService{
    public IEmptyService? Service { get; set; } = service;
}


public interface INullableDependantService {
    IEmptyService? Service { get; }
}
