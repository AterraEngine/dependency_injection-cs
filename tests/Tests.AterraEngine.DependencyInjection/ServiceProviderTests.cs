﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using AterraEngine.DependencyInjection;
using Tests.AterraEngine.DependencyInjection.Helpers;
using Tests.AterraEngine.DependencyInjection.Services;
using IServiceProvider=AterraEngine.DependencyInjection.IServiceProvider;

namespace Tests.AterraEngine.DependencyInjection;

// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public class ServiceProviderTests {
    [Test]
    public async Task ServiceProvider_Should_Return_Service_Provider_Empty() {
        // Arrange
        var collection = new ServiceCollection();

        // Act
        IServiceProvider provider = collection.Build();

        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(0);
    }

    [Test]
    [Arguments(0b1)]
    [Arguments(0b10)]
    [Arguments(0b100)]
    [Arguments(0b1000)]
    [Arguments(0b1000_0)]
    [Arguments(0b1000_00)]
    [Arguments(0b1000_000)]
    [Arguments(0b1000_0000)]
    [Arguments(0b1000_0000_0)]
    [Arguments(0b1000_0000_00)]
    [Arguments(0b1000_0000_000)]
    [Arguments(0b1000_0000_0000)]
    // [Arguments(0b1000_0000_0000_0)] // Takes a bit too long to auto generate these services. Maybe look into a small library that can 
    // [Arguments(0b1000_0000_0000_00)]
    // [Arguments(0b1000_0000_0000_000)]
    // [Arguments(0b1000_00000_000_0000)]
    public async Task ServiceProvider_Should_Return_Service_Provider_Count(int count) {
        // Arrange
        var collection = new ServiceCollection();
        ServiceHelper.AddAutoGeneratedServicesServices(collection, count);
        
        // Act
        IServiceProvider provider = collection.Build();
        
        // Assert
        await Assert.That(provider)
            .IsNotNull()
            .And.HasCount().EqualTo(count);
    }

    [Test]
    [Arguments(typeof(IdService))] 
    [Arguments(typeof(IIdService))]
    public async Task ServiceProvider_GetService_ReturnsNullOnInvalid(Type serviceType) {
        // Arrange
        var collection = new ServiceCollection();
        ServiceHelper.AddAutoGeneratedServicesServices(collection, 10); // THe actual count here doesn't really matter except for performance reasons.
        IServiceProvider provider = collection.Build();
        
        // Act
        object? service = provider.GetService(serviceType);

        // Assert
        await Assert.That(service)
            .IsNull();
    }
    
    [Test]
    [Arguments(typeof(IIdService), typeof(IdService))] 
    [Arguments(typeof(IEmptyService), typeof(EmptyService))] 
    [Arguments(typeof(IServiceProviderRequiredService), typeof(ServiceProviderRequiredService))] 
    // [Arguments(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics))] // TODO can only be tested when these types of generics are implemented
    public async Task ServiceProvider_GetService_ReturnsValidImplementation(Type serviceType, Type implementationType) {
        // Arrange
        var collection = new ServiceCollection();
        ServiceHelper.AddAutoGeneratedServicesServices(collection, 10); // THe actual count here doesn't really matter except for performance reasons.
        collection.AddSingleton(serviceType, implementationType);
        IServiceProvider provider = collection.Build();
        
        // Act
        object? service = provider.GetService(serviceType);
        
        // Assert
        await Assert.That(service)
            .IsNotNull()
            .And.IsAssignableTo(serviceType)
            .And.IsTypeOf(implementationType);
    }
    
    [Test]
    [Arguments(typeof(IdService))] 
    [Arguments(typeof(IIdService))]
    public async Task ServiceProvider_GetRequiredService_ThrowsOnNotRegisteredService(Type serviceType) {
        // Arrange
        var collection = new ServiceCollection();
        ServiceHelper.AddAutoGeneratedServicesServices(collection, 10); // THe actual count here doesn't really matter except for performance reasons.
        IServiceProvider provider = collection.Build();
        
        // Act && Assert
        await Assert.ThrowsAsync<CouldNotBeResolvedException>(() => Task.FromResult(provider.GetRequiredService(serviceType)));
    }
    
    [Test]
    [Arguments(typeof(IIdService), typeof(IdService))] 
    [Arguments(typeof(IEmptyService), typeof(EmptyService))] 
    [Arguments(typeof(IServiceProviderRequiredService), typeof(ServiceProviderRequiredService))] 
    // [Arguments(typeof(IServiceWithGenerics<,>), typeof(ServiceWithGenerics))] // TODO can only be tested when these types of generics are implemented
    public async Task ServiceProvider_GetRequiredService_ReturnsValidImplementation(Type serviceType, Type implementationType) {
        // Arrange
        var collection = new ServiceCollection();
        ServiceHelper.AddAutoGeneratedServicesServices(collection, 10); // THe actual count here doesn't really matter except for performance reasons.
        collection.AddSingleton(serviceType, implementationType);
        IServiceProvider provider = collection.Build();
        
        // Act
        object service = provider.GetRequiredService(serviceType);
        
        // Assert
        await Assert.That(service)
            .IsNotNull()
            .And.IsAssignableTo(serviceType)
            .And.IsTypeOf(implementationType);
            
    }
}
