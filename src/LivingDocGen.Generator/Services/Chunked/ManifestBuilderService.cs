using System;
using System.Collections.Generic;
using System.Linq;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Services.Chunked;

/// <summary>
/// Builds the <see cref="FeatureManifest"/> from enriched documentation,
/// mapping features to their chunk files with deterministic IDs and content metadata.
/// </summary>
public interface IManifestBuilderService
{
    /// <summary>
    /// Builds a <see cref="FeatureManifest"/> from enriched features and chunk metadata.
    /// </summary>
    /// <param name="documentation">The enriched living documentation.</param>
    /// <param name="buildId">Build identifier for this generation run.</param>
    /// <param name="chunkMetadata">Per-feature chunk metadata (hash, estimated bytes) keyed by feature ID.</param>
    /// <returns>A fully populated manifest.</returns>
    FeatureManifest BuildManifest(
        LivingDocumentation documentation,
        string buildId,
        Dictionary<string, ChunkMetadata> chunkMetadata);
}

/// <summary>
/// Metadata about a serialized feature chunk, populated after chunk emission.
/// </summary>
public class ChunkMetadata
{
    /// <summary>SHA-256 hash of the serialized chunk JSON.</summary>
    public string ChunkHash { get; set; } = string.Empty;

    /// <summary>Size in bytes of the serialized chunk JSON.</summary>
    public long EstimatedBytes { get; set; }
}

/// <summary>
/// Default implementation that constructs the manifest
/// using <see cref="ContractHashValidator"/> for deterministic feature IDs.
/// </summary>
public class ManifestBuilderService : IManifestBuilderService
{
    /// <inheritdoc/>
    public FeatureManifest BuildManifest(
        LivingDocumentation documentation,
        string buildId,
        Dictionary<string, ChunkMetadata> chunkMetadata)
    {
        if (documentation == null)
            throw new ArgumentNullException(nameof(documentation));
        if (chunkMetadata == null)
            throw new ArgumentNullException(nameof(chunkMetadata));

        var manifest = new FeatureManifest
        {
            BuildId = buildId,
            GeneratedAt = DateTime.UtcNow,
            TotalFeatures = documentation.Features.Count,
            TotalScenarios = documentation.Features.Sum(f => f.Scenarios.Count),
            IndexFile = "feature-index.json",
            ChunkStrategy = "per-feature",
            ChunkCount = documentation.Features.Count,
            Capabilities = new ManifestCapabilities
            {
                WorkerSearch = true,
                Virtualization = true,
                TableChunking = true
            }
        };

        int scenarioOrdinal = 0;

        for (int i = 0; i < documentation.Features.Count; i++)
        {
            var feature = documentation.Features[i];
            var featureId = ContractHashValidator.GenerateFeatureId(
                feature.Feature.FilePath ?? string.Empty,
                feature.Feature.Name);

            var scenarioCount = feature.Scenarios.Count;
            var chunkFile = $"features/{featureId}.json";

            // Get chunk metadata if available
            var metadata = chunkMetadata.ContainsKey(featureId)
                ? chunkMetadata[featureId]
                : new ChunkMetadata();

            // Collect tags
            var tags = feature.Feature.Tags?.ToList() ?? new List<string>();

            var entry = new FeatureManifestEntry
            {
                FeatureId = featureId,
                Name = feature.Feature.Name,
                FilePath = feature.Feature.FilePath ?? string.Empty,
                Status = MapStatus(feature.OverallStatus),
                ScenarioCount = scenarioCount,
                ChunkFile = chunkFile,
                ChunkHash = metadata.ChunkHash,
                EstimatedBytes = metadata.EstimatedBytes,
                Tags = tags,
                ScenarioRange = new ScenarioRange
                {
                    Start = scenarioOrdinal,
                    End = scenarioOrdinal + scenarioCount
                }
            };

            manifest.FeatureMap.Add(entry);
            scenarioOrdinal += scenarioCount;
        }

        return manifest;
    }

    /// <summary>
    /// Maps the enriched execution status to a string status for manifest entries.
    /// </summary>
    private static string MapStatus(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "passed",
            ExecutionStatus.Failed => "failed",
            ExecutionStatus.Skipped => "skipped",
            _ => "untested"
        };
    }
}
