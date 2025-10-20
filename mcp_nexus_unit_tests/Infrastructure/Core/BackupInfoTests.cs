using Xunit;
using mcp_nexus.Infrastructure.Core;

namespace mcp_nexus_unit_tests.Infrastructure.Core
{
    /// <summary>
    /// Tests for BackupInfo model.
    /// </summary>
    public class BackupInfoTests
    {
        [Fact]
        public void BackupInfo_DefaultConstructor_InitializesDefaults()
        {
            // Act
            var backupInfo = new BackupInfo();

            // Assert
            Assert.Equal(string.Empty, backupInfo.Path);
            Assert.Equal(default(DateTime), backupInfo.Created);
            Assert.Equal(0, backupInfo.Size);
            Assert.Equal(string.Empty, backupInfo.Name);
            Assert.False(backupInfo.IsValid);
            Assert.NotNull(backupInfo.Files);
            Assert.Empty(backupInfo.Files);
            Assert.Equal(string.Empty, backupInfo.ErrorMessage);
            Assert.Equal(default(DateTime), backupInfo.CreationTime);
            Assert.Equal(0, backupInfo.SizeBytes);
        }

        [Fact]
        public void BackupInfo_Path_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.Path = "C:\\Backups\\backup1";

            // Assert
            Assert.Equal("C:\\Backups\\backup1", backupInfo.Path);
        }

        [Fact]
        public void BackupInfo_Created_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();
            var createdDate = new DateTime(2025, 1, 15, 10, 30, 0);

            // Act
            backupInfo.Created = createdDate;

            // Assert
            Assert.Equal(createdDate, backupInfo.Created);
        }

        [Fact]
        public void BackupInfo_Size_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.Size = 1024 * 1024 * 100; // 100 MB

            // Assert
            Assert.Equal(1024 * 1024 * 100, backupInfo.Size);
        }

        [Fact]
        public void BackupInfo_Name_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.Name = "MyBackup";

            // Assert
            Assert.Equal("MyBackup", backupInfo.Name);
        }

        [Fact]
        public void BackupInfo_IsValid_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.IsValid = true;

            // Assert
            Assert.True(backupInfo.IsValid);
        }

        [Fact]
        public void BackupInfo_Files_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();
            var files = new[] { "file1.txt", "file2.dat", "file3.log" };

            // Act
            backupInfo.Files = files;

            // Assert
            Assert.Equal(files, backupInfo.Files);
            Assert.Equal(3, backupInfo.Files.Length);
        }

        [Fact]
        public void BackupInfo_ErrorMessage_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.ErrorMessage = "Backup failed due to insufficient space";

            // Assert
            Assert.Equal("Backup failed due to insufficient space", backupInfo.ErrorMessage);
        }

        [Fact]
        public void BackupInfo_CreationTime_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();
            var creationTime = new DateTime(2025, 2, 20, 14, 45, 30);

            // Act
            backupInfo.CreationTime = creationTime;

            // Assert
            Assert.Equal(creationTime, backupInfo.CreationTime);
        }

        [Fact]
        public void BackupInfo_SizeBytes_CanBeSet()
        {
            // Arrange
            var backupInfo = new BackupInfo();

            // Act
            backupInfo.SizeBytes = 5000000; // 5 MB

            // Assert
            Assert.Equal(5000000, backupInfo.SizeBytes);
        }

        [Fact]
        public void BackupInfo_AllProperties_CanBeSetTogether()
        {
            // Arrange & Act
            var backupInfo = new BackupInfo
            {
                Path = "D:\\Backups\\backup2",
                Created = new DateTime(2025, 3, 10, 8, 0, 0),
                Size = 2048000,
                Name = "DailyBackup",
                IsValid = true,
                Files = new[] { "data1.db", "data2.db" },
                ErrorMessage = "",
                CreationTime = new DateTime(2025, 3, 10, 8, 0, 0),
                SizeBytes = 2048000
            };

            // Assert
            Assert.Equal("D:\\Backups\\backup2", backupInfo.Path);
            Assert.Equal(new DateTime(2025, 3, 10, 8, 0, 0), backupInfo.Created);
            Assert.Equal(2048000, backupInfo.Size);
            Assert.Equal("DailyBackup", backupInfo.Name);
            Assert.True(backupInfo.IsValid);
            Assert.Equal(2, backupInfo.Files.Length);
            Assert.Equal("", backupInfo.ErrorMessage);
            Assert.Equal(new DateTime(2025, 3, 10, 8, 0, 0), backupInfo.CreationTime);
            Assert.Equal(2048000, backupInfo.SizeBytes);
        }

        [Fact]
        public void BackupInfo_InvalidBackup_HasErrorMessage()
        {
            // Arrange & Act
            var backupInfo = new BackupInfo
            {
                Path = "E:\\Backups\\failed_backup",
                IsValid = false,
                ErrorMessage = "Checksum verification failed"
            };

            // Assert
            Assert.False(backupInfo.IsValid);
            Assert.Equal("Checksum verification failed", backupInfo.ErrorMessage);
        }

        [Fact]
        public void BackupInfo_EmptyFilesList_IsValid()
        {
            // Arrange & Act
            var backupInfo = new BackupInfo
            {
                Files = []
            };

            // Assert
            Assert.NotNull(backupInfo.Files);
            Assert.Empty(backupInfo.Files);
        }

        [Fact]
        public void BackupInfo_LargeFilesList_CanBeStored()
        {
            // Arrange
            var backupInfo = new BackupInfo();
            var manyFiles = Enumerable.Range(1, 1000).Select(i => $"file{i}.dat").ToArray();

            // Act
            backupInfo.Files = manyFiles;

            // Assert
            Assert.Equal(1000, backupInfo.Files.Length);
            Assert.Equal("file1.dat", backupInfo.Files[0]);
            Assert.Equal("file1000.dat", backupInfo.Files[999]);
        }
    }
}

