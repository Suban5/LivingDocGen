using System;
using System.Collections.Generic;

namespace LivingDocGen.Generator.Services.Telemetry;

/// <summary>
/// Captures timing and size metrics for a single generation run.
/// Designed for PR-0 baseline measurement before architecture changes.
/// </summary>
public class GenerationMetrics
{
    // --- Identity ---
    public string RunId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }

    // --- Input dimensions ---
    public int FeatureFileCount { get; set; }
    public int TestResultFileCount { get; set; }

    // --- Phase timings (milliseconds) ---
    public double FileDiscoveryMs { get; set; }
    public double ParsingMs { get; set; }
    public double TestResultParsingMs { get; set; }
    public double EnrichmentMs { get; set; }
    public double HtmlGenerationMs { get; set; }
    public double FileWriteMs { get; set; }
    public double TotalMs { get; set; }

    // --- Output dimensions ---
    public int ParsedFeatureCount { get; set; }
    public int ParsedScenarioCount { get; set; }
    public int ParsedStepCount { get; set; }
    public long OutputSizeBytes { get; set; }

    // --- Memory snapshots (bytes) ---
    public long MemoryBeforeBytes { get; set; }
    public long MemoryAfterParsingBytes { get; set; }
    public long MemoryAfterEnrichmentBytes { get; set; }
    public long MemoryAfterHtmlGenBytes { get; set; }
    public long PeakMemoryBytes { get; set; }

    // --- Derived ---
    public double ParsingThroughputFeaturesPerSec =>
        ParsingMs > 0 ? FeatureFileCount / (ParsingMs / 1000.0) : 0;

    public double HtmlGenThroughputFeaturesPerSec =>
        HtmlGenerationMs > 0 ? ParsedFeatureCount / (HtmlGenerationMs / 1000.0) : 0;

    // --- Custom phase markers for fine-grained measurement ---
    public Dictionary<string, double> CustomPhasesMs { get; set; } = new Dictionary<string, double>();

    /// <summary>
    /// Returns a compact single-line summary suitable for console output.
    /// </summary>
    public string ToSummaryLine()
    {
        return $"[{RunId}] features={ParsedFeatureCount} scenarios={ParsedScenarioCount} " +
               $"parse={ParsingMs:F0}ms enrich={EnrichmentMs:F0}ms html={HtmlGenerationMs:F0}ms " +
               $"total={TotalMs:F0}ms output={OutputSizeBytes / 1024.0:F1}KB " +
               $"peakMem={PeakMemoryBytes / (1024.0 * 1024):F1}MB";
    }
}
