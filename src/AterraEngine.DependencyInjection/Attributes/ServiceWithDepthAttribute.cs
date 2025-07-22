// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceWithDepthAttribute(Type serviceType, int serviceDepth) : ServiceAttribute(serviceType, serviceDepth);

[AttributeUsage(AttributeTargets.Class)]
public class ServiceWithDepthAttribute<TService>(int serviceDepth) : ServiceWithDepthAttribute(typeof(TService), serviceDepth);
