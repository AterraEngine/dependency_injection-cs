// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Benchmarks.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RepeatedServiceCollectionBenchmarks {
    [Benchmark(Baseline = true, OperationsPerInvoke = 1000)]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency_Transient() {
        var collection = new ServiceCollection();

        collection.AddTransient<IService, Service>();

        ServiceProvider provider = collection.BuildServiceProvider();
        
        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
        return list;
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddTransient<IService, Service>();

        IServiceProvider provider = collection.Build();
        
        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
        return list;
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency_Singleton() {
        var collection = new ServiceCollection();

        collection.AddSingleton<IService, Service>();

        ServiceProvider provider = collection.BuildServiceProvider();
        
        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
        return list;
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddSingleton<IService, Service>();

        IServiceProvider provider = collection.Build();
        
        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
        return list;
    }
    
    public interface IService {}
    public class Service : IService {}
}

