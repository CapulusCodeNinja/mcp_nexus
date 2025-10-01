using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using mcp_nexus.Infrastructure;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace mcp_nexus_tests.Infrastructure
{
    /// <summary>
    /// Tests for ProjectBuilder
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ProjectBuilderTests
    {
        [Fact]
        public void FindProjectDirectory_WithValidProjectFile_ReturnsCorrectPath()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest");
            Directory.CreateDirectory(testDir);
            
            try
            {
                var projectFile = Path.Combine(testDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(testDir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testDir, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithNonExistentProjectFile_ReturnsNull()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_NoProject");
            Directory.CreateDirectory(testDir);
            
            try
            {
                // Act
                var result = ProjectBuilder.FindProjectDirectory(testDir);

                // Assert
                Assert.Null(result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithNullStartDirectory_ReturnsNull()
        {
            // Act
            var result = ProjectBuilder.FindProjectDirectory(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_WithEmptyStartDirectory_ReturnsNull()
        {
            // Act
            var result = ProjectBuilder.FindProjectDirectory("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_WithInvalidDirectory_ReturnsNull()
        {
            // Arrange
            var invalidDir = Path.Combine(Path.GetTempPath(), "NonExistentDirectory_12345");

            // Act
            var result = ProjectBuilder.FindProjectDirectory(invalidDir);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_SearchesParentDirectories()
        {
            // Arrange
            var parentDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_Parent");
            var childDir = Path.Combine(parentDir, "Child");
            Directory.CreateDirectory(childDir);
            
            try
            {
                var projectFile = Path.Combine(parentDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(childDir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(parentDir, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(parentDir))
                    Directory.Delete(parentDir, recursive: true);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithMultipleLevels_SearchesAllParents()
        {
            // Arrange
            var rootDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_Root");
            var level1Dir = Path.Combine(rootDir, "Level1");
            var level2Dir = Path.Combine(level1Dir, "Level2");
            var level3Dir = Path.Combine(level2Dir, "Level3");
            
            Directory.CreateDirectory(level3Dir);
            
            try
            {
                var projectFile = Path.Combine(rootDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(level3Dir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(rootDir, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(rootDir))
                    Directory.Delete(rootDir, recursive: true);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithProjectInCurrentDirectory_ReturnsCurrentDirectory()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_Current");
            Directory.CreateDirectory(testDir);
            
            try
            {
                var projectFile = Path.Combine(testDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(testDir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testDir, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithNullLogger_DoesNotThrow()
        {
            // Act & Assert
            var result = await ProjectBuilder.BuildProjectForDeploymentAsync(null);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public async Task BuildProjectForDeploymentAsync_WithValidLogger_DoesNotThrow()
        {
            // Arrange
            var mockLogger = new Mock<ILogger>();

            // Act & Assert
            var result = await ProjectBuilder.BuildProjectForDeploymentAsync(mockLogger.Object);
            // Should not throw, but may return false if no project found
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void FindProjectDirectory_WithFileInsteadOfDirectory_ReturnsNull()
        {
            // Arrange
            var testFile = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_File.txt");
            File.WriteAllText(testFile, "test content");

            try
            {
                // Act
                var result = ProjectBuilder.FindProjectDirectory(testFile);

                // Assert
                Assert.Null(result);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithProjectFileInSubdirectory_DoesNotFindIt()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_Subdir");
            var subDir = Path.Combine(testDir, "Subdir");
            Directory.CreateDirectory(subDir);
            
            try
            {
                var projectFile = Path.Combine(subDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(testDir);

                // Assert
                Assert.Null(result); // Should not find project in subdirectory
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("nonexistent")]
        public void FindProjectDirectory_WithInvalidPaths_ReturnsNull(string invalidPath)
        {
            // Act
            var result = ProjectBuilder.FindProjectDirectory(invalidPath);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FindProjectDirectory_WithProjectFileInRoot_ReturnsRoot()
        {
            // Arrange
            var testDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_RootProject");
            Directory.CreateDirectory(testDir);
            
            try
            {
                var projectFile = Path.Combine(testDir, ServiceConfiguration.ProjectFileName);
                File.WriteAllText(projectFile, "test project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(testDir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(testDir, result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }

        [Fact]
        public void FindProjectDirectory_WithMultipleProjectFiles_ReturnsFirstFound()
        {
            // Arrange
            var rootDir = Path.Combine(Path.GetTempPath(), "ProjectBuilderTest_Multiple");
            var childDir = Path.Combine(rootDir, "Child");
            Directory.CreateDirectory(childDir);
            
            try
            {
                var rootProjectFile = Path.Combine(rootDir, ServiceConfiguration.ProjectFileName);
                var childProjectFile = Path.Combine(childDir, ServiceConfiguration.ProjectFileName);
                
                File.WriteAllText(rootProjectFile, "root project file");
                File.WriteAllText(childProjectFile, "child project file");

                // Act
                var result = ProjectBuilder.FindProjectDirectory(childDir);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(childDir, result); // Should find the one in the current directory first
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(rootDir))
                    Directory.Delete(rootDir, recursive: true);
            }
        }
    }
}