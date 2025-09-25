using Microsoft.Extensions.DependencyInjection;
using System;

// Test circular dependency detection
var services = new ServiceCollection();

// Create a circular dependency: A depends on B, B depends on A
services.AddSingleton<CircularDependencyA>();
services.AddSingleton<CircularDependencyB>();

var serviceProvider = services.BuildServiceProvider();

try
{
    var a = serviceProvider.GetRequiredService<CircularDependencyA>();
    Console.WriteLine("CircularDependencyA resolved successfully: " + (a != null));
}
catch (Exception ex)
{
    Console.WriteLine("Exception when resolving CircularDependencyA: " + ex.GetType().Name + " - " + ex.Message);
}

try
{
    var b = serviceProvider.GetRequiredService<CircularDependencyB>();
    Console.WriteLine("CircularDependencyB resolved successfully: " + (b != null));
}
catch (Exception ex)
{
    Console.WriteLine("Exception when resolving CircularDependencyB: " + ex.GetType().Name + " - " + ex.Message);
}

public class CircularDependencyA
{
    public CircularDependencyA(CircularDependencyB b) { }
}

public class CircularDependencyB
{
    public CircularDependencyB(CircularDependencyA a) { }
}