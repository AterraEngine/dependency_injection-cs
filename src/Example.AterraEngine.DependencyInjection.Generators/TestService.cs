// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;

namespace Example.AterraEngine.DependencyInjection.Generators;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------

[Service(typeof(ITestService),0)]
public class TestService : ITestService;

[Service<ITestService>(0)]
public class GenericTestservice<T> : ITestService;

[TransientService<ITestService>()]
public class TransientGenericTestservice<T> : ITestService;

public interface ITestService;

public interface ITestService2;

[Service(typeof(ITestService2),1)]
public class TestService2(ITestService testService) : ITestService2;


public class GeneratorTest {
    public void Foo() {
        var collection = new ServiceCollection();

        // What should be generated =>
        //      static implementation factory calls
        collection.AddService(new ServiceRecord<ITestService>(
            ServiceType: typeof(ITestService),
            ImplementationType: typeof(TestService),
            ImplementationFactory: static _ => new TestService(),
            Lifetime: 0
        ));
        
        collection.AddService(new ServiceRecord<ITestService2>(
            ServiceType: typeof(ITestService2),
            ImplementationType: typeof(TestService2),
            ImplementationFactory: static provider => new TestService2(provider.GetRequiredService<ITestService>()),
            Lifetime: 1
        ));
    }
}