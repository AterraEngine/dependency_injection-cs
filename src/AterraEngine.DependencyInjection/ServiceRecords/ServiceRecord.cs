// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record ServiceRecord<TService>(
    Type ServiceType,
    Type ImplementationType,
    Func<IScopedProvider, TService> ImplementationFactory,
    int ScopeDepth
) : IServiceRecord {
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public bool IsTransient { get; } = ScopeDepth == (int)DefaultScopeDepth.Transient;
    public bool IsSingleton { get; } = ScopeDepth == (int)DefaultScopeDepth.Singleton;
    public bool IsProviderScoped { get; } = ScopeDepth == (int)DefaultScopeDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public FrozenServiceRecord ToFrozen() => new() {
        ServiceType = ServiceType,
        ImplementationType = ImplementationType,
        ImplementationFactory = ImplementationFactory,
        ScopeDepth = ScopeDepth,
        Depth = this switch {
            { IsSingleton: true } => FrozenServiceRecord.KnownScopeDepth.Singleton,
            { IsProviderScoped: true } => FrozenServiceRecord.KnownScopeDepth.ProviderScoped,
            { ScopeDepth: > (int)DefaultScopeDepth.ProviderScoped } => FrozenServiceRecord.KnownScopeDepth.CustomScoped,
            _ => FrozenServiceRecord.KnownScopeDepth.Transient
        },
        Disposal = this switch {
            { IsDisposable: true } => FrozenServiceRecord.DisposalType.Disposable,
            { IsAsyncDisposable: true } => FrozenServiceRecord.DisposalType.AsyncDisposable,
            _ => FrozenServiceRecord.DisposalType.None
        },
        GenericService = FrozenServiceRecord.GenericServiceState.None
    };
}
