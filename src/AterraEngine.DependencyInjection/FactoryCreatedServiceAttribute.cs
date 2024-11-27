﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FactoryCreatedServiceAttribute<TFactory, TService>(ServiceLifetime lifetime) : Attribute
    where TFactory : IFactoryService<TService> 
{
    [UsedImplicitly] public ServiceLifetime Lifetime { get; } = lifetime;
    [UsedImplicitly] public Type ServiceType { get; } = typeof(TService);
    [UsedImplicitly] public Type FactoryType { get; } = typeof(TFactory);
}