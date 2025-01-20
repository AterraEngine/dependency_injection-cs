// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using BenchmarkDotNet.Running;

namespace Benchmarks.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Program {
    public static void Main(string[] args) {
        BenchmarkRunner.Run<RepeatedServiceCollectionBenchmarks>();
        // BenchmarkRunner.Run<ServiceCollectionBenchmarks>();
        // BenchmarkRunner.Run<LargeServiceCollectionBenchmarks>();
    }
}
