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


public class a {
    public void b() {
        var collection = new ServiceCollection();
        
        
        collection.AddSingleton<ITestService, TestService>();
    }
}