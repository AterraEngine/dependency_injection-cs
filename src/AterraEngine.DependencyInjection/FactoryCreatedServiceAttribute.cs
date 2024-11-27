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
public class FactoryCreatedServiceAttribute<TFactory, TService>(ServiceLifetime lifetime) : Attribute {
    [UsedImplicitly] public ServiceLifetime Lifetime { get; } = lifetime;
    [UsedImplicitly] public Type ServiceType { get; } = typeof(TService);
    [UsedImplicitly] public Type FactoryType { get; } = typeof(TFactory);
}