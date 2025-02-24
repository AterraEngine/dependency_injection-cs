// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using JetBrains.Annotations;

namespace AterraEngine.DependencyInjection.ServiceRecords;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record InstanceServiceRecord<TService>(
    [UsedImplicitly] TService Instance,
    int ScopeDepth
) : ServiceRecord<TService>(
    typeof(TService),
    typeof(TService),
    _ => Instance,
    ScopeDepth
    );
