using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using mcp_nexus.Infrastructure;

namespace mcp_nexus_tests.Infrastructure
{
    public class ServiceRegistryInfoTests
    {
        [Fact]
        public void ServiceRegistryInfo_Class_Exists()
        {
            // Act & Assert
            Assert.True(typeof(ServiceRegistryInfo).IsClass);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotSealed()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsSealed);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotStatic()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsAbstract);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotAbstract()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsAbstract);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotInterface()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsInterface);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotEnum()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsEnum);
        }

        [Fact]
        public void ServiceRegistryInfo_IsNotValueType()
        {
            // Act & Assert
            Assert.False(typeof(ServiceRegistryInfo).IsValueType);
        }

        [Fact]
        public void Constructor_Default_InitializesWithDefaultValues()
        {
            // Act
            var info = new ServiceRegistryInfo();

            // Assert
            Assert.Equal(string.Empty, info.ServiceName);
            Assert.Equal(string.Empty, info.DisplayName);
            Assert.Equal(string.Empty, info.Description);
            Assert.Equal(string.Empty, info.ImagePath);
            Assert.Equal(default(ServiceStartType), info.StartType);
            Assert.Equal(string.Empty, info.ObjectName);
            Assert.NotNull(info.Dependencies);
            Assert.Empty(info.Dependencies);
        }

        [Fact]
        public void ServiceName_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var serviceName = "TestService";

            // Act
            info.ServiceName = serviceName;

            // Assert
            Assert.Equal(serviceName, info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ServiceName = null!;

            // Assert
            Assert.Null(info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithEmptyString_SetsToEmptyString()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ServiceName = string.Empty;

            // Assert
            Assert.Equal(string.Empty, info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithWhitespace_SetsToWhitespace()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var whitespace = "   ";

            // Act
            info.ServiceName = whitespace;

            // Assert
            Assert.Equal(whitespace, info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var unicodeName = "服务名称测试";

            // Act
            info.ServiceName = unicodeName;

            // Assert
            Assert.Equal(unicodeName, info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var specialName = "Test-Service_123!@#$%^&*()";

            // Act
            info.ServiceName = specialName;

            // Assert
            Assert.Equal(specialName, info.ServiceName);
        }

        [Fact]
        public void ServiceName_WithVeryLongString_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var longName = new string('A', 10000);

            // Act
            info.ServiceName = longName;

            // Assert
            Assert.Equal(longName, info.ServiceName);
        }

        [Fact]
        public void DisplayName_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var displayName = "Test Service Display Name";

            // Act
            info.DisplayName = displayName;

            // Assert
            Assert.Equal(displayName, info.DisplayName);
        }

        [Fact]
        public void DisplayName_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.DisplayName = null!;

            // Assert
            Assert.Null(info.DisplayName);
        }

        [Fact]
        public void DisplayName_WithEmptyString_SetsToEmptyString()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.DisplayName = string.Empty;

            // Assert
            Assert.Equal(string.Empty, info.DisplayName);
        }

        [Fact]
        public void DisplayName_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var unicodeDisplayName = "测试服务显示名称";

            // Act
            info.DisplayName = unicodeDisplayName;

            // Assert
            Assert.Equal(unicodeDisplayName, info.DisplayName);
        }

        [Fact]
        public void Description_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var description = "This is a test service description";

            // Act
            info.Description = description;

            // Assert
            Assert.Equal(description, info.Description);
        }

        [Fact]
        public void Description_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.Description = null!;

            // Assert
            Assert.Null(info.Description);
        }

        [Fact]
        public void Description_WithEmptyString_SetsToEmptyString()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.Description = string.Empty;

            // Assert
            Assert.Equal(string.Empty, info.Description);
        }

        [Fact]
        public void Description_WithVeryLongString_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var longDescription = new string('D', 50000);

            // Act
            info.Description = longDescription;

            // Assert
            Assert.Equal(longDescription, info.Description);
        }

        [Fact]
        public void ImagePath_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var imagePath = @"C:\Program Files\TestService\TestService.exe";

            // Act
            info.ImagePath = imagePath;

            // Assert
            Assert.Equal(imagePath, info.ImagePath);
        }

        [Fact]
        public void ImagePath_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ImagePath = null!;

            // Assert
            Assert.Null(info.ImagePath);
        }

        [Fact]
        public void ImagePath_WithEmptyString_SetsToEmptyString()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ImagePath = string.Empty;

            // Assert
            Assert.Equal(string.Empty, info.ImagePath);
        }

        [Fact]
        public void ImagePath_WithUnicodePath_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var unicodePath = @"C:\测试路径\服务程序.exe";

            // Act
            info.ImagePath = unicodePath;

            // Assert
            Assert.Equal(unicodePath, info.ImagePath);
        }

        [Fact]
        public void StartType_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var startType = ServiceStartType.Automatic;

            // Act
            info.StartType = startType;

            // Assert
            Assert.Equal(startType, info.StartType);
        }

        [Theory]
        [InlineData(ServiceStartType.Automatic)]
        [InlineData(ServiceStartType.Manual)]
        [InlineData(ServiceStartType.Disabled)]
        [InlineData(ServiceStartType.Boot)]
        [InlineData(ServiceStartType.System)]
        public void StartType_WithAllEnumValues_HandlesCorrectly(ServiceStartType startType)
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.StartType = startType;

            // Assert
            Assert.Equal(startType, info.StartType);
        }

        [Fact]
        public void StartType_WithDefaultValue_IsZero()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act & Assert
            Assert.Equal(default(ServiceStartType), info.StartType);
            Assert.Equal((ServiceStartType)0, info.StartType);
        }

        [Fact]
        public void ObjectName_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var objectName = @"NT AUTHORITY\LocalService";

            // Act
            info.ObjectName = objectName;

            // Assert
            Assert.Equal(objectName, info.ObjectName);
        }

        [Fact]
        public void ObjectName_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ObjectName = null!;

            // Assert
            Assert.Null(info.ObjectName);
        }

        [Fact]
        public void ObjectName_WithEmptyString_SetsToEmptyString()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ObjectName = string.Empty;

            // Assert
            Assert.Equal(string.Empty, info.ObjectName);
        }

        [Fact]
        public void ObjectName_WithUnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var unicodeObjectName = "测试对象名称";

            // Act
            info.ObjectName = unicodeObjectName;

            // Assert
            Assert.Equal(unicodeObjectName, info.ObjectName);
        }

        [Fact]
        public void Dependencies_CanBeSetAndRetrieved()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var dependencies = new[] { "Service1", "Service2", "Service3" };

            // Act
            info.Dependencies = dependencies;

            // Assert
            Assert.Equal(dependencies, info.Dependencies);
            Assert.Equal(3, info.Dependencies.Length);
        }

        [Fact]
        public void Dependencies_WithNull_SetsToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.Dependencies = null!;

            // Assert
            Assert.Null(info.Dependencies);
        }

        [Fact]
        public void Dependencies_WithEmptyArray_SetsToEmptyArray()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var emptyDependencies = Array.Empty<string>();

            // Act
            info.Dependencies = emptyDependencies;

            // Assert
            Assert.Equal(emptyDependencies, info.Dependencies);
            Assert.Empty(info.Dependencies);
        }

        [Fact]
        public void Dependencies_WithSingleDependency_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var singleDependency = new[] { "SingleService" };

            // Act
            info.Dependencies = singleDependency;

            // Assert
            Assert.Equal(singleDependency, info.Dependencies);
            Assert.Single(info.Dependencies);
            Assert.Equal("SingleService", info.Dependencies[0]);
        }

        [Fact]
        public void Dependencies_WithManyDependencies_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var manyDependencies = Enumerable.Range(1, 100).Select(i => $"Service{i}").ToArray();

            // Act
            info.Dependencies = manyDependencies;

            // Assert
            Assert.Equal(manyDependencies, info.Dependencies);
            Assert.Equal(100, info.Dependencies.Length);
        }

        [Fact]
        public void Dependencies_WithNullElements_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var dependenciesWithNulls = new[] { "Service1", null, "Service3", null };

            // Act
            info.Dependencies = dependenciesWithNulls!;

            // Assert
            Assert.Equal(dependenciesWithNulls, info.Dependencies);
            Assert.Equal(4, info.Dependencies.Length);
            Assert.Null(info.Dependencies[1]);
            Assert.Null(info.Dependencies[3]);
        }

        [Fact]
        public void Dependencies_WithEmptyStringElements_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var dependenciesWithEmptyStrings = new[] { "Service1", string.Empty, "Service3", "   " };

            // Act
            info.Dependencies = dependenciesWithEmptyStrings;

            // Assert
            Assert.Equal(dependenciesWithEmptyStrings, info.Dependencies);
            Assert.Equal(4, info.Dependencies.Length);
            Assert.Equal(string.Empty, info.Dependencies[1]);
            Assert.Equal("   ", info.Dependencies[3]);
        }

        [Fact]
        public void Dependencies_WithUnicodeElements_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var unicodeDependencies = new[] { "服务1", "Service2", "服务3" };

            // Act
            info.Dependencies = unicodeDependencies;

            // Assert
            Assert.Equal(unicodeDependencies, info.Dependencies);
            Assert.Equal(3, info.Dependencies.Length);
        }

        [Fact]
        public void Dependencies_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var info = new ServiceRegistryInfo();
            var specialDependencies = new[] { "Service-1", "Service_2", "Service.3", "Service+4" };

            // Act
            info.Dependencies = specialDependencies;

            // Assert
            Assert.Equal(specialDependencies, info.Dependencies);
            Assert.Equal(4, info.Dependencies.Length);
        }

        [Fact]
        public void AllProperties_CanBeSetIndependently()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ServiceName = "TestService";
            info.DisplayName = "Test Service Display";
            info.Description = "Test service description";
            info.ImagePath = @"C:\Test\TestService.exe";
            info.StartType = ServiceStartType.Manual;
            info.ObjectName = @"NT AUTHORITY\NetworkService";
            info.Dependencies = new[] { "DepService1", "DepService2" };

            // Assert
            Assert.Equal("TestService", info.ServiceName);
            Assert.Equal("Test Service Display", info.DisplayName);
            Assert.Equal("Test service description", info.Description);
            Assert.Equal(@"C:\Test\TestService.exe", info.ImagePath);
            Assert.Equal(ServiceStartType.Manual, info.StartType);
            Assert.Equal(@"NT AUTHORITY\NetworkService", info.ObjectName);
            Assert.Equal(new[] { "DepService1", "DepService2" }, info.Dependencies);
        }

        [Fact]
        public void AllProperties_CanBeSetToNull()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act
            info.ServiceName = null!;
            info.DisplayName = null!;
            info.Description = null!;
            info.ImagePath = null!;
            info.ObjectName = null!;
            info.Dependencies = null!;

            // Assert
            Assert.Null(info.ServiceName);
            Assert.Null(info.DisplayName);
            Assert.Null(info.Description);
            Assert.Null(info.ImagePath);
            Assert.Null(info.ObjectName);
            Assert.Null(info.Dependencies);
        }

        [Fact]
        public void MultipleInstances_AreIndependent()
        {
            // Arrange
            var info1 = new ServiceRegistryInfo();
            var info2 = new ServiceRegistryInfo();

            // Act
            info1.ServiceName = "Service1";
            info1.StartType = ServiceStartType.Automatic;
            info1.Dependencies = new[] { "Dep1" };

            info2.ServiceName = "Service2";
            info2.StartType = ServiceStartType.Manual;
            info2.Dependencies = new[] { "Dep2", "Dep3" };

            // Assert
            Assert.Equal("Service1", info1.ServiceName);
            Assert.Equal(ServiceStartType.Automatic, info1.StartType);
            Assert.Equal(new[] { "Dep1" }, info1.Dependencies);

            Assert.Equal("Service2", info2.ServiceName);
            Assert.Equal(ServiceStartType.Manual, info2.StartType);
            Assert.Equal(new[] { "Dep2", "Dep3" }, info2.Dependencies);
        }

        [Fact]
        public void Properties_CanBeModifiedMultipleTimes()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act & Assert - First set
            info.ServiceName = "FirstService";
            info.StartType = ServiceStartType.Automatic;
            Assert.Equal("FirstService", info.ServiceName);
            Assert.Equal(ServiceStartType.Automatic, info.StartType);

            // Act & Assert - Second set
            info.ServiceName = "SecondService";
            info.StartType = ServiceStartType.Manual;
            Assert.Equal("SecondService", info.ServiceName);
            Assert.Equal(ServiceStartType.Manual, info.StartType);

            // Act & Assert - Third set
            info.ServiceName = "ThirdService";
            info.StartType = ServiceStartType.Disabled;
            Assert.Equal("ThirdService", info.ServiceName);
            Assert.Equal(ServiceStartType.Disabled, info.StartType);
        }

        [Fact]
        public void Dependencies_CanBeModifiedMultipleTimes()
        {
            // Arrange
            var info = new ServiceRegistryInfo();

            // Act & Assert - First set
            info.Dependencies = new[] { "Dep1" };
            Assert.Equal(new[] { "Dep1" }, info.Dependencies);

            // Act & Assert - Second set
            info.Dependencies = new[] { "Dep2", "Dep3" };
            Assert.Equal(new[] { "Dep2", "Dep3" }, info.Dependencies);

            // Act & Assert - Third set
            info.Dependencies = Array.Empty<string>();
            Assert.Empty(info.Dependencies);

            // Act & Assert - Fourth set
            info.Dependencies = new[] { "Dep4", "Dep5", "Dep6", "Dep7" };
            Assert.Equal(new[] { "Dep4", "Dep5", "Dep6", "Dep7" }, info.Dependencies);
        }

        [Fact]
        public void ServiceRegistryInfo_HasExpectedProperties()
        {
            // Arrange
            var type = typeof(ServiceRegistryInfo);

            // Act & Assert
            Assert.NotNull(type.GetProperty("ServiceName"));
            Assert.NotNull(type.GetProperty("DisplayName"));
            Assert.NotNull(type.GetProperty("Description"));
            Assert.NotNull(type.GetProperty("ImagePath"));
            Assert.NotNull(type.GetProperty("StartType"));
            Assert.NotNull(type.GetProperty("ObjectName"));
            Assert.NotNull(type.GetProperty("Dependencies"));
        }

        [Fact]
        public void ServiceRegistryInfo_PropertiesAreSettable()
        {
            // Arrange
            var type = typeof(ServiceRegistryInfo);

            // Act & Assert
            Assert.True(type.GetProperty("ServiceName")?.CanWrite);
            Assert.True(type.GetProperty("DisplayName")?.CanWrite);
            Assert.True(type.GetProperty("Description")?.CanWrite);
            Assert.True(type.GetProperty("ImagePath")?.CanWrite);
            Assert.True(type.GetProperty("StartType")?.CanWrite);
            Assert.True(type.GetProperty("ObjectName")?.CanWrite);
            Assert.True(type.GetProperty("Dependencies")?.CanWrite);
        }

        [Fact]
        public void ServiceRegistryInfo_PropertiesAreGettable()
        {
            // Arrange
            var type = typeof(ServiceRegistryInfo);

            // Act & Assert
            Assert.True(type.GetProperty("ServiceName")?.CanRead);
            Assert.True(type.GetProperty("DisplayName")?.CanRead);
            Assert.True(type.GetProperty("Description")?.CanRead);
            Assert.True(type.GetProperty("ImagePath")?.CanRead);
            Assert.True(type.GetProperty("StartType")?.CanRead);
            Assert.True(type.GetProperty("ObjectName")?.CanRead);
            Assert.True(type.GetProperty("Dependencies")?.CanRead);
        }
    }
}
