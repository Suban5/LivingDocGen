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

/// <summary>
/// Tests for ChunkedOutputPipeline's shell HTML emission (PR-3 addition).
/// Verifies that the pipeline writes index.html when IHtmlGeneratorService is available.
/// </summary>
public class ChunkedOutputPipelineShellHtmlTests : IDisposable
{
    private readonly Mock<IChunkEmitterService> _chunkEmitterMock = new();
    private readonly Mock<IIndexBuilderService> _indexBuilderMock = new();
    private readonly Mock<IManifestBuilderService> _manifestBuilderMock = new();
    private readonly Mock<IHtmlGeneratorService> _htmlGeneratorMock = new();
    private readonly string _tempDir;

    public ChunkedOutputPipelineShellHtmlTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "chunked_shell_tests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        _chunkEmitterMock
            .Setup(e => e.BuildChunk(It.IsAny<EnrichedFeature>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<HtmlGenerationOptions>()))
            .Returns((EnrichedFeature f, int idx, string buildId, HtmlGenerationOptions opts) =>
                new FeatureChunk
                {
                    BuildId = buildId,
                    FeatureId = ContractHashValidator.GenerateFeatureId(f.Feature.FilePath ?? "", f.Feature.Name),
                    Name = f.Feature.Name,
                    Status = "passed",
                    Html = "<div>test</div>"
                });

        _chunkEmitterMock
            .Setup(e => e.SerializeChunk(It.IsAny<FeatureChunk>()))
            .Returns((FeatureChunk c) => ("{}", "hash_" + c.FeatureId));

        _indexBuilderMock
            .Setup(i => i.BuildIndex(It.IsAny<LivingDocumentation>(), It.IsAny<string>()))
            .Returns((LivingDocumentation doc, string buildId) => new FeatureIndex { BuildId = buildId });

        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Returns((LivingDocumentation doc, string buildId, Dictionary<string, ChunkMetadata> meta) =>
                new FeatureManifest
                {
                    BuildId = buildId,
                    TotalFeatures = doc.Features.Count,
                    TotalScenarios = doc.Features.Sum(f => f.Scenarios.Count)
                });

        _htmlGeneratorMock
            .Setup(h => h.GenerateShellHtml(It.IsAny<LivingDocumentation>(), It.IsAny<HtmlGenerationOptions>()))
            .Returns("<!DOCTYPE html><html><body>Shell HTML</body></html>");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
        catch { /* cleanup best-effort */ }
    }

    // ===== Shell HTML Emission Tests =====

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_EmitsShellHtml()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.NotEmpty(result.ShellHtmlPath);
        Assert.True(File.Exists(result.ShellHtmlPath));
    }

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_ShellPathIsIndexHtml()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.Equal(Path.Combine(_tempDir, "index.html"), result.ShellHtmlPath);
    }

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_ShellContainsExpectedContent()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        var content = await File.ReadAllTextAsync(result.ShellHtmlPath);
        Assert.Contains("Shell HTML", content);
    }

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_CallsGenerateShellHtml()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        _htmlGeneratorMock.Verify(
            h => h.GenerateShellHtml(It.IsAny<LivingDocumentation>(), It.IsAny<HtmlGenerationOptions>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_PassesCorrectDocumentation()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));
        LivingDocumentation capturedDoc = null;

        _htmlGeneratorMock
            .Setup(h => h.GenerateShellHtml(It.IsAny<LivingDocumentation>(), It.IsAny<HtmlGenerationOptions>()))
            .Callback<LivingDocumentation, HtmlGenerationOptions>((d, o) => capturedDoc = d)
            .Returns("<html></html>");

        await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.Same(doc, capturedDoc);
    }

    [Fact]
    public async Task ExecuteAsync_WithHtmlGenerator_PassesCorrectOptions()
    {
        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));
        var options = new HtmlGenerationOptions { Theme = "blue" };
        HtmlGenerationOptions capturedOptions = null;

        _htmlGeneratorMock
            .Setup(h => h.GenerateShellHtml(It.IsAny<LivingDocumentation>(), It.IsAny<HtmlGenerationOptions>()))
            .Callback<LivingDocumentation, HtmlGenerationOptions>((d, o) => capturedOptions = o)
            .Returns("<html></html>");

        await sut.ExecuteAsync(doc, _tempDir, options);

        Assert.Same(options, capturedOptions);
    }

    // ===== Without HtmlGenerator =====

    [Fact]
    public async Task ExecuteAsync_WithoutHtmlGenerator_DoesNotEmitShellHtml()
    {
        var sut = CreatePipeline(withHtmlGenerator: false);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.Empty(result.ShellHtmlPath);
        Assert.False(File.Exists(Path.Combine(_tempDir, "index.html")));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutHtmlGenerator_StillCreatesManifestAndChunks()
    {
        var sut = CreatePipeline(withHtmlGenerator: false);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.NotEmpty(result.ManifestPath);
        Assert.NotEmpty(result.IndexPath);
        Assert.Single(result.ChunkPaths);
    }

    // ===== Shell HTML Ordering =====

    [Fact]
    public async Task ExecuteAsync_ShellHtmlWrittenAfterManifest()
    {
        var callOrder = new List<string>();

        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Callback(() => callOrder.Add("Manifest"))
            .Returns(new FeatureManifest());

        _htmlGeneratorMock
            .Setup(h => h.GenerateShellHtml(It.IsAny<LivingDocumentation>(), It.IsAny<HtmlGenerationOptions>()))
            .Callback(() => callOrder.Add("ShellHtml"))
            .Returns("<html></html>");

        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        var manifestIdx = callOrder.IndexOf("Manifest");
        var shellIdx = callOrder.IndexOf("ShellHtml");
        Assert.True(manifestIdx < shellIdx, "Manifest should be built before shell HTML");
    }

    // ===== Cancellation =====

    [Fact]
    public async Task ExecuteAsync_CancellationBeforeShellHtml_ThrowsOperationCanceled()
    {
        var cts = new CancellationTokenSource();
        
        // Cancel after manifest is built
        _manifestBuilderMock
            .Setup(m => m.BuildManifest(It.IsAny<LivingDocumentation>(), It.IsAny<string>(), It.IsAny<Dictionary<string, ChunkMetadata>>()))
            .Callback(() => cts.Cancel())
            .Returns(new FeatureManifest());

        var sut = CreatePipeline(withHtmlGenerator: true);
        var doc = CreateDocumentation(CreateFeature("F1", "f1.feature"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions(), cts.Token));
    }

    // ===== Integration with Real HtmlGeneratorService =====

    [Fact]
    public async Task ExecuteAsync_WithRealHtmlGenerator_ProducesValidHtml()
    {
        var realHtmlGenerator = new HtmlGeneratorService();
        var sut = new ChunkedOutputPipeline(
            _chunkEmitterMock.Object,
            _indexBuilderMock.Object,
            _manifestBuilderMock.Object,
            realHtmlGenerator);

        var doc = CreateDocumentation(CreateFeature("Feature1", "Features/f1.feature"));
        doc.Title = "Test Documentation";
        doc.GeneratedAt = DateTime.Now;
        doc.Statistics = new DocumentStatistics
        {
            TotalFeatures = 1,
            TotalScenarios = 1,
            PassedScenarios = 1
        };

        var result = await sut.ExecuteAsync(doc, _tempDir, new HtmlGenerationOptions());

        Assert.True(File.Exists(result.ShellHtmlPath));
        var content = await File.ReadAllTextAsync(result.ShellHtmlPath);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("CHUNKED_MODE", content);
        Assert.Contains("LRUCache", content);
        Assert.Contains("sidebar-nav", content);
        Assert.Contains("main-content", content);
    }

    // ===== Helpers =====

    private ChunkedOutputPipeline CreatePipeline(bool withHtmlGenerator)
    {
        return new ChunkedOutputPipeline(
            _chunkEmitterMock.Object,
            _indexBuilderMock.Object,
            _manifestBuilderMock.Object,
            withHtmlGenerator ? _htmlGeneratorMock.Object : null);
    }

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
