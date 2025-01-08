// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Linq.Expressions;
using System.Reflection;

namespace AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ServiceRecordFactory {
    public static ServiceRecord<TService, TImplementation> CreateWithFactory<TService, TImplementation>(int lifetime) where TImplementation : class, TService {
        Type type = typeof(TImplementation);
        if (type.GetConstructors() is { Length: 0 }) throw new Exception("No constructors");
        
        // Special case for empty constructor
        if (type.GetConstructor([]) is {} emptyConstructor) return new ServiceRecord<TService, TImplementation>(
            typeof(TService),
            typeof(TImplementation),
            _ => (TService)emptyConstructor.Invoke(null),
            lifetime
        );
        
        // in the statement above we already check for a constructor with zero args
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(info => info.GetParameters().Length > 0)
            .ToArray();
        if (constructors.Length > 1 ) throw new Exception("Too many constructors");
        ConstructorInfo constructor = constructors[0];
        
        ParameterInfo[] parameters = constructor.GetParameters();

        // Small optimization
        // if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IServiceProvider)) {
        //     return new ServiceRecord<TService, TImplementation>(
        //         typeof(TService),
        //         typeof(TImplementation),
        //         provider =>  (TService)constructor.Invoke([provider]),
        //         lifetime
        //     );
        // }
        
        // Lambda generation
        ParameterExpression parameterExpression = Expression.Parameter(typeof(IServiceProvider), "provider");
        MethodInfo methodInfo = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetRequiredService), BindingFlags.Instance | BindingFlags.Public)!;
        
        Expression[] arguments = parameters.Select(p => Expression.Call(
            parameterExpression,
            methodInfo.MakeGenericMethod(p.ParameterType)
        )).ToArray<Expression>();
        
        NewExpression constructorCall = Expression.New(constructor, arguments);
        Expression<Func<IServiceProvider, TService>> lambda = Expression.Lambda<Func<IServiceProvider, TService>>(constructorCall, parameterExpression);

        // Actually store the record
        return new ServiceRecord<TService, TImplementation>(
            typeof(TService),
            typeof(TImplementation),
            lambda.Compile(), // Compiles into (provider) => new TImplementation(provider.GetRequiredService<TArg>, ...)
            lifetime
        );
    }
}
