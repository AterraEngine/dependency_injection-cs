// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Linq.Expressions;
using System.Reflection;

namespace AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ConstructorReflectionFactory {
    private static readonly Type ResolveAsTieredServiceProvider = typeof(ITieredServiceProvider);
    
    private static readonly MethodInfo GetRequiredServiceMethod = ResolveAsTieredServiceProvider
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(ITieredServiceProvider.GetRequiredService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1);

    private static readonly MethodInfo GetServiceMethod = ResolveAsTieredServiceProvider
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(ITieredServiceProvider.GetService), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1);

    private static readonly ParameterExpression ProviderExpression = Expression.Parameter(ResolveAsTieredServiceProvider, "provider");

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    public static Func<ITieredServiceProvider, TService> CreateFunc<TService>(Type implementationType) {
        // Select the most parameterized constructor (constructor with the most parameters)
        ConstructorInfo? constructor = implementationType
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor is null) throw new MultipleConstructorsException($"Multiple constructors found for {implementationType.FullName} with parameters");

        ParameterInfo[] parameters = constructor.GetParameters();

        // Generate constructor arguments, handling IServiceProvider specially
        var arguments = new Expression[parameters.Length];
        for (int i = parameters.Length - 1; i >= 0; i--) {
            ParameterInfo parameter = parameters[i];
            Type parameterType = parameter.ParameterType;
            if (ResolveAsTieredServiceProvider == parameterType) {
                arguments[i] = ProviderExpression;
                continue;
            }

            // Check if the parameter type is specifically T?
            //      This means we can allow for services to not always having to be implemented
            if (parameter.IsNullableReferenceType() || parameter is { HasDefaultValue: true, DefaultValue: null }) {
                arguments[i] = Expression.Call(
                    ProviderExpression,
                    GetServiceMethod.MakeGenericMethod(parameterType)
                );

                continue;
            }

            arguments[i] = Expression.Call(
                ProviderExpression,
                GetRequiredServiceMethod.MakeGenericMethod(parameterType)
            );
        }

        // Create a constructor call with the generated arguments
        NewExpression constructorCall = Expression.New(constructor, arguments);

        // Build the lambda expression for the factory
        Expression<Func<ITieredServiceProvider, TService>> lambda = Expression.Lambda<Func<ITieredServiceProvider, TService>>(constructorCall, ProviderExpression);
        Func<ITieredServiceProvider, TService> compiled = lambda.Compile();// Compiles into (provider) => new TImplementation(provider.GetRequiredService<TArg>(), ...)

        return compiled;
    }
}
