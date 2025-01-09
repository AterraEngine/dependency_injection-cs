// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceWithGenerics : IServiceWithGenerics<int, string> {
    public int Key { get; set; } = 1;
    public string Value { get; set; } = string.Empty;
}

public interface IServiceWithGenerics<out T0, out T1> {
    T0 Key { get; }
    T1 Value { get; }
}
