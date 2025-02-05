// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Collections.Frozen;
using System.Linq.Expressions;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ServiceRecordReflectionFactory {
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(IScopedProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(IScopedProvider.GetRequiredService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1);

    private static readonly FrozenSet<Type> ResolveAsScopedProvider = new[] { typeof(IServiceProvider), typeof(IScopedProvider) }.ToFrozenSet();

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public static ServiceRecord<TService> CreateWithFactory<TService, TImplementation>(int scopeDepth) where TImplementation : class, TService {
        Type type = typeof(TImplementation);
        if (type.GetConstructors() is { Length: 0 }) throw new Exception("No constructors");

        #region Special Constructor format cases
        // Special case for empty constructor
        if (type.GetConstructor([]) is {} emptyConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: _ => (TService)emptyConstructor.Invoke(null),
                scopeDepth
            );
        }

        // special case for only a service provider
        if (type.GetConstructor([typeof(IScopedProvider)]) is {} onlyServiceProviderConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: provider => (TService)onlyServiceProviderConstructor.Invoke([provider]),
                scopeDepth
            );
        }
        #endregion

        ConstructorInfo? constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(info => info.GetParameters().Length > 0);

        if (constructor is null) throw new MultipleConstructorsException($"Multiple constructors found for {type.FullName} with parameters");

        ParameterInfo[] parameters = constructor.GetParameters();

        // Lambda generation
        ParameterExpression parameterExpression = Expression.Parameter(typeof(IScopedProvider), "provider");

        // Generate constructor arguments, handling IServiceProvider specially
        var arguments = new Expression[parameters.Length];
        for (int i = parameters.Length - 1; i >= 0; i--) {
            Type parameterType = parameters[i].ParameterType;
            if (ResolveAsScopedProvider.Contains(parameterType)) {
                arguments[i] = parameterExpression;
                continue;
            }

            arguments[i] = Expression.Call(
                parameterExpression,
                GetRequiredServiceMethod.MakeGenericMethod(parameterType)
            );
        }

        // Create a constructor call with the generated arguments
        NewExpression constructorCall = Expression.New(constructor, arguments);

        // Build the lambda expression for the factory
        Expression<Func<IScopedProvider, TService>> lambda = Expression.Lambda<Func<IScopedProvider, TService>>(constructorCall, parameterExpression);
        Func<IScopedProvider, TService> compiled = lambda.Compile();// Compiles into (provider) => new TImplementation(provider.GetRequiredService<TArg>, ...)

        // Actually store the record
        return new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            compiled,
            scopeDepth
        );
    }
}
