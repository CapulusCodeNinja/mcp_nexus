using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using mcp_nexus.Services;
using Moq;
using Xunit;

namespace mcp_nexus_tests.Services
{
	public class WindowsServiceInstallerTests
	{
		private readonly Mock<ILogger> m_mockLogger;

		public WindowsServiceInstallerTests()
		{
			m_mockLogger = new Mock<ILogger>();
		}

		[Fact]
		public void WindowsServiceInstaller_Class_HasCorrectAttributes()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act
			var attributes = type.GetCustomAttributes<SupportedOSPlatformAttribute>();

			// Assert
			Assert.NotEmpty(attributes);
			var platformAttribute = attributes.First();
			Assert.Equal("windows", platformAttribute.PlatformName);
		}

		[Fact]
		public void WindowsServiceInstaller_IsStaticClass()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Assert
			Assert.True(type.IsAbstract);
			Assert.True(type.IsSealed);
		}

		[Fact]
		public void WindowsServiceInstaller_HasExpectedConstants()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act
			var serviceNameField = type.GetField("ServiceName", BindingFlags.NonPublic | BindingFlags.Static);
			var serviceDisplayNameField = type.GetField("ServiceDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
			var serviceDescriptionField = type.GetField("ServiceDescription", BindingFlags.NonPublic | BindingFlags.Static);
			var installFolderField = type.GetField("InstallFolder", BindingFlags.NonPublic | BindingFlags.Static);

			// Assert
			Assert.NotNull(serviceNameField);
			Assert.NotNull(serviceDisplayNameField);
			Assert.NotNull(serviceDescriptionField);
			Assert.NotNull(installFolderField);

			Assert.Equal("MCP-Nexus", serviceNameField!.GetValue(null));
			Assert.Equal("MCP Nexus Server", serviceDisplayNameField!.GetValue(null));
			Assert.Equal("Model Context Protocol server providing AI tool integration", serviceDescriptionField!.GetValue(null));
			Assert.Equal(@"C:\Program Files\MCP-Nexus", installFolderField!.GetValue(null));
		}

		[Fact]
		public void WindowsServiceInstaller_HasPublicMethods()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act
			var installMethod = type.GetMethod("InstallServiceAsync", BindingFlags.Public | BindingFlags.Static);
			var uninstallMethod = type.GetMethod("UninstallServiceAsync", BindingFlags.Public | BindingFlags.Static);
			var forceUninstallMethod = type.GetMethod("ForceUninstallServiceAsync", BindingFlags.Public | BindingFlags.Static);
			var updateMethod = type.GetMethod("UpdateServiceAsync", BindingFlags.Public | BindingFlags.Static);

			// Assert
			Assert.NotNull(installMethod);
			Assert.NotNull(uninstallMethod);
			Assert.NotNull(forceUninstallMethod);
			Assert.NotNull(updateMethod);

			// Check return types
			Assert.Equal(typeof(Task<bool>), installMethod!.ReturnType);
			Assert.Equal(typeof(Task<bool>), uninstallMethod!.ReturnType);
			Assert.Equal(typeof(Task<bool>), forceUninstallMethod!.ReturnType);
			Assert.Equal(typeof(Task<bool>), updateMethod!.ReturnType);
		}

		[Fact]
		public void WindowsServiceInstaller_HasPrivateHelperMethods()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act
			var buildProjectMethod = type.GetMethod("BuildProjectForDeploymentAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var findProjectMethod = type.GetMethod("FindProjectDirectory", BindingFlags.NonPublic | BindingFlags.Static);
			var copyFilesMethod = type.GetMethod("CopyApplicationFilesAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var copyDirectoryMethod = type.GetMethod("CopyDirectoryAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var forceCleanupMethod = type.GetMethod("ForceCleanupServiceAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var directRegistryMethod = type.GetMethod("DirectRegistryCleanupAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var runScCommandMethod = type.GetMethod("RunScCommandAsync", BindingFlags.NonPublic | BindingFlags.Static);
			var isServiceInstalledMethod = type.GetMethod("IsServiceInstalled", BindingFlags.NonPublic | BindingFlags.Static);
			var isAdminMethod = type.GetMethod("IsRunAsAdministrator", BindingFlags.NonPublic | BindingFlags.Static);

			// Assert
			Assert.NotNull(buildProjectMethod);
			Assert.NotNull(findProjectMethod);
			Assert.NotNull(copyFilesMethod);
			Assert.NotNull(copyDirectoryMethod);
			Assert.NotNull(forceCleanupMethod);
			Assert.NotNull(directRegistryMethod);
			Assert.NotNull(runScCommandMethod);
			Assert.NotNull(isServiceInstalledMethod);
			Assert.NotNull(isAdminMethod);
		}

		[Fact]
		public void WindowsServiceInstaller_FindProjectDirectory_WithValidPath_ReturnsProjectPath()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);
			var findProjectMethod = type.GetMethod("FindProjectDirectory", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(findProjectMethod);

			// Create a temporary directory structure with a .csproj file
			var tempDir = Path.GetTempPath();
			var testDir = Path.Combine(tempDir, "TestProject");
			var subDir = Path.Combine(testDir, "SubDir");
			
			try
			{
				Directory.CreateDirectory(subDir);
				var csprojFile = Path.Combine(testDir, "test.csproj");
				File.WriteAllText(csprojFile, "<Project></Project>");

				// Act
				var result = (string?)findProjectMethod!.Invoke(null, new object[] { subDir });

				// Assert
				Assert.NotNull(result);
				Assert.Equal(testDir, result);
			}
			finally
			{
				// Cleanup
				if (Directory.Exists(testDir))
				{
					Directory.Delete(testDir, true);
				}
			}
		}

		[Fact]
		public void WindowsServiceInstaller_FindProjectDirectory_WithInvalidPath_ReturnsNull()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);
			var findProjectMethod = type.GetMethod("FindProjectDirectory", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(findProjectMethod);

			// Use a path that doesn't contain a .csproj file
			var tempDir = Path.GetTempPath();
			var testDir = Path.Combine(tempDir, "NoProjectHere");
			
			try
			{
				Directory.CreateDirectory(testDir);

				// Act
				var result = (string?)findProjectMethod!.Invoke(null, new object[] { testDir });

				// Assert
				Assert.Null(result);
			}
			finally
			{
				// Cleanup
				if (Directory.Exists(testDir))
				{
					Directory.Delete(testDir, true);
				}
			}
		}

		[Fact]
		public void WindowsServiceInstaller_IsServiceInstalled_ReturnsBoolean()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);
			var isInstalledMethod = type.GetMethod("IsServiceInstalled", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(isInstalledMethod);

			// Act
			var result = (bool)isInstalledMethod!.Invoke(null, null)!;

			// Assert
			Assert.IsType<bool>(result);
			// The result can be true or false depending on whether the service is actually installed
		}

		[Fact]
		public void WindowsServiceInstaller_IsRunAsAdministrator_ReturnsBoolean()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);
			var isAdminMethod = type.GetMethod("IsRunAsAdministrator", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(isAdminMethod);

			// Act
			var result = (bool)isAdminMethod!.Invoke(null, null)!;

			// Assert
			Assert.IsType<bool>(result);
			// The result depends on whether the test is running as administrator
		}

		[Fact]
		public async Task WindowsServiceInstaller_CopyDirectoryAsync_CreatesTargetDirectory()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);
			var copyDirectoryMethod = type.GetMethod("CopyDirectoryAsync", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(copyDirectoryMethod);

			var tempDir = Path.GetTempPath();
			var sourceDir = Path.Combine(tempDir, "SourceDir");
			var targetDir = Path.Combine(tempDir, "TargetDir");

			try
			{
				// Create source directory with a test file
				Directory.CreateDirectory(sourceDir);
				var testFile = Path.Combine(sourceDir, "test.txt");
				File.WriteAllText(testFile, "test content");

				// Act
				var task = (Task)copyDirectoryMethod!.Invoke(null, new object[] { sourceDir, targetDir })!;
				await task;

				// Assert
				Assert.True(Directory.Exists(targetDir));
				var copiedFile = Path.Combine(targetDir, "test.txt");
				Assert.True(File.Exists(copiedFile));
				Assert.Equal("test content", File.ReadAllText(copiedFile));
			}
			finally
			{
				// Cleanup
				if (Directory.Exists(sourceDir))
				{
					Directory.Delete(sourceDir, true);
				}
				if (Directory.Exists(targetDir))
				{
					Directory.Delete(targetDir, true);
				}
			}
		}

		[Fact]
		public void WindowsServiceInstaller_MethodSignatures_AreCorrect()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act & Assert - InstallServiceAsync
			var installMethod = type.GetMethod("InstallServiceAsync");
			Assert.NotNull(installMethod);
			var installParams = installMethod!.GetParameters();
			Assert.Single(installParams);
			Assert.Equal("logger", installParams[0].Name);
			Assert.Equal(typeof(ILogger), installParams[0].ParameterType);
			Assert.True(installParams[0].HasDefaultValue);

			// Act & Assert - UninstallServiceAsync
			var uninstallMethod = type.GetMethod("UninstallServiceAsync");
			Assert.NotNull(uninstallMethod);
			var uninstallParams = uninstallMethod!.GetParameters();
			Assert.Single(uninstallParams);
			Assert.Equal("logger", uninstallParams[0].Name);
			Assert.Equal(typeof(ILogger), uninstallParams[0].ParameterType);
			Assert.True(uninstallParams[0].HasDefaultValue);
		}

		[Fact]
		public void WindowsServiceInstaller_PrivateMethodSignatures_AreCorrect()
		{
			// Arrange
			var type = typeof(WindowsServiceInstaller);

			// Act & Assert - FindProjectDirectory
			var findMethod = type.GetMethod("FindProjectDirectory", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(findMethod);
			var findParams = findMethod!.GetParameters();
			Assert.Single(findParams);
			Assert.Equal("startPath", findParams[0].Name);
			Assert.Equal(typeof(string), findParams[0].ParameterType);

			// Act & Assert - RunScCommandAsync
			var runScMethod = type.GetMethod("RunScCommandAsync", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(runScMethod);
			var runScParams = runScMethod!.GetParameters();
			Assert.Equal(3, runScParams.Length);
			Assert.Equal("arguments", runScParams[0].Name);
			Assert.Equal("logger", runScParams[1].Name);
			Assert.Equal("allowFailure", runScParams[2].Name);
			Assert.Equal(typeof(string), runScParams[0].ParameterType);
			Assert.Equal(typeof(ILogger), runScParams[1].ParameterType);
			Assert.Equal(typeof(bool), runScParams[2].ParameterType);
		}
	}
}
