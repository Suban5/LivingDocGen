using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models;

public class EnrichedModelsTests
{
    [Fact]
    public void EnrichedFeature_CalculatesPassRateCorrectly()
    {
        // Arrange
        var feature = new EnrichedFeature
        {
            PassedCount = 8,
            FailedCount = 2,
            Scenarios = new List<EnrichedScenario>
            {
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Passed },
                new EnrichedScenario { Status = ExecutionStatus.Failed },
                new EnrichedScenario { Status = ExecutionStatus.Failed }
            }
        };

        // Act
        var passRate = feature.PassRate;

        // Assert
        Assert.Equal(80.0, passRate);
    }

    [Fact]
    public void EnrichedFeature_WithNoScenarios_HasZeroPassRate()
    {
        // Arrange
        var feature = new EnrichedFeature();

        // Act
        var passRate = feature.PassRate;

        // Assert
        Assert.Equal(0, passRate);
    }

    [Fact]
    public void DocumentStatistics_CalculatesExecutedScenariosCorrectly()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            PassedScenarios = 10,
            FailedScenarios = 2,
            SkippedScenarios = 3,
            UntestedScenarios = 5
        };

        // Act
        var executedScenarios = stats.ExecutedScenarios;

        // Assert
        Assert.Equal(15, executedScenarios); // 10 + 2 + 3
    }

    [Fact]
    public void DocumentStatistics_CalculatesPassRateCorrectly()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            PassedScenarios = 10,
            FailedScenarios = 2,
            SkippedScenarios = 3,
            UntestedScenarios = 5
        };

        // Act
        var passRate = stats.PassRate;

        // Assert
        Assert.Equal(66.66666666666667, passRate, 5); // 10 / 15 * 100
    }

    [Fact]
    public void DocumentStatistics_CalculatesFailRateCorrectly()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            PassedScenarios = 10,
            FailedScenarios = 2,
            SkippedScenarios = 3
        };

        // Act
        var failRate = stats.FailRate;

        // Assert
        Assert.Equal(13.333333333333334, failRate, 5); // 2 / 15 * 100
    }

    [Fact]
    public void DocumentStatistics_CalculatesSkipRateCorrectly()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            PassedScenarios = 10,
            FailedScenarios = 2,
            SkippedScenarios = 3
        };

        // Act
        var skipRate = stats.SkipRate;

        // Assert
        Assert.Equal(20.0, skipRate, 5); // 3 / 15 * 100
    }

    [Fact]
    public void DocumentStatistics_CalculatesCoverageCorrectly()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            TotalScenarios = 20,
            PassedScenarios = 10,
            FailedScenarios = 2,
            SkippedScenarios = 3,
            UntestedScenarios = 5
        };

        // Act
        var coverage = stats.Coverage;

        // Assert
        Assert.Equal(75.0, coverage); // 15 / 20 * 100
    }

    [Fact]
    public void DocumentStatistics_WithNoExecutedScenarios_HasZeroRates()
    {
        // Arrange
        var stats = new DocumentStatistics
        {
            UntestedScenarios = 10
        };

        // Act & Assert
        Assert.Equal(0, stats.PassRate);
        Assert.Equal(0, stats.FailRate);
        Assert.Equal(0, stats.SkipRate);
    }

    [Fact]
    public void DocumentStatistics_WithNoTotalScenarios_HasZeroCoverage()
    {
        // Arrange
        var stats = new DocumentStatistics();

        // Act
        var coverage = stats.Coverage;

        // Assert
        Assert.Equal(0, coverage);
    }

    [Fact]
    public void EnrichedScenario_InitializesWithDefaults()
    {
        // Act
        var scenario = new EnrichedScenario();

        // Assert
        Assert.NotNull(scenario.Scenario);
        Assert.NotNull(scenario.Steps);
        Assert.NotNull(scenario.ExampleResults);
        Assert.Empty(scenario.Steps);
        Assert.Empty(scenario.ExampleResults);
    }

    [Fact]
    public void EnrichedStep_InitializesWithDefaults()
    {
        // Act
        var step = new EnrichedStep();

        // Assert
        Assert.NotNull(step.Step);
        Assert.NotNull(step.Screenshots);
        Assert.Empty(step.Screenshots);
    }

    [Fact]
    public void LivingDocumentation_InitializesWithDefaults()
    {
        // Act
        var doc = new LivingDocumentation();

        // Assert
        Assert.NotNull(doc.Features);
        Assert.NotNull(doc.Statistics);
        Assert.NotNull(doc.TagDistribution);
        Assert.NotNull(doc.FrameworkDistribution);
        Assert.Equal("LivingDoc Living Documentation", doc.Title);
        Assert.True(doc.GeneratedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void EnrichedScenario_CanStoreMultipleExampleResults()
    {
        // Arrange
        var scenario = new EnrichedScenario();
        var result1 = new ScenarioExecutionResult { Status = ExecutionStatus.Passed };
        var result2 = new ScenarioExecutionResult { Status = ExecutionStatus.Failed };

        // Act
        scenario.ExampleResults[0] = result1;
        scenario.ExampleResults[1] = result2;

        // Assert
        Assert.Equal(2, scenario.ExampleResults.Count);
        Assert.Equal(ExecutionStatus.Passed, scenario.ExampleResults[0].Status);
        Assert.Equal(ExecutionStatus.Failed, scenario.ExampleResults[1].Status);
    }
}
