// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceContainer(IDictionary<Type, IServiceRecord> records) : IServiceContainer {
    public FrozenDictionary<Type, IServiceRecord> Records { get; internal init; } = records.ToFrozenDictionary();

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public static IServiceContainer FromCollection(ConcurrentDictionary<Type, IServiceRecord> records) => new ServiceContainer(records);
}
