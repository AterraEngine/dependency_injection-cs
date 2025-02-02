﻿// ---------------------------------------------------------------------------------------------------------------------
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
// [Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RepeatedServiceCollectionBenchmarks {
    [Benchmark(Baseline = true)]
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

    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Transient() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddTransient<IService, Service>();

        IScopedProvider provider = collection.Build();

        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    [Benchmark]
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

    [Benchmark]
    public object AterraEngine_AddBuildAndRetrieve_SingleDependency_Singleton() {
        var collection = new global::AterraEngine.DependencyInjection.ServiceCollection();

        collection.AddSingleton<IService, Service>();

        IScopedProvider provider = collection.Build();

        var list = new List<IService>(1000);
        for (int i = 0; i < 1000; i++) {
            list.Add(provider.GetRequiredService<IService>());
        }

        return list;
    }

    public interface IService {}

    public class Service : IService {}
}
