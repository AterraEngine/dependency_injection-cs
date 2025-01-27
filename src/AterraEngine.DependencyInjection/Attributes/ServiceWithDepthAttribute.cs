// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class ServiceWithDepthAttribute(Type serviceType, int scopeDepth) : ServiceAttribute(serviceType, scopeDepth);

public class ServiceWithDepthAttribute<TService>(int scopeDepth) : ServiceWithDepthAttribute(typeof(TService), scopeDepth);
