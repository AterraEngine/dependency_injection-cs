// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using AterraEngine.DependencyInjection.ServiceRecords;
using System.Diagnostics.CodeAnalysis;
using Tests.AterraEngine.DependencyInjection.Helpers;
using Tests.AterraEngine.DependencyInjection.Services;

namespace Tests.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public class TieredServiceCollectionTests {
    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_Should_Return_Service_Provider(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>()
        );

        // Act
        ITieredServiceProvider provider = collection.Build();
        var service = provider.GetService<IEmptyService>();
        var container = provider.GetService<ITieredServiceContainer>();

        // Assert
        await Assert.That(container)
            .IsNotNull()
            .And.IsTypeOf<TieredServiceContainer>()
            .And.HasCount().EqualTo(1);

        await Assert.That(service)
            .IsNotNull();
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_Should_Return_Service_Provider_With_Multiple_Services(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();

        // Add multiple services
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>().AddTransient<ISampleService, SampleService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>().AddSingleton<ISampleService, SampleService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>().AddScoped<ISampleService, SampleService>()
        );

        // Act
        ITieredServiceProvider provider = collection.Build();

        var emptyService = provider.GetService<IEmptyService>();
        var sampleService = provider.GetService<ISampleService>();
        var container = provider.GetService<ITieredServiceContainer>();

        // Assert
        await Assert.That(container)
            .IsNotNull()
            .And.IsTypeOf<TieredServiceContainer>()
            .And.HasCount().EqualTo(2);// Expecting two services registered

        await Assert.That(emptyService)
            .IsNotNull();

        await Assert.That(sampleService)
            .IsNotNull();
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_Should_Handle_TieredServiceProvider_Service(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<ITieredServiceProviderRequiredService, TieredServiceProviderRequiredService>(),
            onSingleton: () => collection.AddSingleton<ITieredServiceProviderRequiredService, TieredServiceProviderRequiredService>(),
            onScoped: () => collection.AddScoped<ITieredServiceProviderRequiredService, TieredServiceProviderRequiredService>()
        );

        // Act
        ITieredServiceProvider provider = collection.Build();
        var container = provider.GetService<ITieredServiceContainer>();
        var service = provider.GetService<ITieredServiceProviderRequiredService>();

        // Assert
        await Assert.That(container)
            .IsNotNull()
            .And.IsTypeOf<TieredServiceContainer>()
            .And.HasCount().EqualTo(1);

        await Assert.That(service).IsTypeOf<TieredServiceProviderRequiredService>();
        if (lifetimeSwitch == LifetimeSwitch.Scoped) {
            await Assert.That(service).HasMember(p => p!.TieredServiceProvider).EqualTo(provider);
        } else {
            await Assert.That(service).HasMember(p => p!.TieredServiceProvider).EqualTo(((TieredServiceContainer)container!).ContainerProvider.Value);
        }
    }

    [Test]
    public async Task Collection_Should_Handle_Scopes() {
        // Arrange
        var collection = new TieredServiceCollection();
        collection.AddSingleton<IEmptyService, EmptyService>();
        collection.AddService<IIdService, IdService>(serviceDepth: 1);
        ITieredServiceProvider globalProvider = collection.Build();

        // Act
        var singletonService = globalProvider.GetService<IEmptyService>();
        ITieredServiceProvider scope0 = globalProvider.CreateNewTier();
        var scope0Service = scope0.GetService<IIdService>();
        var scope0SingletonService = scope0.GetService<IEmptyService>();

        ITieredServiceProvider scope1 = globalProvider.CreateNewTier();
        var scope1Service = scope1.GetService<IIdService>();
        var scope1SingletonService = scope0.GetService<IEmptyService>();

        // Assert
        await Assert.That(singletonService).IsNotNull()
            .And.IsEqualTo(scope0SingletonService)
            .And.IsEqualTo(scope1SingletonService);

        await Assert.That(scope0Service).IsNotNull()
            .And.IsNotEqualTo(scope1Service)
            .And.HasMember(s => s!.Id).NotEqualTo(scope1Service!.Id);

        await Assert.That(scope0SingletonService).IsNotNull()
            .And.IsEqualTo(singletonService)
            .And.IsEqualTo(scope1SingletonService);

        await Assert.That(scope1Service).IsNotNull()
            .And.IsNotEqualTo(scope0Service)
            .And.HasMember(s => s!.Id).NotEqualTo(scope0Service!.Id);

        await Assert.That(scope1SingletonService).IsNotNull()
            .And.IsEqualTo(scope0SingletonService)
            .And.IsEqualTo(singletonService);
    }

    [Test]
    public async Task Collection_Should_Handle_Multiple_Services() {
        // Arrange
        var collection = new TieredServiceCollection();

        const int serviceCount = 1000;// Number of services to generate
        Dictionary<Type, Type> data = ServiceHelper.GenerateServices(serviceCount).ToDictionary();

        // Register each dynamically created service in the collection
        foreach ((Type interfaceType, Type implementationType) in data) {
            // Here, adapt this to match your service registration method
            collection.AddSingleton(interfaceType, implementationType);
        }

        // Act
        ITieredServiceProvider provider = collection.Build();

        // Assert
        foreach ((Type interfaceType, Type implementationType) in data) {
            object? service = provider.GetService(interfaceType);
            await Assert.That(service)
                .IsNotNull()
                .And.IsTypeOf(implementationType);
        }
    }

    [Test]
    public async Task Collection_AddServiceFromFactory_ShouldAddService() {
        // Arrange
        var collection = new TieredServiceCollection();

        // Act
        collection.AddService<ExampleFactoryService>((int)DefaultServiceDepth.Singleton);
        collection.AddServiceFromFactory<IFactoryCreatedService, ExampleFactoryService>((int)DefaultServiceDepth.Transient);

        // Assert
        Dictionary<Type, IServiceRecord> records = collection.ToDictionary(
            keySelector: static record => record.ServiceType,
            elementSelector: static record => record
        );

        await Assert.That(collection).HasCount().EqualTo(2);
        await Assert.That(records.ContainsKey(typeof(IFactoryCreatedService))).IsTrue();
        await Assert.That(records.ContainsKey(typeof(ExampleFactoryService))).IsTrue();

        IServiceRecord factoryRecord = records[typeof(ExampleFactoryService)];
        IServiceRecord serviceRecord = records[typeof(IFactoryCreatedService)];

        await Assert.That(factoryRecord).IsNotNull()
            .And.IsAssignableTo<IServiceRecord>()
            .And.HasMember(static bool (record) => record.IsTransient).EqualTo(false).Because("Should not be transient")
            .And.HasMember(static bool (record) => record.IsSingleton).EqualTo(true).Because("Should be a singleton");

        await Assert.That(serviceRecord).IsNotNull()
            .And.IsAssignableTo<IServiceRecord>()
            .And.HasMember(static bool (record) => record.IsTransient).EqualTo(true).Because("Should not be transient")
            .And.HasMember(static bool (record) => record.IsSingleton).EqualTo(false).Because("Should be a singleton");
    }

    [Test]
    public async Task Collection_AddServiceFromFactory_ShouldAddService_SameScope() {
        // Arrange
        var collection = new TieredServiceCollection();

        // Act
        collection.AddService<ExampleFactoryService>((int)DefaultServiceDepth.Transient);
        collection.AddServiceFromFactory<IFactoryCreatedService, ExampleFactoryService>((int)DefaultServiceDepth.Transient);

        // Assert
        Dictionary<Type, IServiceRecord> records = collection.ToDictionary(
            keySelector: static record => record.ServiceType,
            elementSelector: static record => record
        );

        await Assert.That(collection).HasCount().EqualTo(2);
        await Assert.That(records.ContainsKey(typeof(IFactoryCreatedService))).IsTrue();
        await Assert.That(records.ContainsKey(typeof(ExampleFactoryService))).IsTrue();

        IServiceRecord factoryRecord = records[typeof(ExampleFactoryService)];
        IServiceRecord serviceRecord = records[typeof(IFactoryCreatedService)];

        await Assert.That(factoryRecord).IsNotNull()
            .And.IsAssignableTo<IServiceRecord>()
            .And.HasMember(static bool (record) => record.IsTransient).EqualTo(true).Because("Should be transient")
            .And.HasMember(static bool (record) => record.IsSingleton).EqualTo(false).Because("Should not be a singleton");

        await Assert.That(serviceRecord).IsNotNull()
            .And.IsAssignableTo<IServiceRecord>()
            .And.HasMember(static bool (record) => record.IsTransient).EqualTo(true).Because("Should not be transient")
            .And.HasMember(static bool (record) => record.IsSingleton).EqualTo(false).Because("Should be a singleton");
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_IsReadOnly_Should_Return_False(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        TieredServiceCollection collection = [];
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>()
        );

        // Act
        bool isReadOnly = collection.IsReadOnly;

        // Assert
        await Assert.That(isReadOnly).IsFalse();
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_IsReadOnly_Should_Return_True(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>()
        );

        ITieredServiceProvider provider = collection.Build();

        // Act
        bool isReadOnly = collection.IsReadOnly;

        // Assert
        await Assert.That(provider).IsNotNull();
        await Assert.That(isReadOnly).IsTrue();
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_ShouldThrow_WhenAddingAfterBuild(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient<IEmptyService, EmptyService>(),
            onSingleton: () => collection.AddSingleton<IEmptyService, EmptyService>(),
            onScoped: () => collection.AddScoped<IEmptyService, EmptyService>()
        );

        ITieredServiceProvider provider = collection.Build();

        // Act & Assert
        await Assert.That(provider).IsNotNull();
        Assert.Throws<InvalidOperationException>(() => collection.AddSingleton<IEmptyService, EmptyService>());
    }

    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_ShouldAllow_RegisteringGenericServices(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();

        // Act
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics<,>)),
            onSingleton: () => collection.AddSingleton(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics<,>)),
            onScoped: () => collection.AddScoped(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics<,>))
        );

        // Assert
        await Assert.That(collection).HasCount().EqualTo(1);
        await Assert.That(collection.Records).ContainsKey(typeof(IServiceWithGenerics<,>));
    }
    
    [Test]
    [Arguments(LifetimeSwitch.Transient)]
    [Arguments(LifetimeSwitch.Singleton)]
    [Arguments(LifetimeSwitch.Scoped)]
    public async Task Collection_ShouldAllow_RegisteringGenericServices_ImplementationOnly(LifetimeSwitch lifetimeSwitch) {
        // Arrange
        var collection = new TieredServiceCollection();

        // Act
        LifetimeSwitcher.On(lifetimeSwitch,
            onTransient: () => collection.AddTransient(typeof(ServiceWithGenerics<,>)),
            onSingleton: () => collection.AddSingleton(typeof(ServiceWithGenerics<,>)),
            onScoped: () => collection.AddScoped(typeof(ServiceWithGenerics<,>))
        );

        // Assert
        await Assert.That(collection).HasCount().EqualTo(1);
        await Assert.That(collection.Records).ContainsKey(typeof(ServiceWithGenerics<,>));
    }
    
    [Test]
    public async Task Collection_Singleton_AddFromInstance() {
        // Arrange
        var collection = new TieredServiceCollection();
        var instance = new EmptyService();
        
        // Act
        collection.AddSingleton(instance);
        
        //Assert
        await Assert.That(collection).HasCount().EqualTo(1);
        await Assert.That(collection.Records).ContainsKey(typeof(EmptyService));
    }

    [Test]
    public async Task Collection_ShouldAddEnumerableService() {
        // Arrange
        var collection = new TieredServiceCollection();
        
        // Act
        collection.AddEnumerableService<IEnumerableCommonInterface, EnumerableService1>((int)DefaultServiceDepth.Singleton);
        collection.AddEnumerableService<IEnumerableCommonInterface, EnumerableService2>((int)DefaultServiceDepth.Singleton);

        // Assert
        await Assert.That(collection).HasCount().EqualTo(3);
        await Assert.That(collection.Records)
            .ContainsKey(typeof(IEnumerable<IEnumerableCommonInterface>))
            .And.ContainsKey(typeof(EnumerableService1))
            .And.ContainsKey(typeof(EnumerableService2));
    }
    
    [Test]
    public Task Collection_ShouldThrow_AddEnumerableService_DifferentScope() {
        // Arrange
        var collection = new TieredServiceCollection();
        
        // Act
        collection.AddEnumerableService<IEnumerableCommonInterface, EnumerableService1>((int)DefaultServiceDepth.Singleton);
        
        // Assert
        Assert.Throws<InvalidOperationException>(() => {
            collection.AddEnumerableService<IEnumerableCommonInterface, EnumerableService2>(1000);
        });

        return Task.CompletedTask;
    }

    [Test]
    public async Task AddEnumerableService_Succeeds_WhenServiceIsInstance() {
        // Arrange
        var collection = new TieredServiceCollection();
        var instance = new EnumerableService1();
        var instance2 = new EnumerableService2();
        
        // Act
        collection.AddEnumerableService<IEnumerableCommonInterface,EnumerableService1>(instance, (int)DefaultServiceDepth.Singleton);
        collection.AddEnumerableService<IEnumerableCommonInterface,EnumerableService2>(instance2, (int)DefaultServiceDepth.Singleton);

        // Assert
        await Assert.That(collection).HasCount().EqualTo(3);
        await Assert.That(collection.Records)
            .ContainsKey(typeof(IEnumerable<IEnumerableCommonInterface>))
            .And.ContainsKey(typeof(EnumerableService1));
    }
}
