// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceRecord {
    Guid Id { get; }
    Type ServiceType { get; }
    Type ImplementationType { get; }
    int Lifetime { get; }

    bool IsSingleton { get; }
    bool IsTransient { get; }
    bool IsDisposable { get; }
    bool IsAsyncDisposable { get; }

    bool TryGetFactory<TService>([NotNullWhen(true)] out Func<IServiceProvider, TService>? factory);
}
