// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public interface IServiceRecord {
    Guid Id { get; set; }
    Type ServiceType { get; }
    Type ImplementationType { get; }
    int ScopeDepth { get; }

    bool IsSingleton { get; }
    bool IsTransient { get; }
    bool IsProviderScoped { get; }
    bool IsDisposable { get; }
    bool IsAsyncDisposable { get; }

    FrozenServiceRecord ToFrozen();
}
