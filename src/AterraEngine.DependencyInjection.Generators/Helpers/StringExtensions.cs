// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection.Generators.Helpers;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class StringExtensions {
    private static readonly ImmutableHashSet<string> ValidLifeTimes = ImmutableHashSet.Create("Singleton", "Scoped", "Transient");
    public static bool TryGetAsServiceLifetimeString(this MemberAccessExpressionSyntax input, [NotNullWhen(true)] out string? output) {
        output = input.Name.Identifier.Text;
        return ValidLifeTimes.Contains(output);
    }
}
