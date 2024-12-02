// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using AterraEngine.DependencyInjection.Generators;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Tests.AterraEngine.DependencyInjection.Generators;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceRegistrationGeneratorTests : IncrementalGeneratorTest<ServiceRegistrationGenerator> {
    protected override Assembly[] ReferenceAssemblies { get; } = [
        typeof(object).Assembly,
        typeof(FactoryCreatedServiceAttribute<,>).Assembly,
        typeof(IFactoryService<>).Assembly,
        typeof(InjectableServiceAttribute<>).Assembly,
        typeof(ValueTuple).Assembly,// For tuples
        typeof(Attribute).Assembly,
        typeof(Console).Assembly,
        typeof(ServiceLifetime).Assembly,
        typeof(PooledObjectPolicy<>).Assembly,
        typeof(DefaultObjectPoolProvider).Assembly,
        Assembly.Load("System.Runtime")
    ];

    [Theory]
    [InlineData(FactoryCreatedServiceInput, FactoryCreatedServiceOutput)]
    [InlineData(InjectableServiceInput, InjectableServiceOutput)]
    public async Task TestText(string inputText, string expectedOutput) {
        GeneratorDriverRunResult runResult = await RunGeneratorAsync(inputText);
        
        GeneratedSourceResult? generatedSource = runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .SingleOrDefault(result => result.HintName.EndsWith("ServiceRegistration.g.cs"));

        Assert.NotNull(generatedSource?.SourceText);
        Assert.Equal(
            expectedOutput.Trim(),
            generatedSource.Value.SourceText.ToString().Trim(),
            ignoreLineEndingDifferences: true,
            ignoreWhiteSpaceDifferences: true
        );
        
    }
    
    [Theory]
    [InlineData(PooledInjectableServiceInput, PooledInjectableServiceOutput, PooledInjectableServiceOutputPooledServices)]
    public async Task TestPooledInjectableServiceOutput(string inputText, string expectedOutput, string expectedOutputPooledServices) {
        GeneratorDriverRunResult runResult = await RunGeneratorAsync(inputText);
        
        GeneratedSourceResult? serviceRegistrationResult = runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .SingleOrDefault(result => result.HintName.EndsWith("ServiceRegistration.g.cs"));
        
        GeneratedSourceResult? pooledServicesResult = runResult.Results
            .SelectMany(result => result.GeneratedSources)
            .SingleOrDefault(result => result.HintName.EndsWith("AutoPooledServices.g.cs"));
        
        Assert.NotNull(serviceRegistrationResult?.SourceText);
        Assert.NotNull(pooledServicesResult?.SourceText);
        Assert.Equal(
            expectedOutput.Trim(),
            serviceRegistrationResult.Value.SourceText.ToString().Trim(),
            ignoreLineEndingDifferences: true,
            ignoreWhiteSpaceDifferences: true
        );
        
        Assert.Equal(
            expectedOutputPooledServices.Trim(),
            pooledServicesResult.Value.SourceText.ToString().Trim(),
            ignoreLineEndingDifferences: true,
            ignoreWhiteSpaceDifferences: true,
            ignoreAllWhiteSpace:true
        );
    }

    #region FactoryCreatedService Test
    [LanguageInjection("csharp")] private const string FactoryCreatedServiceInput = """
        using Microsoft.Extensions.DependencyInjection;
        namespace TestNamespace {
            [AterraEngine.DependencyInjection.FactoryCreatedService<IExampleFactory, ICreatedService>(ServiceLifetime.Transient)]
            public class CreatedService : ICreatedService;
            public interface ICreatedService;
            
            [AterraEngine.DependencyInjection.InjectableService<IExampleFactory>(ServiceLifetime.Singleton)]
            public class ExampleFactory :IExampleFactory {
                public ICreatedService Create() => new CreatedService();
            }
            
            public interface IExampleFactory : AterraEngine.DependencyInjection.IFactoryService<ICreatedService>;
        }
        
        """;

    [LanguageInjection("csharp")] private const string FactoryCreatedServiceOutput = """
        // <auto-generated />
        using Microsoft.Extensions.DependencyInjection;
        namespace TestProject;

        public static class ServiceRegistration {
            public static IServiceCollection RegisterServicesFromTestProject(this IServiceCollection services) {
                services.AddSingleton<TestNamespace.IExampleFactory, TestNamespace.ExampleFactory>();
                services.AddTransient<TestNamespace.ICreatedService>(
                    (provider) => provider.GetRequiredService<TestNamespace.IExampleFactory>().Create()
                );
                return services;
            }
        }
        
        """;
    #endregion

    #region InjectableService Test
    [LanguageInjection("csharp")] private const string InjectableServiceInput = """
        using Microsoft.Extensions.DependencyInjection;
        namespace TestNamespace {
            [AterraEngine.DependencyInjection.InjectableService<IExampleService>(ServiceLifetime.Singleton)]
            public class ExampleService : IExampleService;
            
            
            public interface IExampleService;
        }
        
        """;

    [LanguageInjection("csharp")] private const string InjectableServiceOutput = """
        // <auto-generated />
        using Microsoft.Extensions.DependencyInjection;
        namespace TestProject;

        public static class ServiceRegistration {
            public static IServiceCollection RegisterServicesFromTestProject(this IServiceCollection services) {
                services.AddSingleton<TestNamespace.IExampleService, TestNamespace.ExampleService>();
                return services;
            }
        }

        """;
    #endregion

    #region PooledInjectableService Test
    [LanguageInjection("csharp")] private const string PooledInjectableServiceInput = """
        namespace TestProject;
        
        [AterraEngine.DependencyInjection.PooledInjectableService<IExamplePooled, ExamplePooled>]
        public class ExamplePooled : IExamplePooled {
            public bool Reset() => true;
        }
        
        public interface IExamplePooled : AterraEngine.DependencyInjection.IManualPoolable;
        """;

    [LanguageInjection("csharp")] private const string PooledInjectableServiceOutput = """
        // <auto-generated />
        using Microsoft.Extensions.DependencyInjection;
        namespace TestProject;
        
        public static class ServiceRegistration {
            public static IServiceCollection RegisterServicesFromTestProject(this IServiceCollection services) {
                services.AddSingleton<TestProject.AutoPooledServices>();
                services.AddTransient<TestProject.IExamplePooled>(
                    (provider) => provider.GetRequiredService<TestProject.AutoPooledServices>().ExamplePooledPool.Get()
                );
                return services;
            }
        }
        
        """;

    [LanguageInjection("csharp")] private const string PooledInjectableServiceOutputPooledServices = """
        // <auto-generated />
        using Microsoft.Extensions.ObjectPool;
        namespace TestProject;
        
        public partial class AutoPooledServices {
            private static readonly DefaultObjectPoolProvider _objectPoolProvider = new();
        
            public ObjectPool<TestProject.ExamplePooled> ExamplePooledPool { get; } = _objectPoolProvider
                .Create(new AterraEngine.DependencyInjection.PooledInjectableServiceObjectPolicy<TestProject.ExamplePooled>());
        }
        
        """;
    #endregion
    
}
