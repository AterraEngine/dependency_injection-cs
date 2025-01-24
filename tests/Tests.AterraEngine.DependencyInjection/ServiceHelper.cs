﻿// ---------------------------------------------------------------------------------------------------------------------
// Imports
// ---------------------------------------------------------------------------------------------------------------------
using System.Reflection;
using System.Reflection.Emit;

namespace Tests.AterraEngine.DependencyInjection;
// ---------------------------------------------------------------------------------------------------------------------
// Code
// ---------------------------------------------------------------------------------------------------------------------
public static class ServiceHelper {
    public static Dictionary<Type, Type> GenerateServices(int count) {
        var interfaceImplementationPairs = new Dictionary<Type, Type>();

        // Create an in-memory assembly for dynamic types
        var assemblyName = new AssemblyName("DynamicServices");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        for (int i = 0; i < count; i++) {
            // Define a new interface
            string interfaceName = $"IService{i}";
            TypeBuilder interfaceBuilder = moduleBuilder.DefineType(interfaceName,
                TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

            Type interfaceType = interfaceBuilder.CreateType();

            // Define a class implementing the interface
            string className = $"Service{i}";
            TypeBuilder classBuilder = moduleBuilder.DefineType(className,
                TypeAttributes.Public | TypeAttributes.Class);

            classBuilder.AddInterfaceImplementation(interfaceType);

            // Create a parameterless constructor for the class
            ConstructorBuilder _ = classBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            Type implementationType = classBuilder.CreateType();

            // Add to the list
            interfaceImplementationPairs.Add(interfaceType, implementationType);
        }

        return interfaceImplementationPairs;
    }
}
