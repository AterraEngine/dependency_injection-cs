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
        // Run all benchmarks in parallel
        var switcher = new BenchmarkSwitcher([
            typeof(RepeatedServiceCollectionBenchmarks)
            // typeof(LargeServiceCollectionBenchmarks),
            // typeof(ServiceCollectionBenchmarks)
        ]);

        switcher.RunAllJoined();
    }
}
