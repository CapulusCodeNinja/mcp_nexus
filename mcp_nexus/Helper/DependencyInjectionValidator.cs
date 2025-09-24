using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mcp_nexus.Helper
{
    /// <summary>
    /// Helper class to validate dependency injection configuration during application startup.
    /// This helps catch circular dependencies and other DI issues early in production.
    /// </summary>
    public static class DependencyInjectionValidator
    {
        /// <summary>
        /// Validates that all registered services can be resolved without circular dependencies or other issues.
        /// This should be called during application startup to catch DI configuration problems early.
        /// </summary>
        /// <param name="serviceProvider">The built service provider to validate</param>
        /// <param name="logger">Logger for validation results</param>
        /// <param name="validateOnlyApplicationServices">If true, only validates application services (not framework services)</param>
        /// <returns>True if validation passed, false if there were issues</returns>
        public static bool ValidateServiceRegistration(
            IServiceProvider serviceProvider, 
            ILogger logger,
            bool validateOnlyApplicationServices = true)
        {
            try
            {
                logger.LogInformation("Starting dependency injection validation...");
                var stopwatch = Stopwatch.StartNew();

                // Get the service collection from the service provider if possible
                var serviceCollection = GetServiceCollection(serviceProvider);
                if (serviceCollection == null)
                {
                    logger.LogWarning("Could not retrieve service collection for validation");
                    return true; // Continue execution
                }

                var servicesToValidate = validateOnlyApplicationServices 
                    ? serviceCollection.Where(s => IsApplicationService(s.ServiceType)).ToList()
                    : serviceCollection.ToList();

                logger.LogInformation("Validating {ServiceCount} services...", servicesToValidate.Count);

                var failures = new List<string>();
                var successCount = 0;

                foreach (var serviceDescriptor in servicesToValidate)
                {
                    try
                    {
                        var serviceType = serviceDescriptor.ServiceType;
                        var resolvedService = serviceProvider.GetRequiredService(serviceType);
                        
                        if (resolvedService != null)
                        {
                            successCount++;
                            logger.LogDebug("✅ {ServiceType}", serviceType.Name);
                        }
                        else
                        {
                            failures.Add($"Service {serviceType.Name} resolved to null");
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("circular dependency"))
                    {
                        var error = $"❌ CIRCULAR DEPENDENCY: {serviceDescriptor.ServiceType.Name} - {ex.Message}";
                        failures.Add(error);
                        logger.LogError(error);
                    }
                    catch (Exception ex)
                    {
                        var error = $"❌ {serviceDescriptor.ServiceType.Name}: {ex.GetType().Name} - {ex.Message}";
                        failures.Add(error);
                        logger.LogError(error);
                    }
                }

                stopwatch.Stop();

                if (failures.Any())
                {
                    logger.LogError("Dependency injection validation FAILED. {FailureCount} services failed to resolve:", failures.Count);
                    foreach (var failure in failures.Take(10)) // Limit to first 10 failures
                    {
                        logger.LogError(failure);
                    }
                    
                    if (failures.Count > 10)
                    {
                        logger.LogError("... and {AdditionalFailures} more failures", failures.Count - 10);
                    }
                    
                    return false;
                }

                logger.LogInformation("✅ Dependency injection validation PASSED. {SuccessCount} services validated successfully in {ElapsedMs}ms", 
                    successCount, stopwatch.ElapsedMilliseconds);
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to perform dependency injection validation");
                return false;
            }
        }

        /// <summary>
        /// Performs a quick validation of critical application services only.
        /// This is a lightweight check that can be run during startup without significant performance impact.
        /// </summary>
        /// <param name="serviceProvider">The service provider to validate</param>
        /// <param name="logger">Logger for validation results</param>
        /// <returns>True if critical services can be resolved, false otherwise</returns>
        public static bool ValidateCriticalServices(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                logger.LogInformation("Validating critical services...");
                var stopwatch = Stopwatch.StartNew();

                // Define critical services that must be resolvable
                var criticalServiceTypes = new[]
                {
                    typeof(mcp_nexus.Helper.ICdbSession),
                    typeof(mcp_nexus.Services.ICommandQueueService),
                    typeof(mcp_nexus.Services.ICdbSessionRecoveryService),
                    typeof(mcp_nexus.Services.ICommandTimeoutService)
                };

                foreach (var serviceType in criticalServiceTypes)
                {
                    try
                    {
                        var service = serviceProvider.GetRequiredService(serviceType);
                        if (service == null)
                        {
                            logger.LogError("Critical service {ServiceType} resolved to null", serviceType.Name);
                            return false;
                        }
                        logger.LogDebug("✅ Critical service {ServiceType} OK", serviceType.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to resolve critical service {ServiceType}", serviceType.Name);
                        return false;
                    }
                }

                stopwatch.Stop();
                logger.LogInformation("✅ Critical services validation PASSED in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate critical services");
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Attempts to extract the service collection from the service provider.
        /// This works with the default .NET DI container but may not work with other containers.
        /// </summary>
        private static IEnumerable<ServiceDescriptor>? GetServiceCollection(IServiceProvider serviceProvider)
        {
            try
            {
                // Try to get the service collection from the service provider
                // This is implementation-specific to Microsoft.Extensions.DependencyInjection
                var serviceProviderType = serviceProvider.GetType();
                var callSiteFactoryField = serviceProviderType.GetField("_callSiteFactory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (callSiteFactoryField?.GetValue(serviceProvider) is not object callSiteFactory)
                {
                    return null;
                }

                var descriptorsProperty = callSiteFactory.GetType().GetProperty("Descriptors",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                return descriptorsProperty?.GetValue(callSiteFactory) as IEnumerable<ServiceDescriptor>;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if a service type is an application service (vs. framework service).
        /// </summary>
        private static bool IsApplicationService(Type serviceType)
        {
            if (serviceType.FullName == null)
                return false;

            // Framework namespaces to exclude
            var frameworkNamespaces = new[]
            {
                "Microsoft.Extensions.",
                "Microsoft.AspNetCore.",
                "Microsoft.EntityFrameworkCore.",
                "System.Net.Http",
                "System.Text.Json"
            };

            // If it starts with any framework namespace, it's not an application service
            if (frameworkNamespaces.Any(ns => serviceType.FullName.StartsWith(ns)))
                return false;

            // Include our application namespaces
            var applicationNamespaces = new[]
            {
                "mcp_nexus.",
                // Add other application namespaces as needed
            };

            return applicationNamespaces.Any(ns => serviceType.FullName.StartsWith(ns));
        }

        #endregion
    }
}
