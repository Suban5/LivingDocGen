using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace LivingDocGen.Generator.Services.Telemetry;

/// <summary>
/// Writes <see cref="GenerationMetrics"/> to JSON files and formatted console output.
/// </summary>
public static class MetricsReportWriter
{
    /// <summary>
    /// Write metrics as a pretty-printed JSON file.
    /// </summary>
    public static void WriteJsonReport(GenerationMetrics metrics, string outputPath)
    {
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("outputPath required");

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonConvert.SerializeObject(metrics, Formatting.Indented);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }

    /// <summary>
    /// Print a human-readable metrics report to the console.
    /// </summary>
    public static void WriteConsoleReport(GenerationMetrics m)
    {
        if (m == null) return;

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              GENERATION PERFORMANCE METRICS                 ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  Run ID         : {m.RunId,-40} ║");
        Console.WriteLine($"║  Started        : {m.StartedAtUtc:yyyy-MM-dd HH:mm:ss.fff} UTC               ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  INPUT                                                     ║");
        Console.WriteLine($"║    Feature files : {m.FeatureFileCount,-10}                                ║");
        Console.WriteLine($"║    Test files    : {m.TestResultFileCount,-10}                                ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  PHASE TIMINGS (ms)                                        ║");
        Console.WriteLine($"║    File discovery : {m.FileDiscoveryMs,10:F1}                               ║");
        Console.WriteLine($"║    Parsing        : {m.ParsingMs,10:F1}                               ║");
        Console.WriteLine($"║    Test results   : {m.TestResultParsingMs,10:F1}                               ║");
        Console.WriteLine($"║    Enrichment     : {m.EnrichmentMs,10:F1}                               ║");
        Console.WriteLine($"║    HTML generation: {m.HtmlGenerationMs,10:F1}                               ║");
        Console.WriteLine($"║    File write     : {m.FileWriteMs,10:F1}                               ║");
        Console.WriteLine($"║    ─────────────────────────────                           ║");
        Console.WriteLine($"║    TOTAL          : {m.TotalMs,10:F1}                               ║");

        if (m.CustomPhasesMs.Count > 0)
        {
            Console.WriteLine("║  CUSTOM PHASES                                             ║");
            foreach (var kv in m.CustomPhasesMs)
            {
                Console.WriteLine($"║    {kv.Key,-16}: {kv.Value,10:F1}                               ║");
            }
        }

        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  OUTPUT                                                    ║");
        Console.WriteLine($"║    Features      : {m.ParsedFeatureCount,-10}                                ║");
        Console.WriteLine($"║    Scenarios     : {m.ParsedScenarioCount,-10}                                ║");
        Console.WriteLine($"║    Steps         : {m.ParsedStepCount,-10}                                ║");
        Console.WriteLine($"║    Output size   : {m.OutputSizeBytes / 1024.0,10:F1} KB                          ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  THROUGHPUT                                                ║");
        Console.WriteLine($"║    Parse         : {m.ParsingThroughputFeaturesPerSec,10:F1} features/s               ║");
        Console.WriteLine($"║    HTML gen      : {m.HtmlGenThroughputFeaturesPerSec,10:F1} features/s               ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine("║  MEMORY                                                    ║");
        Console.WriteLine($"║    Before        : {m.MemoryBeforeBytes / (1024.0 * 1024),10:F1} MB                          ║");
        Console.WriteLine($"║    After parse   : {m.MemoryAfterParsingBytes / (1024.0 * 1024),10:F1} MB                          ║");
        Console.WriteLine($"║    After enrich  : {m.MemoryAfterEnrichmentBytes / (1024.0 * 1024),10:F1} MB                          ║");
        Console.WriteLine($"║    After HTML gen: {m.MemoryAfterHtmlGenBytes / (1024.0 * 1024),10:F1} MB                          ║");
        Console.WriteLine($"║    Peak          : {m.PeakMemoryBytes / (1024.0 * 1024),10:F1} MB                          ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }
}
