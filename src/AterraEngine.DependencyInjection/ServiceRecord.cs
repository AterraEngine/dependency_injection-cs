// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public readonly record struct ServiceRecord<TService, TImplementation>(
    Type ServiceType,
    Type ImplementationType,
    Func<IServiceProvider, TService>? ImplementationFactory,
    int Lifetime
) : IServiceRecord
    where TImplementation : class, TService {

    public bool IsSingleton { get; } = Lifetime == 0;
    public bool IsTransient { get; } = Lifetime == -1;

    // -----------------------------------------------------------------------------------------------------------------
    // Constructors
    // -----------------------------------------------------------------------------------------------------------------
    public ServiceRecord(Type ServiceType, Type ImplementationType, TImplementation instance, int Lifetime) : this(
        ServiceType,
        ImplementationType,
        _ => instance,
        Lifetime
    ) {}

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------

    public bool TryGetFactory<T>([NotNullWhen(true)]  out Func<IServiceProvider, T>? factory) {
        factory = null;
        if (typeof(T) != typeof(TService)) return false;
        if (ImplementationFactory is not Func<IServiceProvider, T> casted) return false;
        return (factory = casted) is not null;
    }
}
