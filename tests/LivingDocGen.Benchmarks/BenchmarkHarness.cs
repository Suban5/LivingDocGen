using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Telemetry;
using LivingDocGen.Parser.Services;
using LivingDocGen.TestReporter.Services;
using Newtonsoft.Json;

namespace LivingDocGen.Benchmarks;

/// <summary>
/// Orchestrates end-to-end benchmark runs across Small/Medium/Large profiles,
/// collects <see cref="GenerationMetrics"/>, and writes a comparative report.
/// 
/// Usage in tests:
///   var harness = new BenchmarkHarness(baseOutputDir);
///   var results = await harness.RunAllProfilesAsync();
///   harness.WriteReport(results);
/// </summary>
public class BenchmarkHarness
{
    private readonly string _baseOutputDir;

    public BenchmarkHarness(string baseOutputDir)
    {
        _baseOutputDir = baseOutputDir ?? throw new ArgumentNullException(nameof(baseOutputDir));
        Directory.CreateDirectory(_baseOutputDir);
    }

    /// <summary>
    /// Run a single benchmark for a given profile.
    /// </summary>
    public async Task<GenerationMetrics> RunBenchmarkAsync(BenchmarkProfile profile, int warmupRuns = 0)
    {
        var profileDir = Path.Combine(_baseOutputDir, profile.ToString().ToLower());
        Directory.CreateDirectory(profileDir);

        // 1. Generate synthetic dataset
        Console.WriteLine($"[Benchmark] Generating {profile} dataset...");
        var genSw = Stopwatch.StartNew();
        var (featuresDir, testResultFile) = SyntheticDatasetGenerator.GenerateProfile(profileDir, profile);
        genSw.Stop();
        Console.WriteLine($"[Benchmark] Dataset generated in {genSw.ElapsedMilliseconds}ms");

        // 2. Discover files
        var featureFiles = Directory.GetFiles(featuresDir, "*.feature", SearchOption.AllDirectories).ToList();
        var testResultFiles = new List<string> { testResultFile };

        // 3. Warmup runs (no telemetry)
        for (int w = 0; w < warmupRuns; w++)
        {
            Console.WriteLine($"[Benchmark] Warmup run {w + 1}/{warmupRuns}...");
            var warmupGen = CreateGenerator();
            await warmupGen.GenerateAsync(featureFiles, testResultFiles).ConfigureAwait(false);
        }

        // 4. Measured run
        Console.WriteLine($"[Benchmark] Measured run for {profile} ({featureFiles.Count} features)...");
        var generator = CreateGenerator();
        var telemetry = new GenerationTelemetry();
        generator.Telemetry = telemetry;

        var outputPath = Path.Combine(profileDir, "living-documentation.html");
        await generator.GenerateToFileAsync(
            featureFiles,
            testResultFiles,
            outputPath,
            $"Benchmark {profile}").ConfigureAwait(false);

        Console.WriteLine($"[Benchmark] {profile} complete: {telemetry.Metrics.ToSummaryLine()}");

        // 5. Save individual metrics
        var metricsPath = Path.Combine(profileDir, "metrics.json");
        MetricsReportWriter.WriteJsonReport(telemetry.Metrics, metricsPath);
        MetricsReportWriter.WriteConsoleReport(telemetry.Metrics);

        return telemetry.Metrics;
    }

    /// <summary>
    /// Run all three profiles and return results.
    /// </summary>
    public async Task<Dictionary<BenchmarkProfile, GenerationMetrics>> RunAllProfilesAsync(int warmupRuns = 0)
    {
        var results = new Dictionary<BenchmarkProfile, GenerationMetrics>();

        foreach (var profile in new[] { BenchmarkProfile.Small, BenchmarkProfile.Medium, BenchmarkProfile.Large })
        {
            // Force GC between profiles to get cleaner memory baselines
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            results[profile] = await RunBenchmarkAsync(profile, warmupRuns).ConfigureAwait(false);
        }

        return results;
    }

    /// <summary>
    /// Write a comparative report across all profiles to a markdown file.
    /// </summary>
    public void WriteReport(Dictionary<BenchmarkProfile, GenerationMetrics> results)
    {
        var reportPath = Path.Combine(_baseOutputDir, "BENCHMARK_REPORT.md");
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Generation Performance Benchmark Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Machine:** {Environment.MachineName} / {Environment.OSVersion}");
        sb.AppendLine($"**Processors:** {Environment.ProcessorCount}");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Small (300) | Medium (1200) | Large (3000) |");
        sb.AppendLine("|--------|-------------|---------------|--------------|");

        string Cell(BenchmarkProfile p, Func<GenerationMetrics, string> fmt) =>
            results.ContainsKey(p) ? fmt(results[p]) : "N/A";

        sb.AppendLine($"| Features | {Cell(BenchmarkProfile.Small, m => m.ParsedFeatureCount.ToString())} | {Cell(BenchmarkProfile.Medium, m => m.ParsedFeatureCount.ToString())} | {Cell(BenchmarkProfile.Large, m => m.ParsedFeatureCount.ToString())} |");
        sb.AppendLine($"| Scenarios | {Cell(BenchmarkProfile.Small, m => m.ParsedScenarioCount.ToString())} | {Cell(BenchmarkProfile.Medium, m => m.ParsedScenarioCount.ToString())} | {Cell(BenchmarkProfile.Large, m => m.ParsedScenarioCount.ToString())} |");
        sb.AppendLine($"| Parse (ms) | {Cell(BenchmarkProfile.Small, m => m.ParsingMs.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.ParsingMs.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.ParsingMs.ToString("F0"))} |");
        sb.AppendLine($"| Enrich (ms) | {Cell(BenchmarkProfile.Small, m => m.EnrichmentMs.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.EnrichmentMs.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.EnrichmentMs.ToString("F0"))} |");
        sb.AppendLine($"| HTML gen (ms) | {Cell(BenchmarkProfile.Small, m => m.HtmlGenerationMs.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.HtmlGenerationMs.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.HtmlGenerationMs.ToString("F0"))} |");
        sb.AppendLine($"| Total (ms) | {Cell(BenchmarkProfile.Small, m => m.TotalMs.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.TotalMs.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.TotalMs.ToString("F0"))} |");
        sb.AppendLine($"| Output (KB) | {Cell(BenchmarkProfile.Small, m => (m.OutputSizeBytes / 1024.0).ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => (m.OutputSizeBytes / 1024.0).ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => (m.OutputSizeBytes / 1024.0).ToString("F0"))} |");
        sb.AppendLine($"| Peak Mem (MB) | {Cell(BenchmarkProfile.Small, m => (m.PeakMemoryBytes / (1024.0 * 1024)).ToString("F1"))} | {Cell(BenchmarkProfile.Medium, m => (m.PeakMemoryBytes / (1024.0 * 1024)).ToString("F1"))} | {Cell(BenchmarkProfile.Large, m => (m.PeakMemoryBytes / (1024.0 * 1024)).ToString("F1"))} |");
        sb.AppendLine($"| Parse throughput (feat/s) | {Cell(BenchmarkProfile.Small, m => m.ParsingThroughputFeaturesPerSec.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.ParsingThroughputFeaturesPerSec.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.ParsingThroughputFeaturesPerSec.ToString("F0"))} |");
        sb.AppendLine($"| HTML throughput (feat/s) | {Cell(BenchmarkProfile.Small, m => m.HtmlGenThroughputFeaturesPerSec.ToString("F0"))} | {Cell(BenchmarkProfile.Medium, m => m.HtmlGenThroughputFeaturesPerSec.ToString("F0"))} | {Cell(BenchmarkProfile.Large, m => m.HtmlGenThroughputFeaturesPerSec.ToString("F0"))} |");

        sb.AppendLine();
        sb.AppendLine("## Memory Breakdown");
        sb.AppendLine();
        sb.AppendLine("| Phase | Small (MB) | Medium (MB) | Large (MB) |");
        sb.AppendLine("|-------|-----------|-------------|------------|");

        string MemCell(BenchmarkProfile p, Func<GenerationMetrics, long> getter) =>
            results.ContainsKey(p) ? (getter(results[p]) / (1024.0 * 1024)).ToString("F1") : "N/A";

        sb.AppendLine($"| Before | {MemCell(BenchmarkProfile.Small, m => m.MemoryBeforeBytes)} | {MemCell(BenchmarkProfile.Medium, m => m.MemoryBeforeBytes)} | {MemCell(BenchmarkProfile.Large, m => m.MemoryBeforeBytes)} |");
        sb.AppendLine($"| After Parse | {MemCell(BenchmarkProfile.Small, m => m.MemoryAfterParsingBytes)} | {MemCell(BenchmarkProfile.Medium, m => m.MemoryAfterParsingBytes)} | {MemCell(BenchmarkProfile.Large, m => m.MemoryAfterParsingBytes)} |");
        sb.AppendLine($"| After Enrich | {MemCell(BenchmarkProfile.Small, m => m.MemoryAfterEnrichmentBytes)} | {MemCell(BenchmarkProfile.Medium, m => m.MemoryAfterEnrichmentBytes)} | {MemCell(BenchmarkProfile.Large, m => m.MemoryAfterEnrichmentBytes)} |");
        sb.AppendLine($"| After HTML | {MemCell(BenchmarkProfile.Small, m => m.MemoryAfterHtmlGenBytes)} | {MemCell(BenchmarkProfile.Medium, m => m.MemoryAfterHtmlGenBytes)} | {MemCell(BenchmarkProfile.Large, m => m.MemoryAfterHtmlGenBytes)} |");
        sb.AppendLine($"| Peak | {MemCell(BenchmarkProfile.Small, m => m.PeakMemoryBytes)} | {MemCell(BenchmarkProfile.Medium, m => m.PeakMemoryBytes)} | {MemCell(BenchmarkProfile.Large, m => m.PeakMemoryBytes)} |");

        sb.AppendLine();
        sb.AppendLine("## SLO Targets (from Design Doc Section 7.2)");
        sb.AppendLine();
        sb.AppendLine("| Metric | Target | Large Result | Status |");
        sb.AppendLine("|--------|--------|-------------|--------|");

        if (results.ContainsKey(BenchmarkProfile.Large))
        {
            var lg = results[BenchmarkProfile.Large];
            sb.AppendLine($"| Total gen time | Baseline (record) | {lg.TotalMs:F0}ms | 📊 |");
            sb.AppendLine($"| Parse throughput | Baseline (record) | {lg.ParsingThroughputFeaturesPerSec:F0} feat/s | 📊 |");
            sb.AppendLine($"| HTML gen throughput | Baseline (record) | {lg.HtmlGenThroughputFeaturesPerSec:F0} feat/s | 📊 |");
            sb.AppendLine($"| Peak memory | Baseline (record) | {lg.PeakMemoryBytes / (1024.0 * 1024):F1} MB | 📊 |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*This report establishes the **PR-0 baseline**. Future PRs must not regress these numbers.*");

        File.WriteAllText(reportPath, sb.ToString());
        Console.WriteLine($"[Benchmark] Report written to: {reportPath}");
    }

    // --- Factory ---

    private static LivingDocumentationGenerator CreateGenerator()
    {
        return new LivingDocumentationGenerator(
            new UniversalParserService(),
            new TestReportService(),
            new DocumentEnrichmentService(),
            new HtmlGeneratorService());
    }
}
