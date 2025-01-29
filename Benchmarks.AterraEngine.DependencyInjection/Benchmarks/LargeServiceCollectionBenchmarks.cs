// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Reflection.Emit;
using ServiceCollection=Microsoft.Extensions.DependencyInjection.ServiceCollection;
using ServiceProvider=Microsoft.Extensions.DependencyInjection.ServiceProvider;

namespace Benchmarks.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[MemoryDiagnoser]
// [Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class LargeServiceCollectionBenchmarks {
    [Benchmark(Baseline = true)]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency() {
        var collection = new ServiceCollection();
        const int serviceCount = 1_000;// Number of services to generate
        Dictionary<Type, Type> generatedServices = GenerateServices(serviceCount);

        // Register each dynamically created service in the collection
        foreach ((Type serviceType, Type implementationType) in generatedServices) {
            collection.AddSingleton(serviceType, implementationType);
        }

        ServiceProvider provider = collection.BuildServiceProvider();

        return provider;
    }

    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();
        const int serviceCount = 1_000;// Number of services to generate
        Dictionary<Type, Type> generatedServices = GenerateServices(serviceCount);

        // Register each dynamically created service in the collection
        foreach ((Type serviceType, Type implementationType) in generatedServices) {
            collection.AddSingleton(serviceType, implementationType);
        }

        IScopedProvider provider = collection.Build();

        return provider;
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
