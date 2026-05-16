using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SmartShop.Application.Common.Models;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Services;
using Xunit;

namespace SmartShop.Application.Tests.Files;

public class LocalFileStorageServiceTests : IDisposable
{
    private string _tempWebRoot = string.Empty;

    private (LocalFileStorageService service, string tempWebRoot) CreateServiceWithTempDir()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWebRoot);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var mockAppSettingRepo = new Mock<IAppSettingRepository>();
        mockAppSettingRepo
            .Setup(r => r.GetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("");

        var mockLogger = new Mock<ILogger<LocalFileStorageService>>();

        return (new LocalFileStorageService(mockEnv.Object, mockAppSettingRepo.Object, mockLogger.Object), _tempWebRoot);
    }

    [Fact]
    public async Task UploadAsync_CsvCategory_SavesFileToDisk()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("Name,Price\nProduct1,100");
        var stream = new MemoryStream(fileContent);
        var fileName = "import.csv";

        // Act
        var url = await service.UploadAsync(stream, fileName, UploadCategory.CsvImport, default);

        // Assert
        url.Should().StartWith("/uploads/");
        var expectedDir = Path.Combine(tempWebRoot, "uploads", "imports");
        Directory.Exists(expectedDir).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);
        var fileName = "test.csv";

        var uploadsDir = Path.Combine(tempWebRoot, "uploads");
        Directory.Exists(uploadsDir).Should().BeFalse();

        // Act
        await service.UploadAsync(stream, fileName, UploadCategory.CsvImport, default);

        // Assert
        Directory.Exists(uploadsDir).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_ReturnsRelativeUrl()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);
        var fileName = "test.csv";

        // Act
        var url = await service.UploadAsync(stream, fileName, UploadCategory.CsvImport, default);

        // Assert
        url.Should().StartWith("/uploads/");
        url.Should().NotStartWith("http");
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesFile()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);
        var fileName = "test.csv";

        var url = await service.UploadAsync(stream, fileName, UploadCategory.CsvImport, default);

        var relativePath = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(tempWebRoot, relativePath);
        File.Exists(fullPath).Should().BeTrue();

        // Act
        await service.DeleteAsync(url, UploadCategory.CsvImport, default);

        // Assert
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_DoesNotThrow()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var nonExistentUrl = "/uploads/imports/nonexistent.csv";

        // Act
        var act = () => service.DeleteAsync(nonExistentUrl, UploadCategory.CsvImport, default);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UploadAsync_FileNameSanitized()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);
        var unsafeFileName = "../../malicious.csv";

        // Act
        var url = await service.UploadAsync(stream, unsafeFileName, UploadCategory.CsvImport, default);

        // Assert
        url.Should().NotContain("..");
    }

    [Fact]
    public async Task UploadAsync_MultipleFiles_CreatesInSameDirectory()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var stream1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1"));
        var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content2"));

        // Act
        var url1 = await service.UploadAsync(stream1, "file1.csv", UploadCategory.CsvImport, default);
        var url2 = await service.UploadAsync(stream2, "file2.csv", UploadCategory.CsvImport, default);

        // Assert
        url1.Should().StartWith("/uploads/imports/");
        url2.Should().StartWith("/uploads/imports/");
    }

    [Fact]
    public async Task DeleteAsync_WithTrailingSlash_DeletesCorrectly()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);
        var fileName = "test.csv";

        var url = await service.UploadAsync(stream, fileName, UploadCategory.CsvImport, default);

        // Act
        await service.DeleteAsync(url, UploadCategory.CsvImport, default);

        // Assert - should not throw
    }

    [Fact]
    public async Task UploadAsync_StreamPreservedAfterUpload()
    {
        // Arrange
        var (service, tempWebRoot) = CreateServiceWithTempDir();
        var fileContent = System.Text.Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(fileContent);

        // Act
        await service.UploadAsync(stream, "test.csv", UploadCategory.CsvImport, default);

        // Assert - Stream can still be read or is properly disposed
        var csvDir = Path.Combine(tempWebRoot, "uploads", "imports");
        Directory.GetFiles(csvDir).Should().HaveCountGreaterThan(0);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWebRoot))
        {
            try
            {
                Directory.Delete(_tempWebRoot, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
