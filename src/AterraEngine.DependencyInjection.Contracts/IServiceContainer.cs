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

    ValueTask<TService?> GetSingletonServiceAsync<TService>(IServiceRecord recordId, IScopedProvider serviceProvider) where TService : class;
}
