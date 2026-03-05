using System.Collections.Generic;
using System.Linq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

public class ManifestBuilderServiceTests
{
    private readonly ManifestBuilderService _sut = new ManifestBuilderService();

    [Fact]
    public void BuildManifest_NullDocumentation_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => _sut.BuildManifest(null!, "build-1", new Dictionary<string, ChunkMetadata>()));
    }

    [Fact]
    public void BuildManifest_NullChunkMetadata_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(
            () => _sut.BuildManifest(new LivingDocumentation(), "build-1", null!));
    }

    [Fact]
    public void BuildManifest_EmptyDocumentation_ReturnsEmptyManifest()
    {
        var doc = new LivingDocumentation();

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        Assert.Equal("build-1", manifest.BuildId);
        Assert.Equal(0, manifest.TotalFeatures);
        Assert.Equal(0, manifest.TotalScenarios);
        Assert.Empty(manifest.FeatureMap);
        Assert.Equal("feature-index.json", manifest.IndexFile);
        Assert.Equal("per-feature", manifest.ChunkStrategy);
    }

    [Fact]
    public void BuildManifest_SingleFeature_CreatesCorrectEntry()
    {
        var doc = CreateDocumentation(
            CreateFeature("Login Feature", "login.feature", ExecutionStatus.Passed,
                CreateScenario("Valid login"),
                CreateScenario("Invalid login")));

        var featureId = ContractHashValidator.GenerateFeatureId("login.feature", "Login Feature");
        var metadata = new Dictionary<string, ChunkMetadata>
        {
            [featureId] = new ChunkMetadata { ChunkHash = "abc123", EstimatedBytes = 4096 }
        };

        var manifest = _sut.BuildManifest(doc, "build-1", metadata);

        Assert.Equal(1, manifest.TotalFeatures);
        Assert.Equal(2, manifest.TotalScenarios);
        Assert.Single(manifest.FeatureMap);

        var entry = manifest.FeatureMap[0];
        Assert.Equal(featureId, entry.FeatureId);
        Assert.Equal("Login Feature", entry.Name);
        Assert.Equal("login.feature", entry.FilePath);
        Assert.Equal("passed", entry.Status);
        Assert.Equal(2, entry.ScenarioCount);
        Assert.Equal($"features/{featureId}.json", entry.ChunkFile);
        Assert.Equal("abc123", entry.ChunkHash);
        Assert.Equal(4096, entry.EstimatedBytes);
    }

    [Fact]
    public void BuildManifest_ScenarioRange_IsCorrect()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature A", "a.feature", ExecutionStatus.Passed,
                CreateScenario("A1"),
                CreateScenario("A2"),
                CreateScenario("A3")),
            CreateFeature("Feature B", "b.feature", ExecutionStatus.Failed,
                CreateScenario("B1"),
                CreateScenario("B2")));

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        // Feature A: scenarios 0, 1, 2 → range [0, 3)
        Assert.Equal(0, manifest.FeatureMap[0].ScenarioRange.Start);
        Assert.Equal(3, manifest.FeatureMap[0].ScenarioRange.End);

        // Feature B: scenarios 3, 4 → range [3, 5)
        Assert.Equal(3, manifest.FeatureMap[1].ScenarioRange.Start);
        Assert.Equal(5, manifest.FeatureMap[1].ScenarioRange.End);
    }

    [Fact]
    public void BuildManifest_MissingChunkMetadata_UsesDefaults()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
                CreateScenario("S1")));

        // Empty metadata — feature ID won't match
        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        var entry = manifest.FeatureMap[0];
        Assert.Equal(string.Empty, entry.ChunkHash);
        Assert.Equal(0, entry.EstimatedBytes);
    }

    [Fact]
    public void BuildManifest_CapabilitiesAreSet()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
                CreateScenario("S1")));

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        Assert.True(manifest.Capabilities.WorkerSearch);
        Assert.True(manifest.Capabilities.Virtualization);
        Assert.True(manifest.Capabilities.TableChunking);
    }

    [Fact]
    public void BuildManifest_PreservesTags()
    {
        var feature = CreateFeature("Feature", "f.feature", ExecutionStatus.Passed,
            CreateScenario("S1"));
        feature.Feature.Tags = new List<string> { "@smoke", "@regression" };
        var doc = CreateDocumentation(feature);

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        Assert.Contains("@smoke", manifest.FeatureMap[0].Tags);
        Assert.Contains("@regression", manifest.FeatureMap[0].Tags);
    }

    [Fact]
    public void BuildManifest_StatusMapping_AllStatuses()
    {
        var doc = CreateDocumentation(
            CreateFeature("Passed", "p.feature", ExecutionStatus.Passed, CreateScenario("S1")),
            CreateFeature("Failed", "f.feature", ExecutionStatus.Failed, CreateScenario("S2")),
            CreateFeature("Skipped", "s.feature", ExecutionStatus.Skipped, CreateScenario("S3")),
            CreateFeature("Untested", "u.feature", ExecutionStatus.NotExecuted, CreateScenario("S4")));

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        Assert.Equal("passed", manifest.FeatureMap[0].Status);
        Assert.Equal("failed", manifest.FeatureMap[1].Status);
        Assert.Equal("skipped", manifest.FeatureMap[2].Status);
        Assert.Equal("untested", manifest.FeatureMap[3].Status);
    }

    [Fact]
    public void BuildManifest_ChunkCount_MatchesFeatureCount()
    {
        var doc = CreateDocumentation(
            CreateFeature("A", "a.feature", ExecutionStatus.Passed, CreateScenario("S1")),
            CreateFeature("B", "b.feature", ExecutionStatus.Passed, CreateScenario("S2")),
            CreateFeature("C", "c.feature", ExecutionStatus.Passed, CreateScenario("S3")));

        var manifest = _sut.BuildManifest(doc, "build-1", new Dictionary<string, ChunkMetadata>());

        Assert.Equal(3, manifest.ChunkCount);
        Assert.Equal(3, manifest.FeatureMap.Count);
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
        string name, string filePath, ExecutionStatus overallStatus,
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
            OverallStatus = overallStatus
        };
    }

    private static EnrichedScenario CreateScenario(string name)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario { Name = name },
            Status = ExecutionStatus.Passed
        };
    }
}
