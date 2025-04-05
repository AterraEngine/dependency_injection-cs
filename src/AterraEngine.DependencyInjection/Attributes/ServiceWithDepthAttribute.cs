// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceWithDepthAttribute(Type serviceType, int ServiceDepth) : ServiceAttribute(serviceType, ServiceDepth);

[AttributeUsage(AttributeTargets.Class)]
public class ServiceWithDepthAttribute<TService>(int ServiceDepth) : ServiceWithDepthAttribute(typeof(TService), ServiceDepth);
