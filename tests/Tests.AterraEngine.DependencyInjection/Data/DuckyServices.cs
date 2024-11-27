﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AterraEngine.DependencyInjection.Data;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IDucky {
    string QuackingNoise { get; }
}

public interface IDuckyFactory : IFactoryService<IDucky>;
public interface IDuckyService {
    string Quack(IDucky ducky);
}

[InjectableService<IDuckyService>(ServiceLifetime.Singleton)]
public class DuckyService : IDuckyService {
    public string Quack(IDucky ducky) => ducky.QuackingNoise;
}

[InjectableService<IDuckyFactory>(ServiceLifetime.Singleton)]
public class DuckyFactory : IDuckyFactory {
    public IDucky Create() => new Ducky();
}

[FactoryCreatedService<IDuckyFactory, IDucky>(ServiceLifetime.Transient)]
public class Ducky : IDucky {
    public string QuackingNoise { get; } = "Quack Quack";
}

