// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
[AttributeUsage(AttributeTargets.Class)]
public class TransientServiceAttribute(Type serviceType) : ServiceAttribute(serviceType, -1);

public class TransientServiceAttribute<TService>() : TransientServiceAttribute(typeof(TService));
