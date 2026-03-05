using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

public class ChunkedOutputPipelineTests : IDisposable
{
    private readonly Mock<IChunkEmitterService> _chunkEmitterMock = new();
    private readonly Mock<IIndexBuilderService> _indexBuilderMock = new();
    private readonly Mock<IManifestBuilderService> _manifestBuilderMock = new();
    private readonly ChunkedOutputPipeline _sut;
    private readonly string _tempDir;

    public ChunkedOutputPipelineTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "chunked_pipeline_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        _sut = new ChunkedOutputPipeline(
            _chunkEmitterMock.Object,
            _indexBuilderMock.Object,
            _manifestBuilderMock.Object);

        // Default mock setups
        _chunkEmitterMock
            .Setup(e => e.BuildChunk(It.IsAny<EnrichedFeature>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<HtmlGenerationOptions>()))
            .Returns((EnrichedFeature f, int idx, string buildId, HtmlGenerationOptions opts) =>
                new FeatureChunk
                {
                    BuildId = buildId,
                    FeatureId = ContractHashValidator.GenerateFeatureId(
                        f.Feature.FilePath ?? string.Empty, f.Feature.Name),
                    Name = f.Feature.Name,
                    Status = "passed",
                    Html = "<div>test</div>"
                });

        _chunkEmitterMock
            .Setup(e => e.SerializeChunk(It.IsAny<FeatureChunk>()))
            .Returns((FeatureChunk c) => ("{\"featureId\":\"" + c.FeatureId + "\"}", "hash_" + c.FeatureId));

        _indexBuilderMock
            .Setup(i => i.BuildIndex(It.IsAny<LivingDocumentation>(), It.IsAny<string>()))
            .Returns((LivingDocumentation doc, string buildId) =>
                new FeatureIndex { BuildId = buildId });

        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Returns((LivingDocumentation doc, string buildId, Dictionary<string, ChunkMetadata> meta) =>
                new FeatureManifest
                {
                    BuildId = buildId,
                    TotalFeatures = doc.Features.Count,
                    TotalScenarios = doc.Features.Sum(f => f.Scenarios.Count)
                });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Cleanup best-effort
        }
    }

    [Fact]
    public async Task ExecuteAsync_NullDocumentation_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.ExecuteAsync(null!, _tempDir, new HtmlGenerationOptions()));
    }

    [Fact]
    public async Task ExecuteAsync_NullOutputDirectory_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ExecuteAsync(new LivingDocumentation(), null!, new HtmlGenerationOptions()));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyOutputDirectory_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.ExecuteAsync(new LivingDocumentation(), "  ", new HtmlGenerationOptions()));
    }

    [Fact]
    public async Task ExecuteAsync_EmptyDocumentation_CreatesManifestAndIndex()
    {
        var doc = new LivingDocumentation();

        var result = await _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.NotEmpty(result.BuildId);
        Assert.Equal(0, result.TotalFeatures);
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.IndexPath));
        Assert.Empty(result.ChunkPaths);
    }

    [Fact]
    public async Task ExecuteAsync_SingleFeature_CreatesAllArtifacts()
    {
        var doc = CreateDocumentation(
            CreateFeature("Login", "login.feature",
                CreateScenario("Valid login")));

        var result = await _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        // Manifest and index should exist
        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.IndexPath));
        Assert.Contains("feature-manifest.json", result.ManifestPath);
        Assert.Contains("feature-index.json", result.IndexPath);

        // One chunk file
        Assert.Single(result.ChunkPaths);
        Assert.True(File.Exists(result.ChunkPaths[0]));

        // Chunk should be in features/ subdirectory
        Assert.Contains("features", result.ChunkPaths[0]);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleFeatures_CreatesChunkPerFeature()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature A", "a.feature", CreateScenario("S1")),
            CreateFeature("Feature B", "b.feature", CreateScenario("S2")),
            CreateFeature("Feature C", "c.feature", CreateScenario("S3")));

        var result = await _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.Equal(3, result.ChunkPaths.Count);
        Assert.Equal(3, result.TotalFeatures);
        Assert.All(result.ChunkPaths, path => Assert.True(File.Exists(path)));
    }

    [Fact]
    public async Task ExecuteAsync_CallsServicesInCorrectOrder()
    {
        var callOrder = new List<string>();

        _chunkEmitterMock
            .Setup(e => e.BuildChunk(It.IsAny<EnrichedFeature>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<HtmlGenerationOptions>()))
            .Callback(() => callOrder.Add("BuildChunk"))
            .Returns((EnrichedFeature f, int idx, string buildId, HtmlGenerationOptions opts) =>
                new FeatureChunk
                {
                    BuildId = buildId,
                    FeatureId = "feat-" + idx,
                    Name = f.Feature.Name,
                    Status = "passed",
                    Html = "<div>test</div>"
                });

        _chunkEmitterMock
            .Setup(e => e.SerializeChunk(It.IsAny<FeatureChunk>()))
            .Callback(() => callOrder.Add("SerializeChunk"))
            .Returns(("{}", "hash"));

        _indexBuilderMock
            .Setup(i => i.BuildIndex(It.IsAny<LivingDocumentation>(), It.IsAny<string>()))
            .Callback(() => callOrder.Add("BuildIndex"))
            .Returns(new FeatureIndex());

        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Callback(() => callOrder.Add("BuildManifest"))
            .Returns(new FeatureManifest());

        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", CreateScenario("S1")));

        await _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        // Chunks should be built and serialized before index, index before manifest
        var chunkIdx = callOrder.IndexOf("BuildChunk");
        var indexIdx = callOrder.IndexOf("BuildIndex");
        var manifestIdx = callOrder.IndexOf("BuildManifest");

        Assert.True(chunkIdx < indexIdx, "Chunks should be built before index");
        Assert.True(indexIdx < manifestIdx, "Index should be built before manifest");
    }

    [Fact]
    public async Task ExecuteAsync_CancellationToken_ThrowsWhenCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", CreateScenario("S1")));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions(), cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_CreatesOutputDirectoryIfNotExists()
    {
        var newDir = Path.Combine(_tempDir, "new_subdir", "output");
        var doc = new LivingDocumentation();

        var result = await _sut.ExecuteAsync(doc, newDir, new HtmlGenerationOptions());

        Assert.True(Directory.Exists(newDir));
        Assert.True(File.Exists(result.ManifestPath));
    }

    [Fact]
    public async Task ExecuteAsync_NullOptions_UsesDefaults()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", CreateScenario("S1")));

        // Should not throw; null options handled gracefully
        var result = await _sut.ExecuteAsync(doc, _tempDir, null);

        Assert.NotNull(result);
        Assert.True(File.Exists(result.ManifestPath));
    }

    [Fact]
    public async Task ExecuteAsync_PassesChunkMetadataToManifestBuilder()
    {
        Dictionary<string, ChunkMetadata> capturedMetadata = null;

        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Callback<LivingDocumentation, string, Dictionary<string, ChunkMetadata>>((doc, buildId, meta) =>
                capturedMetadata = meta)
            .Returns(new FeatureManifest());

        var feature = CreateFeature("Login", "login.feature", CreateScenario("S1"));
        var featureId = ContractHashValidator.GenerateFeatureId("login.feature", "Login");
        var docData = CreateDocumentation(feature);

        await _sut.ExecuteAsync(docData, _tempDir, new HtmlGenerationOptions());

        Assert.NotNull(capturedMetadata);
        Assert.True(capturedMetadata.ContainsKey(featureId));
        Assert.Equal("hash_" + featureId, capturedMetadata[featureId].ChunkHash);
    }

    [Fact]
    public async Task ExecuteAsync_ChunkFilesArePersisted()
    {
        var doc = CreateDocumentation(
            CreateFeature("Feature", "f.feature", CreateScenario("S1")));

        var result = await _sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        var chunkContent = await File.ReadAllTextAsync(result.ChunkPaths[0]);
        Assert.Contains("featureId", chunkContent);
    }

    // ---------- Helpers ----------

    private static LivingDocumentation CreateDocumentation(params EnrichedFeature[] features)
    {
        return new LivingDocumentation
        {
            Features = features?.ToList() ?? new List<EnrichedFeature>()
        };
    }

    private static EnrichedFeature CreateFeature(string name, string filePath, params EnrichedScenario[] scenarios)
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
            OverallStatus = ExecutionStatus.Passed
        };
    }

    private static EnrichedScenario CreateScenario(string name)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario { Name = name },
            Status = ExecutionStatus.Passed,
            Steps = new List<EnrichedStep>()
        };
    }
}
