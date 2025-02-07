// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record GenericServiceRecord(
    Type ServiceType,
    Type ImplementationType,
    int ScopeDepth
) : IServiceRecord {
    public bool IsGenericService { get; } = true;

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public bool IsTransient { get; } = ScopeDepth == (int)DefaultScopeDepth.Transient;
    public bool IsSingleton { get; } = ScopeDepth == (int)DefaultScopeDepth.Singleton;
    public bool IsProviderScoped { get; } = ScopeDepth == (int)DefaultScopeDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public FrozenServiceRecord ToFrozen() => new(
        Id,
        ServiceType,
        ImplementationType,
        (Func<IScopedProvider, object>)(static _ => throw new InvalidOperationException("Open generic types can't be instantiated directly. Use a closed type.")),
        ScopeDepth,
        this switch {
            { IsSingleton: true } => FrozenServiceRecord.KnownScopeDepth.Singleton,
            { IsProviderScoped: true } => FrozenServiceRecord.KnownScopeDepth.ProviderScoped,
            { ScopeDepth: > (int)DefaultScopeDepth.ProviderScoped } => FrozenServiceRecord.KnownScopeDepth.CustomScoped,
            _ => FrozenServiceRecord.KnownScopeDepth.Transient
        },
        this switch {
            { IsDisposable: true } => FrozenServiceRecord.DisposalType.Disposable,
            { IsAsyncDisposable: true } => FrozenServiceRecord.DisposalType.AsyncDisposable,
            _ => FrozenServiceRecord.DisposalType.None
        },
        IsGenericService ? FrozenServiceRecord.GenericServiceState.OpenGeneric : FrozenServiceRecord.GenericServiceState.None
    );
}
