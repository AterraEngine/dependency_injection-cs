// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, 0);

[AttributeUsage(AttributeTargets.Class)]
public class SingletonServiceAttribute<TService>() : SingletonServiceAttribute(typeof(TService));
