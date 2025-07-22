// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(Type serviceType, int serviceDepth) : Attribute {
    public Type ServiceType { get; } = serviceType;
    public int ServiceDepth { get; } = serviceDepth;
}

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute<TService>(int serviceDepth) : ServiceAttribute(typeof(TService), serviceDepth);
