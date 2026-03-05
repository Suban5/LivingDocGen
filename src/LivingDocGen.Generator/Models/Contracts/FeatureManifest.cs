using System;
using System.Collections.Generic;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Report-level metadata and chunk map.
/// This is the entry point file for the chunked output mode.
/// The runtime loads this first to discover index and feature chunks.
/// </summary>
public class FeatureManifest : ContractBase
{
    /// <summary>
    /// UTC timestamp when this manifest was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of features in the report.
    /// </summary>
    public int TotalFeatures { get; set; }

    /// <summary>
    /// Total number of scenarios across all features.
    /// </summary>
    public int TotalScenarios { get; set; }

    /// <summary>
    /// Relative path to the feature-index.json file.
    /// </summary>
    public string IndexFile { get; set; } = "feature-index.json";

    /// <summary>
    /// SHA-256 hash of the index file content for integrity validation.
    /// </summary>
    public string IndexHash { get; set; } = string.Empty;

    /// <summary>
    /// Chunking strategy used (e.g., "per-feature", "hybrid").
    /// </summary>
    public string ChunkStrategy { get; set; } = "per-feature";

    /// <summary>
    /// Total number of chunk files emitted.
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// Runtime capability flags indicating what the report supports.
    /// </summary>
    public ManifestCapabilities Capabilities { get; set; } = new ManifestCapabilities();

    /// <summary>
    /// Ordered list of feature entries with chunk mapping metadata.
    /// </summary>
    public List<FeatureManifestEntry> FeatureMap { get; set; } = new List<FeatureManifestEntry>();
}

/// <summary>
/// Per-feature entry in the manifest, mapping feature identity to its chunk file.
/// </summary>
public class FeatureManifestEntry
{
    /// <summary>
    /// Deterministic unique identifier for the feature (e.g., hash of file path + name).
    /// </summary>
    public string FeatureId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the feature.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Original file path of the .feature file (relative to features root).
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Aggregated execution status of the feature.
    /// </summary>
    public string Status { get; set; } = "untested";

    /// <summary>
    /// Number of scenarios in this feature.
    /// </summary>
    public int ScenarioCount { get; set; }

    /// <summary>
    /// Relative path to the feature chunk JSON file (e.g., "features/abc123.json").
    /// </summary>
    public string ChunkFile { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the chunk file content for integrity validation.
    /// </summary>
    public string ChunkHash { get; set; } = string.Empty;

    /// <summary>
    /// Estimated size in bytes of the chunk file, for prefetch budget decisions.
    /// </summary>
    public long EstimatedBytes { get; set; }

    /// <summary>
    /// Tags associated with this feature.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Scenario ordinal range in the global index (start inclusive, end exclusive).
    /// Used for fast index lookups.
    /// </summary>
    public ScenarioRange ScenarioRange { get; set; } = new ScenarioRange();
}

/// <summary>
/// Represents a range of scenario ordinals in the global index.
/// </summary>
public class ScenarioRange
{
    /// <summary>Start ordinal (inclusive).</summary>
    public int Start { get; set; }

    /// <summary>End ordinal (exclusive).</summary>
    public int End { get; set; }
}

/// <summary>
/// Capability flags indicating runtime features supported by this report.
/// </summary>
public class ManifestCapabilities
{
    /// <summary>Whether the report includes a search worker index.</summary>
    public bool WorkerSearch { get; set; } = true;

    /// <summary>Whether scenario-level virtualization is supported.</summary>
    public bool Virtualization { get; set; } = true;

    /// <summary>Whether table row chunking is supported.</summary>
    public bool TableChunking { get; set; } = true;
}
