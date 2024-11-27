﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class InjectableServiceAttribute<TService>(ServiceLifetime lifetime) : Attribute {
    public ServiceLifetime Lifetime { get; } = lifetime;
    public Type ServiceType { get; } = typeof(TService);
}
