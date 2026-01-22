using LivingDocGen.Parser.Parsers;
using LivingDocGen.Parser.Models;
using Xunit;

namespace LivingDocGen.Parser.Tests;

public class GherkinParserTests
{
    [Fact]
    public void Parse_ValidFeatureFile_ReturnsUniversalFeature()
    {
        // Arrange
        var parser = new GherkinParser(BDDFramework.Cucumber);
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "features", "login.feature");
        var fullPath = Path.GetFullPath(samplePath);

        // Act
        var feature = parser.Parse(fullPath);

        // Assert
        Assert.NotNull(feature);
        Assert.Equal("User Login", feature.Name);
        Assert.NotEmpty(feature.Scenarios);
        Assert.Contains("@authentication", feature.Tags);
    }

    [Fact]
    public void Parse_FeatureWithBackground_ParsesCorrectly()
    {
        // Arrange
        var parser = new GherkinParser(BDDFramework.Cucumber);
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "features", "login.feature");
        var fullPath = Path.GetFullPath(samplePath);

        // Act
        var feature = parser.Parse(fullPath);

        // Assert
        Assert.NotNull(feature.Background);
        Assert.NotEmpty(feature.Background.Steps);
        Assert.Contains(feature.Background.Steps, s => s.Text.Contains("application is running"));
    }

    [Fact]
    public void Parse_ScenarioOutline_HasExamples()
    {
        // Arrange
        var parser = new GherkinParser(BDDFramework.Cucumber);
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "features", "login.feature");
        var fullPath = Path.GetFullPath(samplePath);

        // Act
        var feature = parser.Parse(fullPath);
        var outlineScenario = feature.Scenarios.FirstOrDefault(s => s.Type == ScenarioType.ScenarioOutline);

        // Assert
        Assert.NotNull(outlineScenario);
        Assert.NotNull(outlineScenario.Examples);
        Assert.NotEmpty(outlineScenario.Examples);
        Assert.Contains("username", outlineScenario.Examples[0].Headers);
    }

    [Fact]
    public void Parse_ScenarioWithDataTable_ParsesTable()
    {
        // Arrange
        var parser = new GherkinParser(BDDFramework.Cucumber);
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "features", "shopping_cart.feature");
        var fullPath = Path.GetFullPath(samplePath);

        // Act
        var feature = parser.Parse(fullPath);
        var scenario = feature.Scenarios.FirstOrDefault(s => s.Name.Contains("Remove product from cart"));

        // Assert
        Assert.NotNull(scenario);
        var stepWithTable = scenario.Steps.FirstOrDefault(s => s.DataTable != null);
        Assert.NotNull(stepWithTable);
        Assert.NotNull(stepWithTable.DataTable);
        Assert.True(stepWithTable.DataTable.Rows.Count > 1);
    }

    [Fact]
    public void CanParse_ValidFeatureFile_ReturnsTrue()
    {
        // Arrange
        var parser = new GherkinParser();
        var samplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "features", "login.feature");
        var fullPath = Path.GetFullPath(samplePath);

        // Act
        var result = parser.CanParse(fullPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanParse_NonFeatureFile_ReturnsFalse()
    {
        // Arrange
        var parser = new GherkinParser();

        // Act
        var result = parser.CanParse("test.txt");

        // Assert
        Assert.False(result);
    }
}
