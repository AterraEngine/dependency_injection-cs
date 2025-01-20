// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
// ReSharper disable once ClassNeverInstantiated.Global
public class SampleService(IEmptyService emptyService) : ISampleService {
    // ReSharper disable once UnusedMember.Global
    public IEmptyService EmptyService { get; } = emptyService;
}

public interface ISampleService;