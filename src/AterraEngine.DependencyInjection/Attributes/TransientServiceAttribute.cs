// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class TransientServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, (int)DefaultScopeDepth.Transient);
public class CustomScopeDepthServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, 5);
public class AnotherCustomScopeDepthServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, 6);²

// public class TransientServiceAttribute<TService>() : TransientServiceAttribute(typeof(TService));

