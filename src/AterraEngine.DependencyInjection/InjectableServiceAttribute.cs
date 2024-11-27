// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class InjectableServiceAttribute<TService>(ServiceLifetime lifetime) : Attribute {
    [UsedImplicitly] public ServiceLifetime Lifetime { get; } = lifetime;
    [UsedImplicitly] public Type ServiceType { get; } = typeof(TService);
}
