// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class TransientServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, (int)DefaultScopeDepth.Transient);

[AttributeUsage(AttributeTargets.Class)]
public class TransientServiceAttribute<TService>() : TransientServiceAttribute(typeof(TService));

