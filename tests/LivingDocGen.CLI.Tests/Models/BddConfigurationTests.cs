using LivingDocGen.CLI.Models;
using System.Text.Json;
using Xunit;

namespace LivingDocGen.CLI.Tests.Models;

public class BddConfigurationTests
{
    [Fact]
    public void BddConfiguration_Deserializes_ValidJson()
    {
        // Arrange
        var json = @"{
            ""enabled"": true,
            ""autoGenerate"": ""afterTests"",
            ""paths"": {
                ""features"": ""./Features"",
                ""testResults"": ""./TestResults"",
                ""output"": ""./living-doc.html""
            },
            ""documentation"": {
                ""title"": ""My BDD Documentation"",
                ""theme"": ""purple"",
                ""primaryColor"": ""#6366f1""
            }
        }";

        // Act
        var config = JsonSerializer.Deserialize<BddConfiguration>(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.Equal("afterTests", config.AutoGenerate);
        Assert.NotNull(config.Paths);
        Assert.Equal("./Features", config.Paths.Features);
        Assert.Equal("./TestResults", config.Paths.TestResults);
        Assert.Equal("./living-doc.html", config.Paths.Output);
        Assert.NotNull(config.Documentation);
        Assert.Equal("My BDD Documentation", config.Documentation.Title);
        Assert.Equal("purple", config.Documentation.Theme);
        Assert.Equal("#6366f1", config.Documentation.PrimaryColor);
    }

    [Fact]
    public void BddConfiguration_HasDefaultValues()
    {
        // Act
        var config = new BddConfiguration();

        // Assert
        Assert.True(config.Enabled);
        Assert.Null(config.AutoGenerate);
        Assert.Null(config.Paths);
        Assert.Null(config.Documentation);
        Assert.Null(config.Advanced);
    }

    [Fact]
    public void BddConfiguration_Serializes_ToJson()
    {
        // Arrange
        var config = new BddConfiguration
        {
            Enabled = true,
            AutoGenerate = "afterTests",
            Paths = new PathsConfiguration
            {
                Features = "./Features",
                TestResults = "./TestResults",
                Output = "./living-doc.html"
            },
            Documentation = new DocumentationConfiguration
            {
                Title = "Test Documentation",
                Theme = "blue"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"enabled\": true", json);
        Assert.Contains("\"autoGenerate\": \"afterTests\"", json);
        Assert.Contains("\"features\": \"./Features\"", json);
        Assert.Contains("\"title\": \"Test Documentation\"", json);
        Assert.Contains("\"theme\": \"blue\"", json);
    }

    [Fact]
    public void PathsConfiguration_AllowsNullValues()
    {
        // Act
        var paths = new PathsConfiguration();

        // Assert
        Assert.Null(paths.Features);
        Assert.Null(paths.TestResults);
        Assert.Null(paths.Output);
    }

    [Fact]
    public void DocumentationConfiguration_AllowsNullValues()
    {
        // Act
        var documentation = new DocumentationConfiguration();

        // Assert
        Assert.Null(documentation.Title);
        Assert.Null(documentation.Theme);
        Assert.Null(documentation.PrimaryColor);
    }

    [Fact]
    public void BddConfiguration_Deserializes_MinimalJson()
    {
        // Arrange
        var json = @"{}";

        // Act
        var config = JsonSerializer.Deserialize<BddConfiguration>(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled); // Property initializer sets default to true
    }

    [Fact]
    public void BddConfiguration_Deserializes_PartialJson()
    {
        // Arrange
        var json = @"{
            ""enabled"": true,
            ""paths"": {
                ""features"": ""./Features""
            }
        }";

        // Act
        var config = JsonSerializer.Deserialize<BddConfiguration>(json);

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.NotNull(config.Paths);
        Assert.Equal("./Features", config.Paths.Features);
        Assert.Null(config.Paths.TestResults);
        Assert.Null(config.Paths.Output);
        Assert.Null(config.Documentation);
    }
}
