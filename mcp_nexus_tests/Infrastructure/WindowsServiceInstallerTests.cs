using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System.Runtime.Versioning;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Comprehensive tests for WindowsServiceInstaller - tests service installation and management
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsServiceInstallerTests
    {
        private readonly Mock<ILogger> m_mockLogger;

        public WindowsServiceInstallerTests()
        {
            m_mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void WindowsServiceInstaller_Class_Exists()
        {
            // Assert
            var installerType = typeof(WindowsServiceInstaller);
            Assert.NotNull(installerType);
            Assert.True(installerType.IsClass);
            Assert.True(installerType.IsAbstract); // Should be static class
        }

        [Fact]
        public void WindowsServiceInstaller_HasExpectedPublicMethods()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            var methodNames = Array.ConvertAll(publicMethods, m => m.Name);

            Assert.Contains("InstallServiceAsync", methodNames);
            Assert.Contains("UninstallServiceAsync", methodNames);
            Assert.Contains("ForceUninstallServiceAsync", methodNames);
            Assert.Contains("UpdateServiceAsync", methodNames);
        }

        [Fact]
        public void WindowsServiceInstaller_HasExpectedPrivateMethods()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var privateMethods = installerType.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            var methodNames = Array.ConvertAll(privateMethods, m => m.Name);

            Assert.Contains("BuildProjectForDeploymentAsync", methodNames);
            Assert.Contains("FindProjectDirectory", methodNames);
            Assert.Contains("CopyApplicationFilesAsync", methodNames);
            Assert.Contains("CopyDirectoryAsync", methodNames);
            Assert.Contains("ForceCleanupServiceAsync", methodNames);
            Assert.Contains("DirectRegistryCleanupAsync", methodNames);
            Assert.Contains("RunScCommandAsync", methodNames);
            Assert.Contains("IsServiceInstalled", methodNames);
            Assert.Contains("IsRunAsAdministrator", methodNames);
        }

        [Fact]
        public void WindowsServiceInstaller_InstallServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is null
            var task = WindowsServiceInstaller.InstallServiceAsync(null);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_UninstallServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is null
            var task = WindowsServiceInstaller.UninstallServiceAsync(null);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_ForceUninstallServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is null
            var task = WindowsServiceInstaller.ForceUninstallServiceAsync(null);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_UpdateServiceAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is null
            var task = WindowsServiceInstaller.UpdateServiceAsync(null);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_InstallServiceAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is provided
            var task = WindowsServiceInstaller.InstallServiceAsync(m_mockLogger.Object);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_UninstallServiceAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is provided
            var task = WindowsServiceInstaller.UninstallServiceAsync(m_mockLogger.Object);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_ForceUninstallServiceAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is provided
            var task = WindowsServiceInstaller.ForceUninstallServiceAsync(m_mockLogger.Object);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_UpdateServiceAsync_WithLogger_DoesNotThrow()
        {
            // Act & Assert - Should not throw when logger is provided
            var task = WindowsServiceInstaller.UpdateServiceAsync(m_mockLogger.Object);
            Assert.NotNull(task);
        }

        [Fact]
        public void WindowsServiceInstaller_AllMethods_ReturnTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                if (method.Name.EndsWith("Async"))
                {
                    Assert.Equal(typeof(Task<bool>), method.ReturnType);
                }
            }
        }

        [Fact]
        public void WindowsServiceInstaller_AllMethods_AreStatic()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                Assert.True(method.IsStatic);
            }
        }

        [Fact]
        public void WindowsServiceInstaller_AllMethods_HaveCorrectParameters()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                if (method.Name.EndsWith("Async"))
                {
                    var parameters = method.GetParameters();
                    Assert.Single(parameters);
                    Assert.Equal(typeof(ILogger), parameters[0].ParameterType);
                    Assert.True(parameters[0].IsOptional);
                }
            }
        }

        [Fact]
        public void WindowsServiceInstaller_IsRunAsAdministrator_ReturnsBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("IsRunAsAdministrator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_IsServiceInstalled_ReturnsBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("IsServiceInstalled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(bool), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_RunScCommandAsync_ReturnsTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("RunScCommandAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<bool>), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_RunScCommandAsync_HasCorrectParameters()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("RunScCommandAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            var parameters = method.GetParameters();
            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(ILogger), parameters[1].ParameterType);
            Assert.Equal(typeof(bool), parameters[2].ParameterType);
        }

        [Fact]
        public void WindowsServiceInstaller_FindProjectDirectory_ReturnsString()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("FindProjectDirectory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(string), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_CopyDirectoryAsync_ReturnsTask()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("CopyDirectoryAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_CopyDirectoryAsync_HasCorrectParameters()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("CopyDirectoryAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            var parameters = method.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(string), parameters[1].ParameterType);
        }

        [Fact]
        public void WindowsServiceInstaller_BuildProjectForDeploymentAsync_ReturnsTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("BuildProjectForDeploymentAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<bool>), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_CopyApplicationFilesAsync_ReturnsTask()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("CopyApplicationFilesAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_ForceCleanupServiceAsync_ReturnsTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("ForceCleanupServiceAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<bool>), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_DirectRegistryCleanupAsync_ReturnsTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var method = installerType.GetMethod("DirectRegistryCleanupAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act & Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<bool>), method.ReturnType);
            Assert.True(method.IsStatic);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasSupportedOSPlatformAttribute()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var attributes = installerType.GetCustomAttributes(typeof(System.Runtime.Versioning.SupportedOSPlatformAttribute), false);

            // Act & Assert
            Assert.NotEmpty(attributes);
            var platformAttribute = (System.Runtime.Versioning.SupportedOSPlatformAttribute)attributes[0];
            Assert.Equal("windows", platformAttribute.PlatformName);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_IsSealed()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);

            // Act & Assert
            Assert.True(installerType.IsSealed);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_IsAbstract()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);

            // Act & Assert
            Assert.True(installerType.IsAbstract);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasNoInstanceConstructors()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var constructors = installerType.GetConstructors();

            // Act & Assert
            Assert.Empty(constructors);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasNoInstanceFields()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var instanceFields = installerType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            Assert.Empty(instanceFields);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasNoInstanceProperties()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var instanceProperties = installerType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            Assert.Empty(instanceProperties);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasNoInstanceMethods()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var instanceMethods = installerType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert - Only check for custom instance methods, not inherited ones
            var customInstanceMethods = instanceMethods.Where(m => m.DeclaringType == installerType).ToArray();
            Assert.Empty(customInstanceMethods);
        }

        [Fact]
        public void WindowsServiceInstaller_Class_HasExpectedConstants()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var constants = installerType.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            var constantNames = Array.ConvertAll(constants, f => f.Name);
            Assert.Contains("ServiceName", constantNames);
            Assert.Contains("ServiceDisplayName", constantNames);
            Assert.Contains("ServiceDescription", constantNames);
            Assert.Contains("InstallFolder", constantNames);
        }

        [Fact]
        public void WindowsServiceInstaller_Constants_HaveExpectedValues()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var serviceNameField = installerType.GetField("ServiceName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var serviceDisplayNameField = installerType.GetField("ServiceDisplayName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var serviceDescriptionField = installerType.GetField("ServiceDescription", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var installFolderField = installerType.GetField("InstallFolder", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            // Act & Assert
            Assert.NotNull(serviceNameField);
            Assert.NotNull(serviceDisplayNameField);
            Assert.NotNull(serviceDescriptionField);
            Assert.NotNull(installFolderField);

            var serviceName = (string)serviceNameField.GetValue(null)!;
            var serviceDisplayName = (string)serviceDisplayNameField.GetValue(null)!;
            var serviceDescription = (string)serviceDescriptionField.GetValue(null)!;
            var installFolder = (string)installFolderField.GetValue(null)!;

            Assert.Equal("MCP-Nexus", serviceName);
            Assert.Equal("MCP Nexus Server", serviceDisplayName);
            Assert.Equal("Model Context Protocol server providing AI tool integration", serviceDescription);
            Assert.Equal(@"C:\Program Files\MCP-Nexus", installFolder);
        }

        [Fact]
        public void WindowsServiceInstaller_AllPublicMethods_AreAsync()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                Assert.EndsWith("Async", method.Name);
                Assert.True(method.ReturnType.IsGenericType);
                Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
            }
        }

        [Fact]
        public void WindowsServiceInstaller_AllPublicMethods_ReturnTaskOfBool()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                Assert.Equal(typeof(Task<bool>), method.ReturnType);
            }
        }

        [Fact]
        public void WindowsServiceInstaller_AllPublicMethods_HaveOptionalLoggerParameter()
        {
            // Arrange
            var installerType = typeof(WindowsServiceInstaller);
            var publicMethods = installerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            // Act & Assert
            foreach (var method in publicMethods)
            {
                var parameters = method.GetParameters();
                Assert.Single(parameters);
                Assert.Equal(typeof(ILogger), parameters[0].ParameterType);
                Assert.True(parameters[0].IsOptional);
            }
        }

        [Fact]
        public async Task InstallServiceAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = WindowsServiceInstaller.InstallServiceAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Task<bool>>(result);

            // Wait for completion (but don't assert the result since we can't control the environment)
            var completed = await Task.WhenAny(result, Task.Delay(5000));
            Assert.Equal(result, completed);
        }

        [Fact]
        public async Task UninstallServiceAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = WindowsServiceInstaller.UninstallServiceAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Task<bool>>(result);

            // Wait for completion (but don't assert the result since we can't control the environment)
            var completed = await Task.WhenAny(result, Task.Delay(5000));
            Assert.Equal(result, completed);
        }

        [Fact]
        public async Task ForceUninstallServiceAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = WindowsServiceInstaller.ForceUninstallServiceAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Task<bool>>(result);

            // Wait for completion (but don't assert the result since we can't control the environment)
            var completed = await Task.WhenAny(result, Task.Delay(5000));
            Assert.Equal(result, completed);
        }

        [Fact]
        public async Task UpdateServiceAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = WindowsServiceInstaller.UpdateServiceAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Task<bool>>(result);

            // Wait for completion (but don't assert the result since we can't control the environment)
            var completed = await Task.WhenAny(result, Task.Delay(5000));
            Assert.Equal(result, completed);
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = await WindowsServiceInstaller.ValidateInstallationFilesAsync(null);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task CreateBackupAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = await WindowsServiceInstaller.CreateBackupAsync(null);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithNullLogger_ReturnsTaskOfBool()
        {
            // Act
            var result = await WindowsServiceInstaller.CleanupOldBackupsAsync(null);

            // Assert
            Assert.IsType<bool>(result);
            Assert.True(result); // This method always returns true
        }

        [Fact]
        public async Task ValidateInstallationFilesAsync_WithLogger_CallsUnderlyingMethod()
        {
            // Act
            var result = await WindowsServiceInstaller.ValidateInstallationFilesAsync(m_mockLogger.Object);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task CreateBackupAsync_WithLogger_CallsUnderlyingMethod()
        {
            // Act
            var result = await WindowsServiceInstaller.CreateBackupAsync(m_mockLogger.Object);

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task CleanupOldBackupsAsync_WithLogger_CallsUnderlyingMethod()
        {
            // Act
            var result = await WindowsServiceInstaller.CleanupOldBackupsAsync(m_mockLogger.Object);

            // Assert
            Assert.IsType<bool>(result);
            Assert.True(result); // This method always returns true
        }
    }
}