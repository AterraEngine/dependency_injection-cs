// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record EnumerableServiceRecord<TService>(int ServiceDepth) : IServiceRecord {
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Type ServiceType { get; } = typeof(IEnumerable<TService>);
    public Type ImplementationType { get; } = null!;
    
    public bool IsTransient { get; } = ServiceDepth == (int)DefaultServiceDepth.Transient;
    public bool IsSingleton { get; } = ServiceDepth == (int)DefaultServiceDepth.Singleton;
    public bool IsProviderScoped { get; } = ServiceDepth == (int)DefaultServiceDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(typeof(TService));
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(typeof(TService));

    private List<Func<ITieredServiceProvider, TService>> ImplementationFactories { get; } = [];
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void AddService<TImplementation>() where TImplementation : class, TService {
        ImplementationFactories.Add(static factory => factory.GetRequiredService<TImplementation>());
    }

    private Func<ITieredServiceProvider, IEnumerable<TService>> GetFactory() {
        return provider => ImplementationFactories.Select(factory => factory(provider)).ToArray();
    }
    
    public FrozenServiceRecord ToFrozen() => new(
        ServiceType,
        ImplementationType,
        GetFactory(),
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
