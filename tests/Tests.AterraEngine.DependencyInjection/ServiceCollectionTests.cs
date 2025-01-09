// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using System.Reflection;
using System.Reflection.Emit;
using Tests.AterraEngine.DependencyInjection.Services;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Tests.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceCollectionTests {
    [Test]
    public async Task Collection_Should_Return_Service_Provider() {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddSingleton<IEmptyService, EmptyService>();

        // Act
        var provider = collection.Build();
        var service = provider.GetService<IEmptyService>();
        
        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(1);
        await Assert.That(service)
            .IsNotNull();
    }

    [Test]
    public async Task Collection_Should_Return_Service_Provider_With_Multiple_Services()
    {
        // Arrange
        var collection = new ServiceCollection();
            
        // Add multiple services
        collection.AddSingleton<IEmptyService, EmptyService>();
        collection.AddSingleton<ISampleService, SampleService>();

        // Act
        var provider = collection.Build();

        var emptyService = provider.GetService<IEmptyService>();
        var sampleService = provider.GetService<ISampleService>();

        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(2); // Expecting two services registered

        await Assert.That(emptyService)
            .IsNotNull();

        await Assert.That(sampleService)
            .IsNotNull();
    }

    [Test]
    public async Task Collection_Should_Handle_ServiceProvider_Service() {
        // Arrange
        var collection = new ServiceCollection();
        collection.AddSingleton<IServiceProviderRequiredService, ServiceProviderRequiredService>();
        
        // Act
        var provider = collection.Build();
        var service = provider.GetService<IServiceProviderRequiredService>();
        
        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(1);

        await Assert.That(service)
            .IsTypeOf<ServiceProviderRequiredService>()
            .And.HasMember(p => p!.ServiceProvider).EqualTo(provider);
    }
    
    [Test]
    public async Task Collection_Should_Handle_Multiple_Services() {
        // Arrange
        var collection = new ServiceCollection();

        const int serviceCount = 1000;// Number of services to generate
        Dictionary<Type, Type> generatedServices = GenerateServices(serviceCount);

        // Register each dynamically created service in the collection
        foreach ((Type interfaceType, Type implementationType) in generatedServices) {
            // Here, adapt this to match your service registration method
            collection.AddSingleton(interfaceType, implementationType);
        }

        // Act
        IServiceProvider provider = collection.Build();

        // Assert
        foreach ((Type interfaceType, Type implementationType) in generatedServices) {
            object? service = provider.GetService(interfaceType);
            await Assert.That(service)
                .IsNotNull()
                .And.IsTypeOf(implementationType);
        }
    }
    
    private static Dictionary<Type, Type> GenerateServices(int count) {
        var interfaceImplementationPairs = new Dictionary<Type, Type>();

        // Create an in-memory assembly for dynamic types
        var assemblyName = new AssemblyName("DynamicServices");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        for (int i = 0; i < count; i++) {
            // Define a new interface
            string interfaceName = $"IService{i}";
            TypeBuilder interfaceBuilder = moduleBuilder.DefineType(interfaceName,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            Type interfaceType = interfaceBuilder.CreateType();

            // Define a class implementing the interface
            string className = $"Service{i}";
            TypeBuilder classBuilder = moduleBuilder.DefineType(className,
                TypeAttributes.Public | TypeAttributes.Class);
            classBuilder.AddInterfaceImplementation(interfaceType);

            // Create a parameterless constructor for the class
            ConstructorBuilder _ = classBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            Type implementationType = classBuilder.CreateType();

            // Add to the list
            interfaceImplementationPairs.Add(interfaceType, implementationType);
        }

        return interfaceImplementationPairs;
    }
}
