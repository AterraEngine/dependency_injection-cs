﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using Microsoft.Extensions.ObjectPool;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class PooledInjectableServiceObjectPolicy<T> : PooledObjectPolicy<T> where T : IManualPoolable, new() {
    public override T Create() => new();
    public override bool Return(T obj) => obj.Reset();
}
