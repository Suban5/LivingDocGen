using System;
using System.Diagnostics;

namespace LivingDocGen.Generator.Services.Telemetry;

/// <summary>
/// Lightweight stopwatch-based collector that records phase timings and memory snapshots
/// into a <see cref="GenerationMetrics"/> instance.
///
/// Usage:
///   var collector = new GenerationTelemetry();
///   collector.Start(featureCount, testFileCount);
///   // ... do work ...
///   collector.MarkPhaseEnd("Parsing");
///   // ... next phase ...
///   collector.MarkPhaseEnd("Enrichment");
///   collector.Complete(outputSizeBytes);
///   var metrics = collector.Metrics;
/// </summary>
public class GenerationTelemetry
{
    private readonly Stopwatch _totalStopwatch = new Stopwatch();
    private readonly Stopwatch _phaseStopwatch = new Stopwatch();
    private string _currentPhase;

    /// <summary>
    /// The metrics being collected. Available after <see cref="Complete"/>.
    /// </summary>
    public GenerationMetrics Metrics { get; } = new GenerationMetrics();

    /// <summary>
    /// Begin a timed generation run.
    /// </summary>
    public void Start(int featureFileCount, int testResultFileCount)
    {
        Metrics.FeatureFileCount = featureFileCount;
        Metrics.TestResultFileCount = testResultFileCount;
        Metrics.StartedAtUtc = DateTime.UtcNow;
        Metrics.MemoryBeforeBytes = GC.GetTotalMemory(forceFullCollection: false);

        _totalStopwatch.Restart();
        _phaseStopwatch.Restart();
        _currentPhase = null;
    }

    /// <summary>
    /// Record the elapsed time for the current phase and start the next one.
    /// Well-known phase names are mapped to typed properties; others go to CustomPhasesMs.
    /// </summary>
    public void MarkPhaseEnd(string phaseName)
    {
        var elapsedMs = _phaseStopwatch.Elapsed.TotalMilliseconds;
        _phaseStopwatch.Restart();

        switch (phaseName)
        {
            case "FileDiscovery":
                Metrics.FileDiscoveryMs = elapsedMs;
                break;
            case "Parsing":
                Metrics.ParsingMs = elapsedMs;
                Metrics.MemoryAfterParsingBytes = GC.GetTotalMemory(false);
                break;
            case "TestResultParsing":
                Metrics.TestResultParsingMs = elapsedMs;
                break;
            case "Enrichment":
                Metrics.EnrichmentMs = elapsedMs;
                Metrics.MemoryAfterEnrichmentBytes = GC.GetTotalMemory(false);
                break;
            case "HtmlGeneration":
                Metrics.HtmlGenerationMs = elapsedMs;
                Metrics.MemoryAfterHtmlGenBytes = GC.GetTotalMemory(false);
                break;
            case "FileWrite":
                Metrics.FileWriteMs = elapsedMs;
                break;
            default:
                Metrics.CustomPhasesMs[phaseName] = elapsedMs;
                break;
        }

        _currentPhase = phaseName;
    }

    /// <summary>
    /// Record a memory snapshot at the current point.
    /// </summary>
    public void SnapshotMemory(string label)
    {
        var mem = GC.GetTotalMemory(false);
        if (mem > Metrics.PeakMemoryBytes)
            Metrics.PeakMemoryBytes = mem;
    }

    /// <summary>
    /// Record output dimensions after HTML generation.
    /// </summary>
    public void RecordOutputDimensions(int featureCount, int scenarioCount, int stepCount, long outputSizeBytes)
    {
        Metrics.ParsedFeatureCount = featureCount;
        Metrics.ParsedScenarioCount = scenarioCount;
        Metrics.ParsedStepCount = stepCount;
        Metrics.OutputSizeBytes = outputSizeBytes;
    }

    /// <summary>
    /// Finalize the metrics collection.
    /// </summary>
    public void Complete(long outputSizeBytes = 0)
    {
        _totalStopwatch.Stop();
        Metrics.TotalMs = _totalStopwatch.Elapsed.TotalMilliseconds;
        Metrics.CompletedAtUtc = DateTime.UtcNow;
        if (outputSizeBytes > 0)
            Metrics.OutputSizeBytes = outputSizeBytes;

        var peakCandidate = GC.GetTotalMemory(false);
        if (peakCandidate > Metrics.PeakMemoryBytes)
            Metrics.PeakMemoryBytes = peakCandidate;
    }
}
