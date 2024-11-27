// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection.Generators.Sample;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[InjectableService<IFirstDualExampleService>(ServiceLifetime.Singleton)]
public class DualExampleService : IFirstDualExampleService, ISecondDualExampleService;


public interface IFirstDualExampleService;
public interface ISecondDualExampleService;
