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
    int ServiceDepth
) : ServiceRecord<TService>(
    typeof(TService),
    typeof(TService),
    _ => Instance,
    ServiceDepth
    );
