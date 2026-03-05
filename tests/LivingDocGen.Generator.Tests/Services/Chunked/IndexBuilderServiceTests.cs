using System.Collections.Generic;
using System.Linq;
using Moq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

public class IndexBuilderServiceTests
{
    private readonly IndexBuilderService _sut;
    private readonly TokenizerService _tokenizer = new TokenizerService();

    public IndexBuilderServiceTests()
    {
        _sut = new IndexBuilderService(_tokenizer);
    }

    [Fact]
    public void BuildIndex_NullDocumentation_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => _sut.BuildIndex(null!, "build-1"));
    }

    [Fact]
    public void BuildIndex_EmptyDocumentation_ReturnsEmptyIndex()
    {
        var doc = CreateDocumentation();

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.Equal("build-1", index.BuildId);
        Assert.Empty(index.ScenarioRecords);
        Assert.Empty(index.InvertedTokenIndex);
    }

    [Fact]
    public void BuildIndex_SingleFeatureSingleScenario_CreatesScenarioRecord()
    {
        var doc = CreateDocumentation(
            CreateFeature("Login Feature", scenarios: new[]
            {
                CreateScenario("Successful login", ExecutionStatus.Passed)
            }));

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.Single(index.ScenarioRecords);
        var record = index.ScenarioRecords[0];
        Assert.Equal(0, record.ScenarioOrdinal);
        Assert.Equal(0, record.FeatureOrdinal);
        Assert.Equal("Successful login", record.Name);
        Assert.Equal("passed", record.Status);
    }

    [Fact]
    public void BuildIndex_PopulatesInvertedTokenIndex()
    {
        var doc = CreateDocumentation(
            CreateFeature("Login Feature", scenarios: new[]
            {
                CreateScenario("Successful login", ExecutionStatus.Passed)
            }));

        var index = _sut.BuildIndex(doc, "build-1");

        // "login" should be a token (appears in both feature name and scenario name)
        Assert.True(index.InvertedTokenIndex.ContainsKey("login"));
        Assert.Contains(0, index.InvertedTokenIndex["login"]);
    }

    [Fact]
    public void BuildIndex_PopulatesStatusIndex()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", scenarios: new[]
            {
                CreateScenario("Passed scenario", ExecutionStatus.Passed),
                CreateScenario("Failed scenario", ExecutionStatus.Failed),
                CreateScenario("Skipped scenario", ExecutionStatus.Skipped)
            }));

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.Contains(0, index.StatusIndex.Passed);
        Assert.Contains(1, index.StatusIndex.Failed);
        Assert.Contains(2, index.StatusIndex.Skipped);
    }

    [Fact]
    public void BuildIndex_PopulatesTagIndex()
    {
        var feature = CreateFeature("Feature",
            tags: new[] { "@smoke" },
            scenarios: new[]
            {
                CreateScenario("Scenario A", ExecutionStatus.Passed, tags: new[] { "@regression" })
            });
        var doc = CreateDocumentation(feature);

        var index = _sut.BuildIndex(doc, "build-1");

        // Tags should be normalized (trimmed @, lowered)
        Assert.True(index.TagIndex.ContainsKey("smoke"));
        Assert.True(index.TagIndex.ContainsKey("regression"));
        Assert.Contains(0, index.TagIndex["smoke"]);
        Assert.Contains(0, index.TagIndex["regression"]);
    }

    [Fact]
    public void BuildIndex_MultipleFeatures_CorrectOrdinals()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature A", scenarios: new[]
            {
                CreateScenario("Scenario A1", ExecutionStatus.Passed),
                CreateScenario("Scenario A2", ExecutionStatus.Failed)
            }),
            CreateFeature("Feature B", scenarios: new[]
            {
                CreateScenario("Scenario B1", ExecutionStatus.Skipped)
            }));

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.Equal(3, index.ScenarioRecords.Count);

        Assert.Equal(0, index.ScenarioRecords[0].FeatureOrdinal);
        Assert.Equal(0, index.ScenarioRecords[0].ScenarioOrdinal);

        Assert.Equal(0, index.ScenarioRecords[1].FeatureOrdinal);
        Assert.Equal(1, index.ScenarioRecords[1].ScenarioOrdinal);

        Assert.Equal(1, index.ScenarioRecords[2].FeatureOrdinal);
        Assert.Equal(2, index.ScenarioRecords[2].ScenarioOrdinal);
    }

    [Fact]
    public void BuildIndex_SetsNormalizationMetadata()
    {
        var doc = CreateDocumentation();

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.NotNull(index.Normalization);
        Assert.True(index.Normalization.CaseFolding);
        Assert.True(index.Normalization.UnicodeFolding);
        Assert.Equal("invariant", index.Normalization.Locale);
    }

    [Fact]
    public void BuildIndex_UntestedStatus_MappedCorrectly()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", scenarios: new[]
            {
                CreateScenario("Untested scenario", ExecutionStatus.NotExecuted)
            }));

        var index = _sut.BuildIndex(doc, "build-1");

        Assert.Equal("untested", index.ScenarioRecords[0].Status);
    }

    [Fact]
    public void BuildIndex_FeatureWithDescription_TokenizesDescription()
    {
        var feature = CreateFeature("Login", description: "Authentication workflow testing");
        feature.Scenarios.Add(CreateScenario("Valid credentials", ExecutionStatus.Passed));
        var doc = CreateDocumentation(feature);

        var index = _sut.BuildIndex(doc, "build-1");

        // Tokens from the description should appear
        Assert.True(index.InvertedTokenIndex.ContainsKey("authentication"));
        Assert.True(index.InvertedTokenIndex.ContainsKey("workflow"));
    }

    // ---------- Helpers ----------

    private static LivingDocumentation CreateDocumentation(params EnrichedFeature[] features)
    {
        return new LivingDocumentation
        {
            Features = features?.ToList() ?? new List<EnrichedFeature>()
        };
    }

    private static EnrichedFeature CreateFeature(
        string name,
        string description = null,
        string[] tags = null,
        EnrichedScenario[] scenarios = null)
    {
        var feature = new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = name,
                Description = description ?? string.Empty,
                FilePath = $"/features/{name.ToLower().Replace(' ', '_')}.feature",
                Tags = tags?.ToList() ?? new List<string>()
            },
            Scenarios = scenarios?.ToList() ?? new List<EnrichedScenario>()
        };
        return feature;
    }

    private static EnrichedScenario CreateScenario(
        string name,
        ExecutionStatus status,
        string description = null,
        string[] tags = null)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario
            {
                Name = name,
                Description = description ?? string.Empty,
                Tags = tags?.ToList() ?? new List<string>()
            },
            Status = status
        };
    }
}
