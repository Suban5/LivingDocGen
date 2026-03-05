using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Generator.Services.Rendering;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

public class ChunkEmitterServiceTests
{
    private readonly Mock<IFeatureRenderer> _rendererMock = new Mock<IFeatureRenderer>();
    private readonly ChunkEmitterService _sut;

    public ChunkEmitterServiceTests()
    {
        _rendererMock
            .Setup(r => r.Render(It.IsAny<EnrichedFeature>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns("<div>rendered html</div>");
        _sut = new ChunkEmitterService(_rendererMock.Object);
    }

    [Fact]
    public void BuildChunk_NullFeature_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => _sut.BuildChunk(null!, 0, "build-1", new HtmlGenerationOptions()));
    }

    [Fact]
    public void BuildChunk_SetsBasicProperties()
    {
        var feature = CreateFeature("Login Feature", "login.feature", ExecutionStatus.Passed);

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal("build-1", chunk.BuildId);
        Assert.Equal("Login Feature", chunk.Name);
        Assert.Equal("passed", chunk.Status);
        Assert.NotEmpty(chunk.FeatureId);
    }

    [Fact]
    public void BuildChunk_UsesRendererForHtml()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed);

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal("<div>rendered html</div>", chunk.Html);
        _rendererMock.Verify(
            r => r.Render(feature, 0, true),
            Times.Once);
    }

    [Fact]
    public void BuildChunk_SetsIncludeCommentsFromOptions()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed);
        var options = new HtmlGenerationOptions { IncludeComments = true };

        _sut.BuildChunk(feature, 0, "build-1", options);

        _rendererMock.VerifySet(r => r.IncludeComments = true, Times.Once);
    }

    [Fact]
    public void BuildChunk_ScenarioSummary_IsCorrect()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
            CreateScenario("S1", ExecutionStatus.Passed),
            CreateScenario("S2", ExecutionStatus.Failed),
            CreateScenario("S3", ExecutionStatus.Skipped));
        feature.PassedCount = 1;
        feature.FailedCount = 1;
        feature.SkippedCount = 1;
        feature.UntestedCount = 0;

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(3, chunk.ScenarioSummary.Total);
        Assert.Equal(1, chunk.ScenarioSummary.Passed);
        Assert.Equal(1, chunk.ScenarioSummary.Failed);
        Assert.Equal(1, chunk.ScenarioSummary.Skipped);
        Assert.Equal(0, chunk.ScenarioSummary.Untested);
    }

    [Fact]
    public void BuildChunk_StatusMapping_AllStatuses()
    {
        void AssertStatus(ExecutionStatus input, string expected)
        {
            var feature = CreateFeature("F", "f.feature", input);
            var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());
            Assert.Equal(expected, chunk.Status);
        }

        AssertStatus(ExecutionStatus.Passed, "passed");
        AssertStatus(ExecutionStatus.Failed, "failed");
        AssertStatus(ExecutionStatus.Skipped, "skipped");
        AssertStatus(ExecutionStatus.NotExecuted, "untested");
    }

    [Fact]
    public void BuildChunk_ContentStats_WithDataTable()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
            CreateScenarioWithDataTable("S1", new List<List<string>>
            {
                new() { "A", "B", "C" },
                new() { "1", "2", "3" },
                new() { "4", "5", "6" }
            }));

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(3, chunk.ContentStats.MaxTableRows);
        Assert.Equal(3, chunk.ContentStats.MaxTableColumns);
    }

    [Fact]
    public void BuildChunk_ContentStats_WithExamples()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
            CreateScenarioWithExamples("Outline", new List<string> { "col1", "col2" },
                new List<List<string>>
                {
                    new() { "a", "b" },
                    new() { "c", "d" }
                }));

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(2, chunk.ContentStats.OutlineExamples);
    }

    [Fact]
    public void BuildChunk_ContentStats_NoDataTablesOrExamples_AllZero()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
            CreateScenario("S1", ExecutionStatus.Passed));

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(0, chunk.ContentStats.MaxTableRows);
        Assert.Equal(0, chunk.ContentStats.MaxTableColumns);
        Assert.Equal(0, chunk.ContentStats.OutlineExamples);
    }

    [Fact]
    public void BuildChunk_RenderHints_AreSet()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed);

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(200, chunk.RenderHints.VirtualizationThreshold);
        Assert.Equal(200, chunk.RenderHints.TableChunkingThreshold);
    }

    [Fact]
    public void BuildChunk_GeneratesDeterministicFeatureId()
    {
        var feature = CreateFeature("Login Feature", "login.feature", ExecutionStatus.Passed);
        var expectedId = ContractHashValidator.GenerateFeatureId("login.feature", "Login Feature");

        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        Assert.Equal(expectedId, chunk.FeatureId);
    }

    [Fact]
    public void BuildChunk_NullOptions_DoesNotThrow()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed);

        var chunk = _sut.BuildChunk(feature, 0, "build-1", null);

        Assert.NotNull(chunk);
    }

    [Fact]
    public void SerializeChunk_ReturnsJsonAndHash()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed);
        var chunk = _sut.BuildChunk(feature, 0, "build-1", new HtmlGenerationOptions());

        var (json, hash) = _sut.SerializeChunk(chunk);

        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Contains("build-1", json);
    }

    // ---------- Helpers ----------

    private static EnrichedFeature CreateFeature(
        string name, string filePath, ExecutionStatus status,
        params EnrichedScenario[] scenarios)
    {
        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = name,
                FilePath = filePath,
                Tags = new List<string>()
            },
            Scenarios = scenarios?.ToList() ?? new List<EnrichedScenario>(),
            OverallStatus = status
        };
    }

    private static EnrichedScenario CreateScenario(string name, ExecutionStatus status)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario { Name = name },
            Status = status,
            Steps = new List<EnrichedStep>()
        };
    }

    private static EnrichedScenario CreateScenarioWithDataTable(
        string name, List<List<string>> rows)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario { Name = name },
            Status = ExecutionStatus.Passed,
            Steps = new List<EnrichedStep>
            {
                new EnrichedStep
                {
                    Step = new UniversalStep
                    {
                        Keyword = "Given",
                        Text = "some data",
                        DataTable = new UniversalDataTable { Rows = rows }
                    }
                }
            }
        };
    }

    private static EnrichedScenario CreateScenarioWithExamples(
        string name, List<string> headers, List<List<string>> rows)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario
            {
                Name = name,
                Type = ScenarioType.ScenarioOutline,
                Examples = new List<UniversalExample>
                {
                    new UniversalExample
                    {
                        Headers = headers,
                        Rows = rows
                    }
                }
            },
            Status = ExecutionStatus.Passed,
            Steps = new List<EnrichedStep>()
        };
    }
}
