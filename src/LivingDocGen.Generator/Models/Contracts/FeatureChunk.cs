namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Individual feature chunk containing the rendered HTML and metadata.
/// Fetched on demand when a feature is selected in the report UI.
/// </summary>
public class FeatureChunk : ContractBase
{
    /// <summary>
    /// Deterministic unique identifier for the feature (matches manifest entry).
    /// </summary>
    public string FeatureId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the feature.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Aggregated execution status of the feature.
    /// </summary>
    public string Status { get; set; } = "untested";

    /// <summary>
    /// SHA-256 hash of this chunk's content (excluding the hash field itself).
    /// Used for integrity validation on load.
    /// </summary>
    public string ChunkHash { get; set; } = string.Empty;

    /// <summary>
    /// Schema version of this specific chunk (may differ from manifest if incrementally rebuilt).
    /// </summary>
    public string ChunkVersion { get; set; } = ContractVersion.SchemaVersion;

    /// <summary>
    /// Pre-rendered HTML body for this feature's documentation.
    /// </summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>
    /// Summary statistics for scenarios within this feature.
    /// </summary>
    public ChunkScenarioSummary ScenarioSummary { get; set; } = new ChunkScenarioSummary();

    /// <summary>
    /// Content statistics for virtualization decisions.
    /// </summary>
    public ChunkContentStats ContentStats { get; set; } = new ChunkContentStats();

    /// <summary>
    /// Rendering hints for the runtime (thresholds, policies).
    /// </summary>
    public ChunkRenderHints RenderHints { get; set; } = new ChunkRenderHints();
}

/// <summary>
/// Scenario count summary for a single feature chunk.
/// </summary>
public class ChunkScenarioSummary
{
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Untested { get; set; }
}

/// <summary>
/// Content statistics for guiding virtualization decisions.
/// </summary>
public class ChunkContentStats
{
    /// <summary>Maximum number of rows in any data table within this feature.</summary>
    public int MaxTableRows { get; set; }

    /// <summary>Maximum number of columns in any data table within this feature.</summary>
    public int MaxTableColumns { get; set; }

    /// <summary>Total number of scenario outline example rows across all outlines.</summary>
    public int OutlineExamples { get; set; }
}

/// <summary>
/// Rendering hints for the runtime, guiding virtualization and chunking behavior.
/// </summary>
public class ChunkRenderHints
{
    /// <summary>Scenario count threshold above which virtualization should activate.</summary>
    public int VirtualizationThreshold { get; set; } = 200;

    /// <summary>Table row threshold above which row chunking should activate.</summary>
    public int TableChunkingThreshold { get; set; } = 200;
}
