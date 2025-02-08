// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceWithGenerics<T0, T1> : IServiceWithGenerics<T0, T1> {
    #pragma warning disable CS8601 // Possible null reference assignment.
    public T0 Key { get; } = default;
    public T1 Value { get; } = default;
    #pragma warning restore CS8601 // Possible null reference assignment.
}

public interface IServiceWithGenerics<out T0, out T1> {
    T0 Key { get; }
    T1 Value { get; }
}
