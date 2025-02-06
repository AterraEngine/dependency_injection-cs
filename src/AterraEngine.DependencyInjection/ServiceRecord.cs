// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection;
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
    public FrozenServiceRecord ToFrozen() {
        var depth = FrozenServiceRecord.KnownScopeDepth.Transient;// same as checking for IsTransient;

        if (IsSingleton) depth = FrozenServiceRecord.KnownScopeDepth.Singleton;
        if (IsProviderScoped) depth = FrozenServiceRecord.KnownScopeDepth.ProviderScoped;
        if (ScopeDepth > (int)DefaultScopeDepth.ProviderScoped) depth = FrozenServiceRecord.KnownScopeDepth.CustomScoped;

        var disposal = FrozenServiceRecord.DisposalType.None;
        if (IsDisposable) disposal = FrozenServiceRecord.DisposalType.Disposable;
        if (IsAsyncDisposable) disposal = FrozenServiceRecord.DisposalType.AsyncDisposable;

        // Create the FrozenServiceRecord using the combined flags
        return new FrozenServiceRecord(
            Id,
            ImplementationFactory,
            ScopeDepth,
            depth,
            disposal
        );
    }
}
