// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

namespace Benchmarks.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Program {
    public static void Main(string[] args) {
        string? projectDir = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
        if (projectDir is null) return;

        var config = DefaultConfig.Instance
            .WithArtifactsPath(projectDir) // Set the output to the directory of the .csproj
            .AddExporter(MarkdownExporter.Default);

        
        // Run all benchmarks in parallel
        var switcher = new BenchmarkSwitcher([
            typeof(RepeatedServiceCollectionBenchmarks),
            // typeof(LargeServiceCollectionBenchmarks),
            // typeof(ServiceCollectionBenchmarks)
        ]);

        switcher.RunAllJoined(config);
    }
}
