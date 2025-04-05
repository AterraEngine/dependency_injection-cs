// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(Type serviceType, int ServiceDepth) : Attribute {
    public Type ServiceType { get; } = serviceType;
    public int ServiceDepth { get; } = ServiceDepth;
}

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute<TService>(int ServiceDepth) : ServiceAttribute(typeof(TService), ServiceDepth);
