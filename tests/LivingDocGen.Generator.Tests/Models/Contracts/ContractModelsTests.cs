using System.Collections.Generic;
using LivingDocGen.Generator.Models.Contracts;
using Xunit;

namespace LivingDocGen.Generator.Tests.Models.Contracts;

public class ContractModelsTests
{
    #region FeatureManifest Defaults

    [Fact]
    public void FeatureManifest_InitializesWithDefaults()
    {
        var manifest = new FeatureManifest();

        Assert.Equal(ContractVersion.SchemaVersion, manifest.SchemaVersion);
        Assert.Equal(ContractVersion.GeneratorVersion, manifest.GeneratorVersion);
        Assert.Equal(ContractVersion.CompatibilityMinVersion, manifest.CompatibilityMinVersion);
        Assert.NotNull(manifest.FeatureMap);
        Assert.Empty(manifest.FeatureMap);
        Assert.Equal("feature-index.json", manifest.IndexFile);
        Assert.Equal("per-feature", manifest.ChunkStrategy);
        Assert.NotNull(manifest.Capabilities);
        Assert.NotNull(manifest.Extensions);
    }

    [Fact]
    public void ManifestCapabilities_DefaultsAllTrue()
    {
        var caps = new ManifestCapabilities();

        Assert.True(caps.WorkerSearch);
        Assert.True(caps.Virtualization);
        Assert.True(caps.TableChunking);
    }

    [Fact]
    public void FeatureManifestEntry_InitializesWithDefaults()
    {
        var entry = new FeatureManifestEntry();

        Assert.Equal(string.Empty, entry.FeatureId);
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.FilePath);
        Assert.Equal("untested", entry.Status);
        Assert.Equal(0, entry.ScenarioCount);
        Assert.Equal(string.Empty, entry.ChunkFile);
        Assert.Equal(string.Empty, entry.ChunkHash);
        Assert.Equal(0, entry.EstimatedBytes);
        Assert.NotNull(entry.Tags);
        Assert.Empty(entry.Tags);
        Assert.NotNull(entry.ScenarioRange);
    }

    [Fact]
    public void ScenarioRange_InitializesWithZeros()
    {
        var range = new ScenarioRange();
        Assert.Equal(0, range.Start);
        Assert.Equal(0, range.End);
    }

    #endregion

    #region FeatureIndex Defaults

    [Fact]
    public void FeatureIndex_InitializesWithDefaults()
    {
        var index = new FeatureIndex();

        Assert.Equal(ContractVersion.SchemaVersion, index.SchemaVersion);
        Assert.NotNull(index.ScenarioRecords);
        Assert.Empty(index.ScenarioRecords);
        Assert.NotNull(index.InvertedTokenIndex);
        Assert.Empty(index.InvertedTokenIndex);
        Assert.NotNull(index.StatusIndex);
        Assert.NotNull(index.TagIndex);
        Assert.Empty(index.TagIndex);
        Assert.NotNull(index.Normalization);
    }

    [Fact]
    public void ScenarioRecord_InitializesWithDefaults()
    {
        var record = new ScenarioRecord();

        Assert.Equal(0, record.ScenarioOrdinal);
        Assert.Equal(0, record.FeatureOrdinal);
        Assert.Equal(string.Empty, record.Name);
        Assert.Equal("untested", record.Status);
        Assert.NotNull(record.Tokens);
        Assert.Empty(record.Tokens);
        Assert.NotNull(record.Tags);
        Assert.Empty(record.Tags);
    }

    [Fact]
    public void StatusIndex_InitializesWithEmptyLists()
    {
        var status = new StatusIndex();

        Assert.NotNull(status.Passed);
        Assert.NotNull(status.Failed);
        Assert.NotNull(status.Skipped);
        Assert.NotNull(status.Untested);
        Assert.Empty(status.Passed);
        Assert.Empty(status.Failed);
        Assert.Empty(status.Skipped);
        Assert.Empty(status.Untested);
    }

    [Fact]
    public void TokenNormalization_InitializesWithDefaults()
    {
        var norm = new TokenNormalization();

        Assert.True(norm.CaseFolding);
        Assert.True(norm.UnicodeFolding);
        Assert.Equal(" _-/.@#", norm.Delimiters);
        Assert.Equal("invariant", norm.Locale);
    }

    #endregion

    #region FeatureChunk Defaults

    [Fact]
    public void FeatureChunk_InitializesWithDefaults()
    {
        var chunk = new FeatureChunk();

        Assert.Equal(ContractVersion.SchemaVersion, chunk.SchemaVersion);
        Assert.Equal(string.Empty, chunk.FeatureId);
        Assert.Equal(string.Empty, chunk.Name);
        Assert.Equal("untested", chunk.Status);
        Assert.Equal(string.Empty, chunk.Html);
        Assert.Equal(string.Empty, chunk.ChunkHash);
        Assert.Equal(ContractVersion.SchemaVersion, chunk.ChunkVersion);
        Assert.NotNull(chunk.ScenarioSummary);
        Assert.NotNull(chunk.ContentStats);
        Assert.NotNull(chunk.RenderHints);
    }

    [Fact]
    public void ChunkScenarioSummary_InitializesWithZeros()
    {
        var summary = new ChunkScenarioSummary();

        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.Passed);
        Assert.Equal(0, summary.Failed);
        Assert.Equal(0, summary.Skipped);
        Assert.Equal(0, summary.Untested);
    }

    [Fact]
    public void ChunkContentStats_InitializesWithZeros()
    {
        var stats = new ChunkContentStats();

        Assert.Equal(0, stats.MaxTableRows);
        Assert.Equal(0, stats.MaxTableColumns);
        Assert.Equal(0, stats.OutlineExamples);
    }

    [Fact]
    public void ChunkRenderHints_InitializesWithThresholds()
    {
        var hints = new ChunkRenderHints();

        Assert.Equal(200, hints.VirtualizationThreshold);
        Assert.Equal(200, hints.TableChunkingThreshold);
    }

    #endregion

    #region WorkerMessage Defaults

    [Fact]
    public void WorkerMessage_InitializesWithDefaults()
    {
        var message = new WorkerMessage();

        Assert.Equal(WorkerMessage.ProtocolVersion, message.Protocol);
        Assert.Equal(string.Empty, message.RequestId);
        Assert.Equal(0, message.QuerySeq);
        Assert.NotNull(message.Payload);
    }

    [Fact]
    public void WorkerPayload_InitializesWithDefaults()
    {
        var payload = new WorkerPayload();

        Assert.Equal(string.Empty, payload.Query);
        Assert.Equal("all", payload.Status);
        Assert.NotNull(payload.Tags);
        Assert.Empty(payload.Tags);
        Assert.NotNull(payload.ScenarioOrdinals);
        Assert.Empty(payload.ScenarioOrdinals);
        Assert.NotNull(payload.FeatureOrdinals);
        Assert.Empty(payload.FeatureOrdinals);
        Assert.Equal(0, payload.Count);
        Assert.Equal(string.Empty, payload.Message);
        Assert.Null(payload.Stats);
    }

    [Fact]
    public void WorkerMessageType_HasExpectedValues()
    {
        Assert.Equal(0, (int)WorkerMessageType.InitIndex);
        Assert.Equal(1, (int)WorkerMessageType.Ready);
        Assert.Equal(2, (int)WorkerMessageType.Query);
        Assert.Equal(3, (int)WorkerMessageType.QueryCancel);
        Assert.Equal(4, (int)WorkerMessageType.Result);
        Assert.Equal(5, (int)WorkerMessageType.Error);
        Assert.Equal(6, (int)WorkerMessageType.Metrics);
    }

    [Fact]
    public void WorkerQueryStats_InitializesWithZeros()
    {
        var stats = new WorkerQueryStats();

        Assert.Equal(0, stats.EvalMs);
        Assert.Equal(0, stats.CandidateCount);
        Assert.Equal(0, stats.IndexLookups);
    }

    #endregion

    #region ContractBase Extensions

    [Fact]
    public void ContractBase_ExtensionsCanHoldArbitraryData()
    {
        var manifest = new FeatureManifest();
        manifest.Extensions["customFlag"] = true;
        manifest.Extensions["customData"] = "value";

        Assert.True((bool)manifest.Extensions["customFlag"]);
        Assert.Equal("value", manifest.Extensions["customData"]);
    }

    #endregion
}
