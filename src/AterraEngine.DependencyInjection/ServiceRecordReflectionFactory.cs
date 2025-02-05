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
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(IScopedProvider)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(m => m is { Name: nameof(IScopedProvider.GetRequiredServiceAsync), IsGenericMethodDefinition: true } && m.GetGenericArguments().Length == 1);

    // -----------------------------------------------------------------------------------------------------------------
    // Methods
    // -----------------------------------------------------------------------------------------------------------------
    // ---------------------------------------------------------------------------------------------------------------------
    public static ServiceRecord<TService> CreateWithFactory<TService, TImplementation>(int scopeDepth)
        where TImplementation : class, TService {
        Type type = typeof(TImplementation);

        // Find the constructor with the most parameters
        ConstructorInfo? constructor = type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor is null) throw new Exception($"No suitable constructor found for {type.Name}");

        ParameterExpression providerParameter = Expression.Parameter(typeof(IScopedProvider), "provider");

        // We are leveraging Task.WhenAll() when we resolve the services
        // This is a bit of a hack, but it works for now
        List<ParameterExpression> taskVariables = [];
        List<Expression> taskAssignments = [];
        List<Expression> constructorArguments = [];

        foreach (ParameterInfo parameter in constructor.GetParameters()) {
            Type parameterType = parameter.ParameterType;

            // For IScopedProvider, pass the provider directly to the constructor
            if (parameterType == typeof(IScopedProvider)) {
                constructorArguments.Add(providerParameter);
                continue;
            }

            // Create the ValueTask<> parameter which will hold the output of the required service
            ParameterExpression taskVariable = Expression.Variable(typeof(ValueTask<>).MakeGenericType(parameterType), parameter.Name + "Task");
            taskVariables.Add(taskVariable);

            taskAssignments.Add(Expression.Assign(taskVariable, Expression.Call(
                providerParameter,
                GetRequiredServiceMethod.MakeGenericMethod(parameterType)
            )));
            
            MemberExpression resultAccess = Expression.Property(
                Expression.Call(taskVariable, "AsTask", null),
                "Result"
            );

            constructorArguments.Add(resultAccess);
        }

        // Array of tasks for Task.WhenAll
        NewArrayExpression taskArray = Expression.NewArrayInit(
            typeof(Task),
            taskVariables.Select(tv => Expression.Call(tv, "AsTask", null))
        );

        // Use reflection to select the appropriate Task.WhenAll overload
        MethodInfo? whenAllMethod = typeof(Task).GetMethod(nameof(Task.WhenAll), [typeof(Task[])]);
        if (whenAllMethod is null) {
            throw new Exception("Could not find Task.WhenAll method.");
        }

        // Await Task.WhenAll to complete all tasks
        MethodCallExpression whenAllCall = Expression.Call(
            whenAllMethod,// Specify the correct overload explicitly
            taskArray
        );

        // Create an expression block
        BlockExpression body = Expression.Block(
            taskVariables,// Declare task variables
            taskAssignments.Append(whenAllCall).Append(// Assign tasks and await Task.WhenAll
                // Invoke the constructor with resolved arguments
                Expression.New(constructor, constructorArguments)
            )
        );

        // Wrap the constructor result in a ValueTask<TService>
        NewExpression valueTaskResult = Expression.New(
            typeof(ValueTask<TService>).GetConstructor([typeof(TService)])!,
            body
        );

        // Create a lambda function
        Expression<Func<IScopedProvider, ValueTask<TService>>> lambda = Expression.Lambda<Func<IScopedProvider, ValueTask<TService>>>(
            valueTaskResult,
            providerParameter
        );
 
        // Compile the factory method
        Func<IScopedProvider, ValueTask<TService>> factory = lambda.Compile();

        // Return the ServiceRecord
        return new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            factory,
            scopeDepth
        );
    }
}
