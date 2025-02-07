// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace AterraEngine.DependencyInjection.ServiceRecords;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceRecordHelper {
    [RequiresDynamicCode("This method uses reflection to create a service record.")]
    public static ServiceRecord<TService> CreateWithFactory<TService, TImplementation>(int scopeDepth) where TImplementation : class, TService {
        #region Special Constructor format cases
        // Special case for empty constructor
        if (typeof(TImplementation).GetConstructor([]) is {} emptyConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: _ => (TService)emptyConstructor.Invoke(null),
                scopeDepth
            );
        }

        // special case for only a service provider
        if (typeof(TImplementation).GetConstructor([typeof(IScopedProvider)]) is {} onlyServiceProviderConstructor) {
            return new ServiceRecord<TService>(
                typeof(TService),
                typeof(TImplementation),
                ImplementationFactory: provider => (TService)onlyServiceProviderConstructor.Invoke([provider]),
                scopeDepth
            );
        }
        #endregion

        // Actually store the record
        return new ServiceRecord<TService>(
            typeof(TService),
            typeof(TImplementation),
            ConstructorReflectionFactory.CreateFunc<TService>(typeof(TImplementation)),
            scopeDepth
        );
    }
}
