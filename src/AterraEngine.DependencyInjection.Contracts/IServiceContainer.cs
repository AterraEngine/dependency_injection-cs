// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceContainer {
    FrozenDictionary<Type, IServiceRecord> ServiceRecords { get; }

    TService? GetSingletonService<TService>(IServiceRecord recordId, IScopedProvider serviceProvider) where TService : class;
}
