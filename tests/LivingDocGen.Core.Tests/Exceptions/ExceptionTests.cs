using LivingDocGen.Core.Exceptions;
using Xunit;

namespace LivingDocGen.Core.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void BDDException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test BDD exception";

        // Act
        var exception = new BDDException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void BDDException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test BDD exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new BDDException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ConfigurationException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test configuration exception";

        // Act
        var exception = new ConfigurationException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ConfigurationException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test configuration exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new ConfigurationException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ParseException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test parse exception";

        // Act
        var exception = new ParseException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ParseException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test parse exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new ParseException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void ValidationException_WithMessage_CreatesException()
    {
        // Arrange
        var message = "Test validation exception";

        // Act
        var exception = new ValidationException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ValidationException_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Test validation exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void AllExceptions_InheritFromBDDException()
    {
        // Assert
        Assert.True(typeof(ConfigurationException).IsSubclassOf(typeof(BDDException)));
        Assert.True(typeof(ParseException).IsSubclassOf(typeof(BDDException)));
        Assert.True(typeof(ValidationException).IsSubclassOf(typeof(BDDException)));
    }

    [Fact]
    public void AllExceptions_InheritFromException()
    {
        // Assert
        Assert.True(typeof(BDDException).IsSubclassOf(typeof(Exception)));
    }
}
