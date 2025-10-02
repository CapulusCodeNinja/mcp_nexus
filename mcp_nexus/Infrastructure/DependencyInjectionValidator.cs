using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using mcp_nexus.Notifications;

namespace mcp_nexus.Infrastructure
{
    /// <summary>
    /// Validates dependency injection configuration
    /// </summary>
    public class DependencyInjectionValidator
    {
        private readonly IServiceCollection _services;
        private readonly List<ValidationResult> _validationResults = new();

        public DependencyInjectionValidator(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public ValidationResult Validate()
        {
            _validationResults.Clear();
            ValidateServices();
            ValidateCircularDependencies();
            ValidateLifetimeMismatches();
            ValidateMissingDependencies();

            return new ValidationResult
            {
                IsValid = !_validationResults.Any(r => r.Severity == ValidationSeverity.Error),
                Results = _validationResults.ToList()
            };
        }

        public ValidationResult ValidateServiceRegistration(Type serviceType, Type implementationType)
        {
            _validationResults.Clear();
            
            // Check if service type is valid
            if (serviceType == null)
            {
                _validationResults.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Service type cannot be null",
                    ServiceType = "Unknown"
                });
            }

            // Check if implementation type is valid
            if (implementationType == null)
            {
                _validationResults.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = "Implementation type cannot be null",
                    ServiceType = serviceType?.Name ?? "Unknown"
                });
            }

            // Check if implementation type can be assigned to service type
            if (serviceType != null && implementationType != null && !serviceType.IsAssignableFrom(implementationType))
            {
                _validationResults.Add(new ValidationResult
                {
                    Severity = ValidationSeverity.Error,
                    Message = $"Implementation type {implementationType.Name} cannot be assigned to service type {serviceType.Name}",
                    ServiceType = serviceType.Name
                });
            }

            return new ValidationResult
            {
                IsValid = !_validationResults.Any(r => r.Severity == ValidationSeverity.Error),
                Results = _validationResults.ToList()
            };
        }

        public static bool ValidateServiceRegistration(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                if (serviceProvider == null)
                    return false;

                if (logger == null)
                    return false;

                // Basic validation - check if service provider can resolve common services
                var loggerService = serviceProvider.GetService<ILogger>();
                return loggerService != null;
            }
            catch
            {
                return false;
            }
        }

        public static ValidationResult ValidateCriticalServices(IServiceProvider serviceProvider, ILogger logger)
        {
            try
            {
                if (serviceProvider == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Service provider is null",
                        Severity = ValidationSeverity.Error
                    };
                }

                if (logger == null)
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        Message = "Logger is null",
                        Severity = ValidationSeverity.Error
                    };
                }

                // Check for critical services that must be registered
                var criticalServices = new[]
                {
                    typeof(ILogger),
                    typeof(IMcpNotificationService),
                    typeof(ServiceConfiguration)
                };

                var results = new List<ValidationResult>();

                foreach (var serviceType in criticalServices)
                {
                    var service = serviceProvider.GetService(serviceType);
                    if (service == null)
                    {
                        results.Add(new ValidationResult
                        {
                            Severity = ValidationSeverity.Error,
                            Message = $"Critical service {serviceType.Name} is not registered",
                            ServiceType = serviceType.Name
                        });
                    }
                }

                return new ValidationResult
                {
                    IsValid = !results.Any(r => r.Severity == ValidationSeverity.Error),
                    Results = results
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Validation failed: {ex.Message}",
                    Severity = ValidationSeverity.Error
                };
            }
        }

        private void ValidateServices()
        {
            foreach (var service in _services)
            {
                if (service.ServiceType == null)
                {
                    _validationResults.Add(new ValidationResult
                    {
                        Severity = ValidationSeverity.Error,
                        Message = "Service type cannot be null",
                        ServiceType = service.ServiceType?.Name ?? "Unknown"
                    });
                }

                if (service.ImplementationType == null && service.ImplementationFactory == null && service.ImplementationInstance == null)
                {
                    _validationResults.Add(new ValidationResult
                    {
                        Severity = ValidationSeverity.Warning,
                        Message = "Service has no implementation",
                        ServiceType = service.ServiceType?.Name ?? "Unknown"
                    });
                }
            }
        }

        private void ValidateCircularDependencies()
        {
            var visited = new HashSet<Type>();
            var recursionStack = new HashSet<Type>();

            foreach (var service in _services.Where(s => s.ServiceType != null))
            {
                if (!visited.Contains(service.ServiceType))
                {
                    CheckForCircularDependency(service.ServiceType, visited, recursionStack);
                }
            }
        }

        private void CheckForCircularDependency(Type serviceType, HashSet<Type> visited, HashSet<Type> recursionStack)
        {
            visited.Add(serviceType);
            recursionStack.Add(serviceType);

            var constructor = serviceType.GetConstructors().FirstOrDefault();
            if (constructor != null)
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    if (recursionStack.Contains(parameter.ParameterType))
                    {
                        _validationResults.Add(new ValidationResult
                        {
                            Severity = ValidationSeverity.Error,
                            Message = $"Circular dependency detected: {serviceType.Name} -> {parameter.ParameterType.Name}",
                            ServiceType = serviceType.Name
                        });
                    }
                    else if (!visited.Contains(parameter.ParameterType))
                    {
                        CheckForCircularDependency(parameter.ParameterType, visited, recursionStack);
                    }
                }
            }

            recursionStack.Remove(serviceType);
        }

        private void ValidateLifetimeMismatches()
        {
            var serviceMap = _services.ToDictionary(s => s.ServiceType, s => s);

            foreach (var service in _services.Where(s => s.ServiceType != null))
            {
                var constructor = service.ServiceType.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        if (serviceMap.TryGetValue(parameter.ParameterType, out var dependency))
                        {
                            if (IsLifetimeMismatch(service.Lifetime, dependency.Lifetime))
                            {
                                _validationResults.Add(new ValidationResult
                                {
                                    Severity = ValidationSeverity.Warning,
                                    Message = $"Lifetime mismatch: {service.ServiceType.Name} ({service.Lifetime}) depends on {parameter.ParameterType.Name} ({dependency.Lifetime})",
                                    ServiceType = service.ServiceType.Name
                                });
                            }
                        }
                    }
                }
            }
        }

        private bool IsLifetimeMismatch(ServiceLifetime dependent, ServiceLifetime dependency)
        {
            return (dependent == ServiceLifetime.Singleton && dependency == ServiceLifetime.Scoped) ||
                   (dependent == ServiceLifetime.Singleton && dependency == ServiceLifetime.Transient) ||
                   (dependent == ServiceLifetime.Scoped && dependency == ServiceLifetime.Transient);
        }

        private void ValidateMissingDependencies()
        {
            foreach (var service in _services.Where(s => s.ServiceType != null))
            {
                var constructor = service.ServiceType.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        if (!_services.Any(s => s.ServiceType == parameter.ParameterType))
                        {
                            _validationResults.Add(new ValidationResult
                            {
                                Severity = ValidationSeverity.Error,
                                Message = $"Missing dependency: {service.ServiceType.Name} requires {parameter.ParameterType.Name}",
                                ServiceType = service.ServiceType.Name
                            });
                        }
                    }
                }
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationResult> Results { get; set; } = new();
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}
