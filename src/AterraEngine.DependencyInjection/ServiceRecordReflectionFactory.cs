// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
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
    public static ServiceRecord<TService> CreateWithFactory<TService, TImplementation>(int scopeDepth)
        where TImplementation : class, TService {
        Type type = typeof(TImplementation);

        if (GetConstructor(type) == null) {
            throw new Exception($"No suitable constructor found for {typeof(TImplementation).FullName}");
        }

        // Return the dynamically created ServiceRecord
        return new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            (Func<IScopedProvider, ValueTask<TService>>)InstanceFactoryAsync<TService, TImplementation>,
            scopeDepth
        );
    }
    
    private static ConstructorInfo? GetConstructor(Type type) {
        return type.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
    }

    private static async ValueTask<TService> InstanceFactoryAsync<TService, TImplementation>(IScopedProvider provider)
        where TImplementation : class, TService {
        Type type = typeof(TImplementation);

        // Find the constructor 
        //      Above we've already established that this can't be null and types don't change.
        ParameterInfo[] parameterInfos = GetConstructor(type)!.GetParameters();
        object[] resolvedDependencies = new object[parameterInfos.Length];

        // Iterate over constructor parameters using a for loop
        //      Yes we are doing this in reverse because of small performance gain
        for (int i = parameterInfos.Length - 1; i >= 0; i--) {
            Type parameterType = parameterInfos[i].ParameterType;

            // Pass the provider directly for IScopedProvider parameters, some performance gain
            if (parameterType == typeof(IScopedProvider)) {
                resolvedDependencies[i] = provider;
                continue;
            }

            // !!! Voodoo magic !!!
            // Don't touch this, or you will lose your mind trying to do it any other way with static typing
            // Dynamic shouldn't be used at all in normal circumstances!
            // This is a very special case and one of the very few and ONLY times this should be allowed!
            resolvedDependencies[i] = await (dynamic)GetRequiredServiceMethod
                .MakeGenericMethod(parameterType)
                .Invoke(provider, null)!;
        }

        // Finally create the instance with the activator
        return (TImplementation)Activator.CreateInstance(type, resolvedDependencies)!;
    }
}
