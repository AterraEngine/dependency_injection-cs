// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceRecord {
    Type ServiceType { get; }
    Type ImplementationType { get; }
    int Lifetime { get; }

    bool TryGetFactory<TService>([NotNullWhen(true)] out Func<IServiceProvider, TService>? factory);
}
