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
    Func<ITieredServiceProvider, TService> ImplementationFactory,
    int ServiceDepth
) : IServiceRecord {
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public bool IsTransient { get; } = ServiceDepth == (int)DefaultServiceDepth.Transient;
    public bool IsSingleton { get; } = ServiceDepth == (int)DefaultServiceDepth.Singleton;
    public bool IsProviderScoped { get; } = ServiceDepth == (int)DefaultServiceDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(ImplementationType);
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(ImplementationType);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public FrozenServiceRecord ToFrozen() => new(
        ServiceType,
        ImplementationType,
        ImplementationFactory,
        ServiceDepth,
        this switch {
            { IsSingleton: true } => FrozenServiceRecord.KnownServiceDepth.Singleton,
            { IsProviderScoped: true } => FrozenServiceRecord.KnownServiceDepth.ProviderScoped,
            { ServiceDepth: > (int)DefaultServiceDepth.ProviderScoped } => FrozenServiceRecord.KnownServiceDepth.CustomTier,
            _ => FrozenServiceRecord.KnownServiceDepth.Transient
        },
        this switch {
            { IsDisposable: true } => FrozenServiceRecord.DisposalType.Disposable,
            { IsAsyncDisposable: true } => FrozenServiceRecord.DisposalType.AsyncDisposable,
            _ => FrozenServiceRecord.DisposalType.None
        },
        FrozenServiceRecord.GenericServiceState.None
    );
}
