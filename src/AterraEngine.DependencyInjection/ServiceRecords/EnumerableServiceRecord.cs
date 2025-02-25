// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
namespace AterraEngine.DependencyInjection.ServiceRecords;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public record EnumerableServiceRecord<TService>(int ScopeDepth) : IServiceRecord {
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Type ServiceType { get; } = typeof(IEnumerable<TService>);
    public Type ImplementationType { get; } = null!;
    
    public bool IsTransient { get; } = ScopeDepth == (int)DefaultScopeDepth.Transient;
    public bool IsSingleton { get; } = ScopeDepth == (int)DefaultScopeDepth.Singleton;
    public bool IsProviderScoped { get; } = ScopeDepth == (int)DefaultScopeDepth.ProviderScoped;
    public bool IsDisposable { get; } = typeof(IDisposable).IsAssignableFrom(typeof(TService));
    public bool IsAsyncDisposable { get; } = typeof(IAsyncDisposable).IsAssignableFrom(typeof(TService));

    private List<Func<IScopedProvider, TService>> ImplementationFactories { get; } = [];
    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public void AddService<TImplementation>() where TImplementation : class, TService {
        ImplementationFactories.Add(static factory => factory.GetRequiredService<TImplementation>());
    }

    private Func<IScopedProvider, IEnumerable<TService>> GetFactory() {
        return provider => ImplementationFactories.Select(factory => factory(provider)).ToArray();
    }
    
    public FrozenServiceRecord ToFrozen() => new(
        ServiceType,
        ImplementationType,
        GetFactory(),
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
        FrozenServiceRecord.GenericServiceState.None
    );
}
