// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace Tests.AterraEngine.DependencyInjection.Services;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class DisposableService : IDisposableService {
    public string? ConnectionString { get; set; } = string.Empty;
    
    public void Dispose() {
        ConnectionString = null;
        GC.SuppressFinalize(this);
    }
}

public interface IDisposableService : IDisposable {
    public string? ConnectionString { get; }
}
