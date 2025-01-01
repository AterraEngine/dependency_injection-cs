// // ---------------------------------------------------------------------------------------------------------------------
// // Imports
// // ---------------------------------------------------------------------------------------------------------------------
// using Microsoft.Extensions.DependencyInjection;
//
// namespace AterraEngine.DependencyInjection.Generators.Sample;
// // ---------------------------------------------------------------------------------------------------------------------
// // Code
// // ---------------------------------------------------------------------------------------------------------------------
// // [InjectableService<ISecondDualExampleService>(ServiceLifetime.Singleton)]
// [InjectableService<IFirstDualExampleService>(ServiceLifetime.Singleton)]
// public class DualExampleService : IFirstDualExampleService, ISecondDualExampleService;
//
// public interface IFirstDualExampleService;
//
// public interface ISecondDualExampleService;

namespace TestProject;
        
[AterraEngine.DependencyInjection.PooledInjectableService<IExamplePooled, ExamplePooled>]
public class ExamplePooled : IExamplePooled {
    public bool Reset() => true;
}
        
public interface IExamplePooled : AterraEngine.DependencyInjection.IManualPoolable;