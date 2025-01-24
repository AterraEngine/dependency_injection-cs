// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Example.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class Program {
    public static Task Main(string[] args) {
        var collection = new ServiceCollection();
        
        collection.AddSingleton<IService, Service>(); 
        collection.AddSingleton<IServiceRez, ServiceRez>(); 
        collection.AddTransient<ITransient, Transient>();
        
        using IServiceProvider disposable = collection.Build();

        var service = disposable.GetService<IService>();
        Console.WriteLine(service?.Name);
        
        var service1 = disposable.GetService<IService>();
        Console.WriteLine(service1?.Name);
        Console.WriteLine(service == service1);
        
        var serviceRez = disposable.GetRequiredService<IServiceRez>();
        Console.WriteLine(serviceRez.Service.Name);
        Console.WriteLine(serviceRez.Service1.Name);
        Console.WriteLine(serviceRez.Transient.Name);
        Console.WriteLine(serviceRez.Service == service);
        Console.WriteLine(serviceRez.Service1 == service1);
        
        var transient = disposable.GetRequiredService<ITransient>();
        Console.WriteLine(transient.Name);
        
        return Task.CompletedTask;

        // var serviceRez2 = provider.GetRequiredService<ServiceReze2>();
    }
    
    public enum ServiceLifetime {
        Transient = -1,
        Singleton = 0,
        EngineScope = 1,
        GameScope = 2,
        WorldScope = 3,
        LevelScope = 4,
    }


    public interface IService {
        string Name { get; }
    }
    public class Service : IService {
        public string Name { get; } = Guid.NewGuid().ToString();
    }
    
    public interface ITransient : IService;
    public class Transient : ITransient {
        public string Name { get; } = Guid.NewGuid().ToString();
    }

    public interface IServiceRez {
        IService Service { get; }
        IService Service1 { get; }
        ITransient Transient { get; }
    }
    public class ServiceRez(IService service, IService service1, ITransient transient) : IServiceRez {
        public IService Service { get; } = service;
        public IService Service1 { get; } = service1;
        public ITransient Transient { get; } = transient;
    }
}
