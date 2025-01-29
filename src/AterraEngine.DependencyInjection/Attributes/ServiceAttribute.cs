// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(Type serviceType, int scopeLevel) : Attribute {
    public Type ServiceType { get; } = serviceType;
    public int ScopeLevel { get; } = scopeLevel;
}

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute<TService>(int scopeLevel) : ServiceAttribute(typeof(TService), scopeLevel);
