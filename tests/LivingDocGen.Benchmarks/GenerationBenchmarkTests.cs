using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace LivingDocGen.Benchmarks;

/// <summary>
/// Benchmark tests that generate synthetic datasets and measure generation performance.
/// These are NOT fast unit tests — they exercise the full pipeline at scale.
/// Run selectively: dotnet test --filter "Category=Benchmark"
/// </summary>
[Trait("Category", "Benchmark")]
public class GenerationBenchmarkTests
{
    private readonly ITestOutputHelper _output;
    private readonly string _benchmarkDir;

    public GenerationBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
        _benchmarkDir = Path.Combine(Path.GetTempPath(), "LivingDocGen_Benchmarks", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_benchmarkDir);
    }

    [Fact]
    [Trait("Profile", "Small")]
    public async Task Benchmark_SmallProfile_300Features()
    {
        var harness = new BenchmarkHarness(_benchmarkDir);
        var metrics = await harness.RunBenchmarkAsync(BenchmarkProfile.Small);

        _output.WriteLine(metrics.ToSummaryLine());
        Assert.True(metrics.ParsedFeatureCount >= 290, "Expected ~300 features parsed");
        Assert.True(metrics.TotalMs > 0, "Total time should be positive");
        Assert.True(metrics.OutputSizeBytes > 0, "Output should be non-empty");
    }

    [Fact]
    [Trait("Profile", "Medium")]
    public async Task Benchmark_MediumProfile_1200Features()
    {
        var harness = new BenchmarkHarness(_benchmarkDir);
        var metrics = await harness.RunBenchmarkAsync(BenchmarkProfile.Medium);

        _output.WriteLine(metrics.ToSummaryLine());
        Assert.True(metrics.ParsedFeatureCount >= 1100, "Expected ~1200 features parsed");
        Assert.True(metrics.TotalMs > 0, "Total time should be positive");
    }

    [Fact]
    [Trait("Profile", "Large")]
    public async Task Benchmark_LargeProfile_3000Features()
    {
        var harness = new BenchmarkHarness(_benchmarkDir);
        var metrics = await harness.RunBenchmarkAsync(BenchmarkProfile.Large);

        _output.WriteLine(metrics.ToSummaryLine());
        Assert.True(metrics.ParsedFeatureCount >= 2900, "Expected ~3000 features parsed");
        Assert.True(metrics.TotalMs > 0, "Total time should be positive");
    }

    [Fact]
    [Trait("Profile", "All")]
    public async Task Benchmark_AllProfiles_WithReport()
    {
        var harness = new BenchmarkHarness(_benchmarkDir);
        var results = await harness.RunAllProfilesAsync();
        harness.WriteReport(results);

        foreach (var (profile, metrics) in results)
        {
            _output.WriteLine($"[{profile}] {metrics.ToSummaryLine()}");
        }

        var reportPath = Path.Combine(_benchmarkDir, "BENCHMARK_REPORT.md");
        Assert.True(File.Exists(reportPath), "Benchmark report should be generated");
        _output.WriteLine($"Report: {reportPath}");
    }

    [Fact]
    public async Task Telemetry_CapturessAllPhases()
    {
        // Validate that telemetry hooks capture all expected phases
        var harness = new BenchmarkHarness(_benchmarkDir);
        var metrics = await harness.RunBenchmarkAsync(BenchmarkProfile.Small);

        Assert.True(metrics.FileDiscoveryMs >= 0, "FileDiscovery should be recorded");
        Assert.True(metrics.ParsingMs > 0, "Parsing should be recorded");
        Assert.True(metrics.TestResultParsingMs >= 0, "TestResultParsing should be recorded");
        Assert.True(metrics.EnrichmentMs >= 0, "Enrichment should be recorded");
        Assert.True(metrics.HtmlGenerationMs > 0, "HtmlGeneration should be recorded");
        Assert.True(metrics.FileWriteMs >= 0, "FileWrite should be recorded");
        Assert.True(metrics.TotalMs > 0, "Total should be recorded");
        Assert.True(metrics.MemoryBeforeBytes > 0, "Memory before should be recorded");
        Assert.True(metrics.PeakMemoryBytes > 0, "Peak memory should be recorded");
        Assert.True(metrics.ParsedFeatureCount > 0, "Feature count should be recorded");
        Assert.True(metrics.ParsedScenarioCount > 0, "Scenario count should be recorded");
    }

    [Fact]
    public void SyntheticDataset_GeneratesExpectedFiles()
    {
        var dataDir = Path.Combine(_benchmarkDir, "dataset_test");
        var (featuresDir, testResultFile) = SyntheticDatasetGenerator.GenerateDataset(
            dataDir, featureCount: 10, scenariosPerFeature: 3);

        var featureFiles = Directory.GetFiles(featuresDir, "*.feature");
        Assert.Equal(10, featureFiles.Length);
        Assert.True(File.Exists(testResultFile), "Test result file should exist");
        Assert.True(new FileInfo(testResultFile).Length > 100, "Test result file should have content");
    }
}
