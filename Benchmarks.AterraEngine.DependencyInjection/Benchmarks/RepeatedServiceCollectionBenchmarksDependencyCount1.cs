// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using ServiceCollection=Microsoft.Extensions.DependencyInjection.ServiceCollection;
using ServiceProvider=Microsoft.Extensions.DependencyInjection.ServiceProvider;

namespace Benchmarks.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[MemoryDiagnoser]
public class RepeatedServiceCollectionBenchmarksDependencyCount1 {
    private const int Count = 1_000;
    
    [Benchmark(Baseline = true)]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency_Transient() {
        var collection = new ServiceCollection();
    
        collection.AddTransient<IService, Service>();
        collection.AddTransient<INestedService, NestedService>();
    
        ServiceProvider provider = collection.BuildServiceProvider();
    
        var list = new List<IService>(Count);
        for (int i = 0; i < Count; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
    
        return list;
    }
    
    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();
    
        collection.AddTransient<IService, Service>();
        collection.AddTransient<INestedService, NestedService>();
    
        IScopedProvider provider = collection.Build();
    
        var list = new List<IService>(Count);
        for (int i = 0; i < Count; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }
    
        return list;
    }

    [Benchmark]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency_Singleton() {
        var collection = new ServiceCollection();

        collection.AddSingleton<IService, Service>();
        collection.AddSingleton<INestedService, NestedService>();

        ServiceProvider provider = collection.BuildServiceProvider();

        var list = new List<IService>(Count);
        for (int i = Count - 1; i >= 0; i--) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddSingleton<IService, Service>();
        collection.AddSingleton<INestedService, NestedService>();

        IScopedProvider provider = collection.Build();

        var list = new List<IService>(Count);
        for (int i = Count - 1; i >= 0; i--) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    [Benchmark]
    public object Microsoft_AddBuildAndRetrieve_SingleDependency_Scoped() {
        var collection = new ServiceCollection();

        collection.AddScoped<IService, Service>(); 
        collection.AddScoped<INestedService, NestedService>();

        ServiceProvider provider = collection.BuildServiceProvider();

        var list = new List<IService>(Count);
        for (int i = Count - 1; i >= 0; i--) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Scoped() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddScoped<IService, Service>();
        collection.AddScoped<INestedService, NestedService>();

        IScopedProvider provider = collection.Build();

        var list = new List<IService>(Count);
        for (int i = Count - 1; i >= 0; i--) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    public interface IService;

    public class Service(INestedService nestedService) : IService {
        public INestedService NestedService { get; } = nestedService;
    }
    
    public interface INestedService;
    public class NestedService : INestedService {}
}
