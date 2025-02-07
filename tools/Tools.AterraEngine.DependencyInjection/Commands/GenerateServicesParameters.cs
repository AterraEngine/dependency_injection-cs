// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using CodeOfChaos.CliArgsParser;

namespace Tools.AterraEngine.DependencyInjection.Commands;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public readonly partial struct GenerateServicesParameters : IParameters {
    [CliArgsParameter("root", "r")] [CliArgsDescription("The root directory of the project to update")]
    public string Root { get; init; } = "../../../../../";

    [CliArgsParameter("count", "c")] [CliArgsDescription("The number of services to generate")]
    public double Count { get; init; } = Math.Pow(2, 13);
}
