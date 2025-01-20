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
public class ServiceCollectionBenchmarks {
    [Benchmark(Baseline = true, OperationsPerInvoke = 1000)]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency() {
        var collection = new ServiceCollection();

        collection.AddSingleton<IService, Service>();

        ServiceProvider provider = collection.BuildServiceProvider();
        
        return provider.GetRequiredService<IService>();
    }
    
    [Benchmark(OperationsPerInvoke = 1000)]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddSingleton<IService, Service>();

        IServiceProvider provider = collection.Build();
        
        return provider.GetRequiredService<IService>();
    }
    
    public interface IService {}
    public class Service : IService {}
}

