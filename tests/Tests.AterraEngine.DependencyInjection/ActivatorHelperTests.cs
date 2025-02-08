// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using Tests.AterraEngine.DependencyInjection.Helpers;
using Tests.AterraEngine.DependencyInjection.Services;

namespace Tests.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ActivatorHelperTests {

    private IScopedProvider BuildServiceProvider(LifetimeSwitch lifetimeSwitch) {var collection = new ServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>().AddTransient<ISampleService, SampleService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>().AddSingleton<ISampleService, SampleService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>().AddScoped<ISampleService, SampleService>()
        );
        IScopedProvider provider = collection.Build();
        return provider;
    }
    
    
    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task CreateInstance_ShouldReturnInstance_SingleDependencyDepth(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        IScopedProvider provider = BuildServiceProvider(lifetimeSwitch);
        
        // Act
        var instance = ActivatorHelper.CreateInstance<CustomClass>(provider);

        // Assert
        await Assert.That(instance).IsNotNull();
    }
    
    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task CreateInstance_ShouldReturnInstance_DoubleDependencyDepth(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        IScopedProvider provider = BuildServiceProvider(lifetimeSwitch);
        
        // Act
        var instance = ActivatorHelper.CreateInstance<CustomClass2>(provider);

        // Assert
        await Assert.That(instance).IsNotNull();
    }

    public class CustomClass(IEmptyService service);
    public class CustomClass2(IEmptyService service, ISampleService sampleService);
}
