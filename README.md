# ⛓️‍💥 AterraEngine.DependencyInjection ⛓️‍💥

AterraEngine.DependencyInjection is a library that provides tools for automizing dependency injection in .NET projects.
It includes source generators for automatic registration of services and attributes to facilitate DI.

## Features

- **Automatic Service Registration:** Use source generators to automatically register services in your project.
- **Custom Attributes:** Define custom attributes to specify the lifetime and other details for your services.

## Getting Started

### Prerequisites

- .NET 9.0 or later

### Installation
You can install `AterraEngine.DependencyInjection` via NuGet Package Manager:
```bash
dotnet add package AterraEngine.DependencyInjection
```

You can install `AterraEngine.DependencyInjection.Generator` via NuGet Package Manager:
```bash
dotnet add package AterraEngine.DependencyInjection.Generator
```

### Usage

#### Define Services with Attributes

You can use the provided attributes to define services:

```csharp
using Microsoft.Extensions.DependencyInjection;
namespace YourNamespace;

[InjectableService<IExampleService>(ServiceLifetime.Singleton)]
public class ExampleService : IExampleService {
    // ...
}

public interface IExampleService {
    // ...
}
```

#### Generate Service Registrations

The source generator will automatically create the necessary registration code. C
all the generated registration method in your `Startup` or `Program` class:

```csharp
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.RegisterServicesFromYourAssemblyName();
```

## Contributing

Contributions are welcome! Please fork this repository and submit a