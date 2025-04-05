// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using Tests.AterraEngine.DependencyInjection.Helpers;
using Tests.AterraEngine.DependencyInjection.Services;

namespace Tests.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public class ActivatorHelperTests {

    private ITieredServiceProvider BuildServiceProvider(LifetimeSwitch lifetimeSwitch) {
        var collection = new TieredServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>().AddTransient<ISampleService, SampleService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>().AddSingleton<ISampleService, SampleService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>().AddScoped<ISampleService, SampleService>()
        );

        ITieredServiceProvider provider = collection.Build();
        return provider;
    }


    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task CreateInstance_ShouldReturnInstance_SingleDependencyDepth(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        ITieredServiceProvider provider = BuildServiceProvider(lifetimeSwitch);

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
        ITieredServiceProvider provider = BuildServiceProvider(lifetimeSwitch);

        // Act
        var instance = ActivatorHelper.CreateInstance<CustomClass2>(provider);

        // Assert
        await Assert.That(instance).IsNotNull();
    }

    #pragma warning disable CS9113 // Parameter is unread.
    [UsedImplicitly] private class CustomClass(IEmptyService service);
    [UsedImplicitly] private class CustomClass2(IEmptyService service, ISampleService sampleService);
    #pragma warning restore CS9113 // Parameter is unread.
}
