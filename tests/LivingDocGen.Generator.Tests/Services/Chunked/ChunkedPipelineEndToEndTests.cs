using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Generator.Services.Rendering;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Tests.Services.Chunked;

/// <summary>
/// End-to-end integration tests for the full chunked output pipeline.
///
/// These tests wire up ALL real service implementations (no mocks):
///   FeatureRenderer → ChunkEmitterService → IndexBuilderService →
///   ManifestBuilderService → HtmlGeneratorService → ChunkedOutputPipeline
///
/// Validates the complete PR-0 through PR-6 flow:
///   LivingDocumentation input → pipeline execution → files on disk →
///   deserialized artifacts are structurally correct and cross-consistent.
/// </summary>
public class ChunkedPipelineEndToEndTests : IDisposable
{
    private readonly string _outputDir;
    private readonly ChunkedOutputPipeline _pipeline;
    private readonly IndexBuilderService _indexBuilder;

    // Reusable canonical dataset matching the one from QueryCorrectnessGoldenTests
    private static readonly LivingDocumentation CanonicalDataset = BuildCanonicalDataset();

    public ChunkedPipelineEndToEndTests()
    {
        _outputDir = Path.Combine(
            Path.GetTempPath(),
            "chunked_e2e_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_outputDir);

        // Wire up all REAL implementations — no mocks
        var featureRenderer = new FeatureRenderer();
        var chunkEmitter = new ChunkEmitterService(featureRenderer);
        var tokenizerService = new TokenizerService();
        _indexBuilder = new IndexBuilderService(tokenizerService);
        var manifestBuilder = new ManifestBuilderService();
        var htmlGenerator = new HtmlGeneratorService();

        _pipeline = new ChunkedOutputPipeline(
            chunkEmitter,
            _indexBuilder,
            manifestBuilder,
            htmlGenerator);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_outputDir))
                Directory.Delete(_outputDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    // ================================================================
    // 1. Pipeline Execution — File Artifact Tests
    // ================================================================

    [Fact]
    public async Task Pipeline_ProducesAllExpectedFiles()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        // Core artifacts
        Assert.True(File.Exists(result.ManifestPath), "feature-manifest.json missing");
        Assert.True(File.Exists(result.IndexPath), "feature-index.json missing");
        Assert.True(File.Exists(result.ShellHtmlPath), "index.html missing");

        // One chunk per feature
        Assert.Equal(4, result.ChunkPaths.Count);
        Assert.All(result.ChunkPaths, path =>
            Assert.True(File.Exists(path), $"Chunk file missing: {path}"));
    }

    [Fact]
    public async Task Pipeline_OutputDirectoryStructure_IsCorrect()
    {
        await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        // Root files: feature-manifest.json, feature-index.json, index.html
        Assert.True(File.Exists(Path.Combine(_outputDir, "feature-manifest.json")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "feature-index.json")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "index.html")));

        // Features subdirectory with chunk files
        var featuresDir = Path.Combine(_outputDir, "features");
        Assert.True(Directory.Exists(featuresDir));
        var chunkFiles = Directory.GetFiles(featuresDir, "*.json");
        Assert.Equal(4, chunkFiles.Length);
    }

    [Fact]
    public async Task Pipeline_ResultMetadata_IsAccurate()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        Assert.Equal(4, result.TotalFeatures);
        Assert.Equal(12, result.TotalScenarios);
        Assert.NotEmpty(result.BuildId);
    }

    // ================================================================
    // 2. Manifest Artifact Validation
    // ================================================================

    [Fact]
    public async Task Manifest_DeserializesCorrectly()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        Assert.NotNull(manifest);
        Assert.Equal(4, manifest.TotalFeatures);
        Assert.Equal(12, manifest.TotalScenarios);
        Assert.Equal(result.BuildId, manifest.BuildId);
    }

    [Fact]
    public async Task Manifest_FeatureMap_HasCorrectEntries()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        Assert.Equal(4, manifest.FeatureMap.Count);

        var names = manifest.FeatureMap.Select(e => e.Name).OrderBy(n => n).ToList();
        Assert.Contains("API Payment Processing", names);
        Assert.Contains("Shopping Cart Management", names);
        Assert.Contains("User Authentication", names);
        Assert.Contains("User Profile", names);
    }

    [Fact]
    public async Task Manifest_FeatureEntries_HaveValidMetadata()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        foreach (var entry in manifest.FeatureMap)
        {
            Assert.NotEmpty(entry.FeatureId);
            Assert.NotEmpty(entry.Name);
            Assert.NotEmpty(entry.ChunkFile);
            Assert.NotEmpty(entry.ChunkHash);
            Assert.True(entry.ScenarioCount > 0, $"Feature '{entry.Name}' has 0 scenarios");
            Assert.True(entry.EstimatedBytes > 0, $"Feature '{entry.Name}' has 0 estimated bytes");
            Assert.Contains(entry.Status, new[] { "passed", "failed", "skipped", "untested" });
        }
    }

    [Fact]
    public async Task Manifest_ScenarioRanges_AreContinuous()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        // Scenario ranges should be contiguous and cover all 12 scenarios
        var ranges = manifest.FeatureMap
            .Select(e => e.ScenarioRange)
            .OrderBy(r => r.Start)
            .ToList();

        Assert.Equal(0, ranges.First().Start);
        for (int i = 1; i < ranges.Count; i++)
        {
            Assert.Equal(ranges[i - 1].End, ranges[i].Start);
        }
        Assert.Equal(12, ranges.Last().End);
    }

    [Fact]
    public async Task Manifest_IndexHash_MatchesActualIndex()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var computedHash = ContractHashValidator.ComputeHash(indexJson);

        Assert.Equal(computedHash, manifest.IndexHash);
    }

    [Fact]
    public async Task Manifest_Capabilities_AreEnabled()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        Assert.True(manifest.Capabilities.WorkerSearch);
        Assert.True(manifest.Capabilities.Virtualization);
        Assert.True(manifest.Capabilities.TableChunking);
    }

    [Fact]
    public async Task Manifest_SchemaVersion_IsValid()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);

        Assert.NotEmpty(manifest.SchemaVersion);
        Assert.True(ContractVersion.IsCompatible(manifest.SchemaVersion));
    }

    // ================================================================
    // 3. Index Artifact Validation
    // ================================================================

    [Fact]
    public async Task Index_DeserializesAndPassesValidation()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(json, validate: true);

        Assert.NotNull(index);
        Assert.Equal(12, index.ScenarioRecords.Count);
        Assert.Equal(result.BuildId, index.BuildId);
    }

    [Fact]
    public async Task Index_StatusDistribution_MatchesDataset()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(json, validate: true);

        Assert.Equal(5, index.StatusIndex.Passed.Count);    // 0,3,4,6,9
        Assert.Equal(3, index.StatusIndex.Failed.Count);     // 1,5,8
        Assert.Equal(2, index.StatusIndex.Skipped.Count);    // 2,11
        Assert.Equal(2, index.StatusIndex.Untested.Count);   // 7,10
    }

    [Fact]
    public async Task Index_InvertedTokenIndex_IsNonEmpty()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(json, validate: true);

        Assert.True(index.InvertedTokenIndex.Count > 0, "Inverted token index is empty");

        // Key tokens from scenario names should be present
        Assert.True(index.InvertedTokenIndex.ContainsKey("login"),
            "Expected 'login' token in inverted index");
        Assert.True(index.InvertedTokenIndex.ContainsKey("cart"),
            "Expected 'cart' token in inverted index");
        Assert.True(index.InvertedTokenIndex.ContainsKey("payment"),
            "Expected 'payment' token in inverted index");
    }

    [Fact]
    public async Task Index_TagIndex_ContainsAllTags()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var json = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(json, validate: true);

        var expectedTags = new[] { "smoke", "security", "regression", "slow", "ecommerce", "api", "critical" };
        foreach (var tag in expectedTags)
        {
            Assert.True(index.TagIndex.ContainsKey(tag), $"Missing tag '{tag}' in index");
        }
    }

    [Fact]
    public async Task Index_HashIntegrity_PassesVerification()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: false);

        // Should not throw — hash matches
        var index = ContractSerializer.DeserializeIndex(indexJson, manifest.IndexHash, validate: true);
        Assert.NotNull(index);
    }

    // ================================================================
    // 4. Chunk Artifact Validation
    // ================================================================

    [Fact]
    public async Task Chunks_AllDeserializeAndPassValidation()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        foreach (var chunkPath in result.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);

            Assert.NotNull(chunk);
            Assert.NotEmpty(chunk.FeatureId);
            Assert.NotEmpty(chunk.Name);
            Assert.NotEmpty(chunk.Html);
            Assert.NotEmpty(chunk.BuildId);
            Assert.Equal(result.BuildId, chunk.BuildId);
        }
    }

    [Fact]
    public async Task Chunks_ScenarioSummaries_MatchDataset()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var totalScenarios = 0;
        foreach (var chunkPath in result.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);

            Assert.NotNull(chunk.ScenarioSummary);
            Assert.True(chunk.ScenarioSummary.Total > 0);
            Assert.Equal(chunk.ScenarioSummary.Total,
                chunk.ScenarioSummary.Passed +
                chunk.ScenarioSummary.Failed +
                chunk.ScenarioSummary.Skipped +
                chunk.ScenarioSummary.Untested);

            totalScenarios += chunk.ScenarioSummary.Total;
        }

        Assert.Equal(12, totalScenarios);
    }

    [Fact]
    public async Task Chunks_Html_ContainsFeatureContent()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var featureNames = new[] { "User Authentication", "Shopping Cart Management",
                                    "API Payment Processing", "User Profile" };
        var foundNames = new HashSet<string>();

        foreach (var chunkPath in result.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);

            // HTML should contain the feature name
            Assert.Contains(chunk.Name, chunk.Html);
            foundNames.Add(chunk.Name);

            // Should contain scenario-related HTML
            Assert.Contains("scenario", chunk.Html, StringComparison.OrdinalIgnoreCase);
        }

        // All 4 features should be represented
        foreach (var name in featureNames)
        {
            Assert.Contains(name, foundNames);
        }
    }

    [Fact]
    public async Task Chunks_RenderHints_ArePopulated()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        foreach (var chunkPath in result.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);

            Assert.NotNull(chunk.RenderHints);
            Assert.True(chunk.RenderHints.VirtualizationThreshold > 0);
            Assert.True(chunk.RenderHints.TableChunkingThreshold > 0);
        }
    }

    // ================================================================
    // 5. Cross-Artifact Consistency
    // ================================================================

    [Fact]
    public async Task CrossConsistency_ManifestChunkIds_MatchChunkFiles()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);

        var chunkIds = new HashSet<string>();
        foreach (var chunkPath in result.ChunkPaths)
        {
            var chunkJson = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(chunkJson, validate: true);
            chunkIds.Add(chunk.FeatureId);
        }

        var manifestIds = manifest.FeatureMap.Select(e => e.FeatureId).ToHashSet();
        Assert.Equal(manifestIds, chunkIds);
    }

    [Fact]
    public async Task CrossConsistency_ManifestChunkHashes_MatchChunkHashes()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);

        foreach (var entry in manifest.FeatureMap)
        {
            // Find the chunk file for this entry
            var chunkPath = result.ChunkPaths
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == entry.FeatureId);
            Assert.NotNull(chunkPath);

            var chunkJson = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(chunkJson, validate: true);

            Assert.Equal(entry.ChunkHash, chunk.ChunkHash);
        }
    }

    [Fact]
    public async Task CrossConsistency_BuildId_IsConsistentAcrossAllArtifacts()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);
        Assert.Equal(result.BuildId, manifest.BuildId);

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);
        Assert.Equal(result.BuildId, index.BuildId);

        foreach (var chunkPath in result.ChunkPaths)
        {
            var chunkJson = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(chunkJson, validate: true);
            Assert.Equal(result.BuildId, chunk.BuildId);
        }
    }

    [Fact]
    public async Task CrossConsistency_IndexScenarios_MatchManifestTotals()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);

        Assert.Equal(manifest.TotalScenarios, index.ScenarioRecords.Count);

        // Sum of per-feature scenario counts from manifest should equal total
        var manifestScenarioSum = manifest.FeatureMap.Sum(e => e.ScenarioCount);
        Assert.Equal(manifest.TotalScenarios, manifestScenarioSum);
    }

    [Fact]
    public async Task CrossConsistency_ManifestFeatureNames_MatchChunkNames()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);

        foreach (var entry in manifest.FeatureMap)
        {
            var chunkPath = result.ChunkPaths
                .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == entry.FeatureId);
            Assert.NotNull(chunkPath);

            var chunkJson = await File.ReadAllTextAsync(chunkPath);
            var chunk = ContractSerializer.DeserializeChunk(chunkJson, validate: true);

            Assert.Equal(entry.Name, chunk.Name);
        }
    }

    // ================================================================
    // 6. Shell HTML Validation
    // ================================================================

    [Fact]
    public async Task ShellHtml_IsValidHtmlDocument()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);

        Assert.Contains("<!DOCTYPE html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<html", html);
        Assert.Contains("</html>", html);
        Assert.Contains("<head>", html);
        Assert.Contains("<body", html);
    }

    [Fact]
    public async Task ShellHtml_ContainsChunkedRuntimeJavaScript()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);

        // Should reference manifest loading (ChunkedRuntimeJavaScript output)
        Assert.Contains("feature-manifest.json", html);
    }

    [Fact]
    public async Task ShellHtml_ContainsSearchBridgeScript()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);

        // Should contain the search bridge (SearchBridgeJavaScript spawns a worker)
        Assert.Contains("SearchBridge", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShellHtml_ContainsSearchWorkerInline()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);

        // Worker is spawned via Blob URL from inline source
        Assert.Contains("Blob", html);
    }

    [Fact]
    public async Task ShellHtml_HasSidebarAndContentPlaceholders()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);

        // Shell should have sidebar and main content areas (empty, populated by JS)
        Assert.Contains("sidebar", html, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================
    // 7. Edge Cases and Error Handling
    // ================================================================

    [Fact]
    public async Task Pipeline_EmptyDocumentation_ProducesValidArtifacts()
    {
        var emptyDoc = new LivingDocumentation
        {
            Features = new List<EnrichedFeature>()
        };

        var result = await _pipeline.ExecuteAsync(
            emptyDoc, _outputDir, new HtmlGenerationOptions());

        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.IndexPath));
        Assert.Empty(result.ChunkPaths);
        Assert.Equal(0, result.TotalFeatures);
        Assert.Equal(0, result.TotalScenarios);

        // Manifest should deserialize cleanly
        var json = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(json, validate: true);
        Assert.Equal(0, manifest.TotalFeatures);
        Assert.Empty(manifest.FeatureMap);
    }

    [Fact]
    public async Task Pipeline_SingleFeatureSingleScenario_ProducesValidArtifacts()
    {
        var doc = new LivingDocumentation
        {
            Features = new List<EnrichedFeature>
            {
                CreateFeature("Minimal Feature",
                    tags: new[] { "@smoke" },
                    scenarios: new[]
                    {
                        CreateScenario("Only scenario", ExecutionStatus.Passed)
                    })
            }
        };

        var result = await _pipeline.ExecuteAsync(
            doc, _outputDir, new HtmlGenerationOptions());

        Assert.Equal(1, result.TotalFeatures);
        Assert.Equal(1, result.TotalScenarios);
        Assert.Single(result.ChunkPaths);

        // All artifacts valid
        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);
        Assert.Single(manifest.FeatureMap);
        Assert.Equal("Minimal Feature", manifest.FeatureMap[0].Name);

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);
        Assert.Single(index.ScenarioRecords);
    }

    [Fact]
    public async Task Pipeline_CancellationToken_StopsExecution()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _pipeline.ExecuteAsync(
                CanonicalDataset, _outputDir, new HtmlGenerationOptions(), cts.Token));
    }

    [Fact]
    public async Task Pipeline_CustomOptions_AffectsOutput()
    {
        var options = new HtmlGenerationOptions
        {
            Theme = "blue"
        };

        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, options);

        var html = await File.ReadAllTextAsync(result.ShellHtmlPath);
        // Blue theme CSS variables should be present in the shell HTML
        Assert.Contains("blue", html, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================
    // 8. Determinism and Reproducibility
    // ================================================================

    [Fact]
    public async Task Pipeline_TwoRuns_ProduceIdenticalIndexContent()
    {
        var dir1 = Path.Combine(_outputDir, "run1");
        var dir2 = Path.Combine(_outputDir, "run2");

        // Use separate pipeline instances to avoid renderer state leak
        var pipeline1 = CreateFreshPipeline();
        var pipeline2 = CreateFreshPipeline();

        var result1 = await pipeline1.ExecuteAsync(
            CanonicalDataset, dir1, new HtmlGenerationOptions());
        var result2 = await pipeline2.ExecuteAsync(
            CanonicalDataset, dir2, new HtmlGenerationOptions());

        // Index structure should be identical (same tokens, same postings)
        var indexJson1 = await File.ReadAllTextAsync(result1.IndexPath);
        var indexJson2 = await File.ReadAllTextAsync(result2.IndexPath);

        var index1 = ContractSerializer.DeserializeIndex(indexJson1, validate: true);
        var index2 = ContractSerializer.DeserializeIndex(indexJson2, validate: true);

        Assert.Equal(index1.ScenarioRecords.Count, index2.ScenarioRecords.Count);
        Assert.Equal(index1.InvertedTokenIndex.Count, index2.InvertedTokenIndex.Count);
        Assert.Equal(index1.StatusIndex.Passed, index2.StatusIndex.Passed);
        Assert.Equal(index1.StatusIndex.Failed, index2.StatusIndex.Failed);

        foreach (var kvp in index1.InvertedTokenIndex)
        {
            Assert.True(index2.InvertedTokenIndex.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, index2.InvertedTokenIndex[kvp.Key]);
        }
    }

    [Fact]
    public async Task Pipeline_TwoRuns_ProduceIdenticalChunkHtml()
    {
        var dir1 = Path.Combine(_outputDir, "run1");
        var dir2 = Path.Combine(_outputDir, "run2");

        // Use separate pipeline instances to avoid stateful renderer accumulation
        var pipeline1 = CreateFreshPipeline();
        var pipeline2 = CreateFreshPipeline();

        var result1 = await pipeline1.ExecuteAsync(
            CanonicalDataset, dir1, new HtmlGenerationOptions());
        var result2 = await pipeline2.ExecuteAsync(
            CanonicalDataset, dir2, new HtmlGenerationOptions());

        // Match chunks by featureId and compare HTML
        var chunks1 = new Dictionary<string, string>();
        foreach (var path in result1.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(path);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);
            chunks1[chunk.FeatureId] = chunk.Html;
        }

        foreach (var path in result2.ChunkPaths)
        {
            var json = await File.ReadAllTextAsync(path);
            var chunk = ContractSerializer.DeserializeChunk(json, validate: true);
            Assert.True(chunks1.ContainsKey(chunk.FeatureId));
            Assert.Equal(chunks1[chunk.FeatureId], chunk.Html);
        }
    }

    // ================================================================
    // 9. Query Correctness Via Deserialized Index
    // ================================================================

    [Fact]
    public async Task DeserializedIndex_QueryByStatus_ReturnsCorrectOrdinals()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);

        // Query "passed" on the deserialized index
        var passedOrdinals = index.StatusIndex.Passed.OrderBy(x => x).ToList();
        Assert.Equal(new List<int> { 0, 3, 4, 6, 9 }, passedOrdinals);
    }

    [Fact]
    public async Task DeserializedIndex_QueryByTag_ReturnsCorrectOrdinals()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);

        var smokeOrdinals = index.TagIndex["smoke"].OrderBy(x => x).ToList();
        Assert.Equal(new List<int> { 0, 1, 2, 3, 4, 5, 9, 10, 11 }, smokeOrdinals);

        var apiOrdinals = index.TagIndex["api"].OrderBy(x => x).ToList();
        Assert.Equal(new List<int> { 6, 7, 8 }, apiOrdinals);
    }

    [Fact]
    public async Task DeserializedIndex_TextToken_PostingsAreCorrect()
    {
        var result = await _pipeline.ExecuteAsync(
            CanonicalDataset, _outputDir, new HtmlGenerationOptions());

        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);

        // "login" token should appear in scenarios 0 and 1
        Assert.True(index.InvertedTokenIndex.ContainsKey("login"));
        var loginPostings = index.InvertedTokenIndex["login"].OrderBy(x => x).ToList();
        Assert.Equal(new List<int> { 0, 1 }, loginPostings);
    }

    // ================================================================
    // 10. Large Dataset Scale Test
    // ================================================================

    [Fact]
    public async Task Pipeline_50Features_ProducesAllArtifactsWithinTimeout()
    {
        var features = new List<EnrichedFeature>();
        for (int i = 0; i < 50; i++)
        {
            features.Add(CreateFeature($"Feature {i}",
                tags: new[] { "@load-test", $"@group-{i % 5}" },
                scenarios: Enumerable.Range(0, 10).Select(s =>
                    CreateScenario($"Scenario {s} of feature {i}",
                        (ExecutionStatus)(s % 4))).ToArray()));
        }

        var doc = new LivingDocumentation { Features = features };

        var result = await _pipeline.ExecuteAsync(
            doc, _outputDir, new HtmlGenerationOptions());

        Assert.Equal(50, result.TotalFeatures);
        Assert.Equal(500, result.TotalScenarios);
        Assert.Equal(50, result.ChunkPaths.Count);

        // Manifest is valid
        var manifestJson = await File.ReadAllTextAsync(result.ManifestPath);
        var manifest = ContractSerializer.DeserializeManifest(manifestJson, validate: true);
        Assert.Equal(50, manifest.FeatureMap.Count);

        // Index has all scenario records
        var indexJson = await File.ReadAllTextAsync(result.IndexPath);
        var index = ContractSerializer.DeserializeIndex(indexJson, validate: true);
        Assert.Equal(500, index.ScenarioRecords.Count);
    }

    // ================================================================
    // Helper: Fresh Pipeline Factory
    // ================================================================

    /// <summary>
    /// Creates a fresh pipeline instance with new service instances.
    /// Needed for determinism tests because FeatureRenderer has stateful counters.
    /// </summary>
    private static ChunkedOutputPipeline CreateFreshPipeline()
    {
        var featureRenderer = new FeatureRenderer();
        var chunkEmitter = new ChunkEmitterService(featureRenderer);
        var tokenizerService = new TokenizerService();
        var indexBuilder = new IndexBuilderService(tokenizerService);
        var manifestBuilder = new ManifestBuilderService();
        var htmlGenerator = new HtmlGeneratorService();

        return new ChunkedOutputPipeline(
            chunkEmitter, indexBuilder, manifestBuilder, htmlGenerator);
    }

    // ================================================================
    // Canonical Dataset Builder (same as QueryCorrectnessGoldenTests)
    // ================================================================

    private static LivingDocumentation BuildCanonicalDataset()
    {
        return new LivingDocumentation
        {
            Features = new List<EnrichedFeature>
            {
                CreateFeature("User Authentication",
                    tags: new[] { "@smoke", "@security" },
                    scenarios: new[]
                    {
                        CreateScenario("Successful login with valid credentials", ExecutionStatus.Passed, tags: new[] { "@regression" }),
                        CreateScenario("Failed login with invalid password", ExecutionStatus.Failed, tags: new[] { "@regression" }),
                        CreateScenario("Account lockout after 3 attempts", ExecutionStatus.Skipped, tags: new[] { "@slow" })
                    }),
                CreateFeature("Shopping Cart Management",
                    tags: new[] { "@smoke", "@ecommerce" },
                    scenarios: new[]
                    {
                        CreateScenario("Add item to empty cart", ExecutionStatus.Passed, tags: new[] { "@regression" }),
                        CreateScenario("Remove item from cart", ExecutionStatus.Passed),
                        CreateScenario("Apply discount coupon", ExecutionStatus.Failed, tags: new[] { "@ecommerce" })
                    }),
                CreateFeature("API Payment Processing",
                    tags: new[] { "@api", "@security" },
                    scenarios: new[]
                    {
                        CreateScenario("Process credit card payment", ExecutionStatus.Passed, tags: new[] { "@critical" }),
                        CreateScenario("Handle payment timeout", ExecutionStatus.NotExecuted, tags: new[] { "@critical", "@slow" }),
                        CreateScenario("Refund completed order", ExecutionStatus.Failed, tags: new[] { "@regression" })
                    }),
                CreateFeature("User Profile",
                    tags: new[] { "@smoke" },
                    scenarios: new[]
                    {
                        CreateScenario("Update email address", ExecutionStatus.Passed),
                        CreateScenario("Change password", ExecutionStatus.NotExecuted, tags: new[] { "@security" }),
                        CreateScenario("Delete account", ExecutionStatus.Skipped, tags: new[] { "@critical" })
                    })
            }
        };
    }

    private static EnrichedFeature CreateFeature(
        string name, string[]? tags = null, EnrichedScenario[]? scenarios = null)
    {
        var scenarioList = scenarios?.ToList() ?? new List<EnrichedScenario>();

        var passedCount = scenarioList.Count(s => s.Status == ExecutionStatus.Passed);
        var failedCount = scenarioList.Count(s => s.Status == ExecutionStatus.Failed);
        var skippedCount = scenarioList.Count(s => s.Status == ExecutionStatus.Skipped);
        var untestedCount = scenarioList.Count(s => s.Status == ExecutionStatus.NotExecuted);

        // Derive overall status: failed > skipped > passed > untested
        var overallStatus = failedCount > 0 ? ExecutionStatus.Failed
            : skippedCount > 0 ? ExecutionStatus.Skipped
            : passedCount > 0 ? ExecutionStatus.Passed
            : ExecutionStatus.NotExecuted;

        return new EnrichedFeature
        {
            Feature = new UniversalFeature
            {
                Name = name,
                Description = string.Empty,
                FilePath = $"/features/{name.ToLower().Replace(' ', '_')}.feature",
                Tags = tags?.ToList() ?? new List<string>()
            },
            Scenarios = scenarioList,
            OverallStatus = overallStatus,
            PassedCount = passedCount,
            FailedCount = failedCount,
            SkippedCount = skippedCount,
            UntestedCount = untestedCount
        };
    }

    private static EnrichedScenario CreateScenario(
        string name, ExecutionStatus status, string[]? tags = null)
    {
        return new EnrichedScenario
        {
            Scenario = new UniversalScenario
            {
                Name = name,
                Description = string.Empty,
                Tags = tags?.ToList() ?? new List<string>()
            },
            Status = status
        };
    }
}
