using LivingDocGen.Parser.Models;
using Xunit;

namespace LivingDocGen.Parser.Tests.Models;

public class UniversalModelsTests
{
    [Fact]
    public void UniversalFeature_InitializesWithDefaults()
    {
        // Act
        var feature = new UniversalFeature();

        // Assert
        Assert.NotNull(feature.Scenarios);
        Assert.NotNull(feature.Tags);
        Assert.Empty(feature.Scenarios);
        Assert.Empty(feature.Tags);
    }

    [Fact]
    public void UniversalScenario_InitializesWithDefaults()
    {
        // Act
        var scenario = new UniversalScenario();

        // Assert
        Assert.NotNull(scenario.Steps);
        Assert.NotNull(scenario.Tags);
        Assert.Empty(scenario.Steps);
        Assert.Empty(scenario.Tags);
        Assert.Equal(ScenarioType.Scenario, scenario.Type);
    }

    [Fact]
    public void UniversalStep_InitializesWithDefaults()
    {
        // Act
        var step = new UniversalStep();

        // Assert
        Assert.Equal(string.Empty, step.Keyword);
        Assert.Null(step.DataTable);
        Assert.Null(step.DocString);
    }

    [Fact]
    public void ScenarioType_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)ScenarioType.Scenario);
        Assert.Equal(1, (int)ScenarioType.ScenarioOutline);
        Assert.Equal(2, (int)ScenarioType.ScenarioTemplate);
    }

    [Fact]
    public void BDDFramework_HasAllFrameworks()
    {
        // Act & Assert
        Assert.Equal(0, (int)BDDFramework.Unknown);
        Assert.Equal(1, (int)BDDFramework.Cucumber);
        Assert.Equal(2, (int)BDDFramework.SpecFlow);
        Assert.Equal(3, (int)BDDFramework.ReqnRoll);
        Assert.Equal(4, (int)BDDFramework.JBehave);
    }

    [Fact]
    public void DataTable_CanStoreRowsAndHeaders()
    {
        // Arrange
        var dataTable = new UniversalDataTable
        {
            Rows = new List<List<string>>
            {
                new List<string> { "Name", "Age" }, // Header row
                new List<string> { "John", "30" },
                new List<string> { "Jane", "25" }
            }
        };

        // Assert
        Assert.Equal(3, dataTable.Rows.Count);
        Assert.Equal("Name", dataTable.Rows[0][0]);
        Assert.Equal("John", dataTable.Rows[1][0]);
        Assert.Equal("25", dataTable.Rows[2][1]);
    }

    [Fact]
    public void Examples_CanStoreHeadersAndRows()
    {
        // Arrange
        var examples = new UniversalExample
        {
            Name = "Login Examples",
            Headers = new List<string> { "username", "password", "result" },
            Rows = new List<List<string>>
            {
                new List<string> { "admin", "pass123", "success" },
                new List<string> { "user", "wrong", "failure" }
            }
        };

        // Assert
        Assert.Equal("Login Examples", examples.Name);
        Assert.Equal(3, examples.Headers.Count);
        Assert.Equal(2, examples.Rows.Count);
    }

    [Fact]
    public void UniversalScenario_CanHaveMultipleExamples()
    {
        // Arrange
        var scenario = new UniversalScenario
        {
            Type = ScenarioType.ScenarioOutline,
            Examples = new List<UniversalExample>
            {
                new UniversalExample { Name = "Valid Logins" },
                new UniversalExample { Name = "Invalid Logins" }
            }
        };

        // Assert
        Assert.Equal(2, scenario.Examples.Count);
        Assert.Equal("Valid Logins", scenario.Examples[0].Name);
        Assert.Equal("Invalid Logins", scenario.Examples[1].Name);
    }

    [Fact]
    public void UniversalFeature_CanHaveBackgroundAndScenarios()
    {
        // Arrange
        var feature = new UniversalFeature
        {
            Name = "Test Feature",
            Background = new UniversalBackground
            {
                Steps = new List<UniversalStep>
                {
                    new UniversalStep { Keyword = "Given", Text = "background step" }
                }
            },
            Scenarios = new List<UniversalScenario>
            {
                new UniversalScenario { Name = "Scenario 1" },
                new UniversalScenario { Name = "Scenario 2" }
            }
        };

        // Assert
        Assert.NotNull(feature.Background);
        Assert.Single(feature.Background.Steps);
        Assert.Equal(2, feature.Scenarios.Count);
    }
}
