using LivingDocGen.Core.Validators;
using LivingDocGen.Core.Exceptions;
using Xunit;

namespace LivingDocGen.Core.Tests.Validators;

public class FileValidatorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testFilePath;

    public FileValidatorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "LivingDocGenTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "test.feature");
        File.WriteAllText(_testFilePath, "Feature: Test");
    }

    [Fact]
    public void ValidateFileExists_WithValidFile_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => FileValidator.ValidateFileExists(_testFilePath));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateFileExists_WithNullPath_ThrowsValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateFileExists(null));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateFileExists_WithEmptyPath_ThrowsValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateFileExists(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateFileExists_WithNonExistentFile_ThrowsValidationException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.feature");

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateFileExists(nonExistentPath));
        Assert.Contains("File not found", exception.Message);
    }

    [Fact]
    public void ValidateFileExists_WithCorrectExtension_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            FileValidator.ValidateFileExists(_testFilePath, ".feature"));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateFileExists_WithIncorrectExtension_ThrowsValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateFileExists(_testFilePath, ".json"));
        Assert.Contains("Invalid file extension", exception.Message);
        Assert.Contains("Expected: .json", exception.Message);
        Assert.Contains("Actual: .feature", exception.Message);
    }

    [Fact]
    public void ValidateFileExists_WithInvalidPath_ThrowsValidationException()
    {
        // Arrange
        var invalidPath = new string(Path.GetInvalidPathChars().Take(1).ToArray());

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateFileExists(invalidPath));
        Assert.Contains("Invalid file path", exception.Message);
    }

    [Fact]
    public void ValidateDirectoryExists_WithValidDirectory_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            FileValidator.ValidateDirectoryExists(_testDirectory));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDirectoryExists_WithNullPath_ThrowsValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateDirectoryExists(null));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateDirectoryExists_WithNonExistentDirectory_ThrowsValidationException()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => 
            FileValidator.ValidateDirectoryExists(nonExistentDir));
        Assert.Contains("Directory not found", exception.Message);
    }

    [Fact]
    public void GetFiles_WithValidDirectory_ReturnsFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test1.feature"), "Feature: Test 1");
        File.WriteAllText(Path.Combine(_testDirectory, "test2.feature"), "Feature: Test 2");

        // Act
        var files = FileValidator.GetFiles(_testDirectory, "*.feature");

        // Assert
        Assert.NotNull(files);
        Assert.Equal(3, files.Length); // Including the file from constructor
    }

    [Fact]
    public void GetFiles_WithSearchPattern_ReturnsMatchingFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testDirectory, "test.json"), "{}");
        File.WriteAllText(Path.Combine(_testDirectory, "test.xml"), "<root/>");

        // Act
        var featureFiles = FileValidator.GetFiles(_testDirectory, "*.feature");
        var jsonFiles = FileValidator.GetFiles(_testDirectory, "*.json");

        // Assert
        Assert.Single(jsonFiles);
        Assert.NotEmpty(featureFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
