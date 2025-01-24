// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Linq.Expressions;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ServiceRecordReflectionFactory {
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetRequiredService), BindingFlags.Instance | BindingFlags.Public)!;

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public static ServiceRecord<TService> CreateWithFactory<TService, TImplementation>(int lifetime) where TImplementation : class, TService {
        Type type = typeof(TImplementation);
        if (type.GetConstructors() is { Length: 0 }) throw new Exception("No constructors");

        #region Special cases
        // Special case for empty constructor
        if (type.GetConstructor([]) is {} emptyConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: _ => (TService)emptyConstructor.Invoke(null),
                lifetime
            );
        }
        
        // special case for only a service provider
        if (type.GetConstructor([typeof(IServiceProvider)]) is {} onlyServiceProviderConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: provider => (TService)onlyServiceProviderConstructor.Invoke([provider]),
                lifetime
            );
        }
        #endregion

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(info => info.GetParameters().Length > 0)
            .ToArray();

        if (constructors.Length > 1) throw new MultipleConstructorsException($"Multiple constructors found for {type.FullName} with parameters");

        ConstructorInfo constructor = constructors.First();
        ParameterInfo[] parameters = constructor.GetParameters();

        // Lambda generation
        ParameterExpression parameterExpression = Expression.Parameter(typeof(IServiceProvider), "provider");

        // Generate constructor arguments, handling IServiceProvider specially
        Expression[] arguments = parameters.Select<ParameterInfo, Expression>(p => {
                // Pass the provider itself for IServiceProvider
                if (p.ParameterType == typeof(IServiceProvider)) return parameterExpression; 
                
                // Call provider.GetRequiredService<T>()
                return Expression.Call(
                    parameterExpression,
                    GetRequiredServiceMethod.MakeGenericMethod(p.ParameterType)
                );
            }
        ).ToArray();

        // Create a constructor call with the generated arguments
        NewExpression constructorCall = Expression.New(constructor, arguments);

        // Build the lambda expression for the factory
        Expression<Func<IServiceProvider, TService>> lambda = Expression.Lambda<Func<IServiceProvider, TService>>(constructorCall, parameterExpression);
        Func<IServiceProvider, TService> compiled = lambda.Compile();  // Compiles into (provider) => new TImplementation(provider.GetRequiredService<TArg>, ...)
        
        // Actually store the record
        return new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            compiled,
            lifetime
        );
    }
}
