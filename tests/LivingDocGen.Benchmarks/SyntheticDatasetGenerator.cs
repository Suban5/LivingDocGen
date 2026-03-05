using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LivingDocGen.Benchmarks;

/// <summary>
/// Generates synthetic .feature files and NUnit-compatible test result XML
/// at configurable scales (300 / 1200 / 3000+ features) for benchmark runs.
/// </summary>
public static class SyntheticDatasetGenerator
{
    // --- Feature file templates ---

    private static readonly string[] TagPools = new[]
    {
        "@smoke", "@regression", "@critical", "@api", "@ui", "@performance",
        "@security", "@integration", "@e2e", "@wip", "@sprint-42", "@payment",
        "@checkout", "@login", "@search", "@admin", "@reporting"
    };

    private static readonly string[] StepVerbs = new[]
    {
        "the user navigates to", "the system displays", "the user clicks on",
        "the API returns", "the database contains", "the response status is",
        "the user enters", "the page loads within", "the cache is cleared",
        "the notification is sent to", "the record is created in",
        "the file is uploaded to", "the queue message is processed"
    };

    /// <summary>
    /// Generate a dataset at the specified scale.
    /// </summary>
    /// <param name="outputDirectory">Root directory to write features/ and test-results/ folders.</param>
    /// <param name="featureCount">Number of .feature files to generate.</param>
    /// <param name="scenariosPerFeature">Average scenarios per feature (±variance).</param>
    /// <param name="stepsPerScenario">Steps per scenario.</param>
    /// <param name="includeOutlines">Whether to include scenario outlines with example tables.</param>
    /// <param name="exampleRowsPerOutline">Number of example rows per outline.</param>
    /// <param name="dataTableChance">Probability (0-1) that a step includes a DataTable.</param>
    /// <param name="dataTableRows">Number of rows in DataTables when generated.</param>
    /// <returns>Tuple of (featureDir, testResultFilePath) for use in benchmarks.</returns>
    public static (string FeaturesDir, string TestResultFile) GenerateDataset(
        string outputDirectory,
        int featureCount = 300,
        int scenariosPerFeature = 5,
        int stepsPerScenario = 5,
        bool includeOutlines = true,
        int exampleRowsPerOutline = 10,
        double dataTableChance = 0.1,
        int dataTableRows = 8)
    {
        var featuresDir = Path.Combine(outputDirectory, "features");
        var testResultsDir = Path.Combine(outputDirectory, "test-results");
        Directory.CreateDirectory(featuresDir);
        Directory.CreateDirectory(testResultsDir);

        var rng = new Random(42); // deterministic seed for reproducibility
        var allScenarioNames = new List<(string FeatureName, string ScenarioName, bool Passed)>();

        for (int f = 0; f < featureCount; f++)
        {
            var featureName = $"Feature_{f:D5}_{GetDomainWord(rng)}";
            var sb = new StringBuilder();

            // Feature-level tags
            var tagCount = rng.Next(1, 4);
            var tags = Enumerable.Range(0, tagCount).Select(_ => TagPools[rng.Next(TagPools.Length)]).Distinct();
            sb.AppendLine(string.Join(" ", tags));
            sb.AppendLine($"Feature: {featureName}");
            sb.AppendLine($"  As a user of domain {GetDomainWord(rng)}");
            sb.AppendLine($"  I want to verify {GetDomainWord(rng)} behavior");
            sb.AppendLine($"  So that the system remains reliable");
            sb.AppendLine();

            // Optional Background
            if (rng.NextDouble() < 0.3)
            {
                sb.AppendLine("  Background:");
                sb.AppendLine($"    Given {StepVerbs[rng.Next(StepVerbs.Length)]} \"{GetDomainWord(rng)}\"");
                sb.AppendLine();
            }

            var scenarioCount = scenariosPerFeature + rng.Next(-2, 3);
            if (scenarioCount < 1) scenarioCount = 1;

            for (int s = 0; s < scenarioCount; s++)
            {
                bool isOutline = includeOutlines && rng.NextDouble() < 0.25;
                var scenarioName = $"Scenario_{f:D5}_{s:D3}_{GetDomainWord(rng)}";
                var passed = rng.NextDouble() < 0.85;

                if (isOutline)
                {
                    sb.AppendLine($"  Scenario Outline: {scenarioName}");
                    WriteSteps(sb, stepsPerScenario, rng, dataTableChance, dataTableRows, isOutline: true);
                    sb.AppendLine();
                    sb.AppendLine("    Examples:");
                    sb.AppendLine("      | param1    | param2    | expected  |");
                    for (int r = 0; r < exampleRowsPerOutline; r++)
                    {
                        sb.AppendLine($"      | val_{r:D3}_a | val_{r:D3}_b | res_{r:D3}   |");
                        allScenarioNames.Add((featureName, $"{scenarioName} (Example {r})", passed));
                    }
                }
                else
                {
                    sb.AppendLine($"  Scenario: {scenarioName}");
                    WriteSteps(sb, stepsPerScenario, rng, dataTableChance, dataTableRows, isOutline: false);
                    allScenarioNames.Add((featureName, scenarioName, passed));
                }

                sb.AppendLine();
            }

            var featureFilePath = Path.Combine(featuresDir, $"{featureName}.feature");
            File.WriteAllText(featureFilePath, sb.ToString(), Encoding.UTF8);
        }

        // Generate NUnit3-compatible XML test results
        var testResultPath = Path.Combine(testResultsDir, "benchmark-results.xml");
        WriteNUnit3TestResults(testResultPath, allScenarioNames, rng);

        return (featuresDir, testResultPath);
    }

    /// <summary>
    /// Convenience method for standard benchmark profiles.
    /// </summary>
    public static (string FeaturesDir, string TestResultFile) GenerateProfile(
        string outputDirectory, BenchmarkProfile profile)
    {
        return profile switch
        {
            BenchmarkProfile.Small => GenerateDataset(outputDirectory, featureCount: 300, scenariosPerFeature: 4),
            BenchmarkProfile.Medium => GenerateDataset(outputDirectory, featureCount: 1200, scenariosPerFeature: 5, includeOutlines: true, exampleRowsPerOutline: 15),
            BenchmarkProfile.Large => GenerateDataset(outputDirectory, featureCount: 3000, scenariosPerFeature: 6, includeOutlines: true, exampleRowsPerOutline: 20, dataTableChance: 0.15, dataTableRows: 12),
            _ => throw new ArgumentOutOfRangeException(nameof(profile))
        };
    }

    // --- Helpers ---

    private static void WriteSteps(StringBuilder sb, int count, Random rng, double dataTableChance, int dataTableRows, bool isOutline)
    {
        var keywords = new[] { "Given", "When", "Then", "And" };

        for (int i = 0; i < count; i++)
        {
            var keyword = i == 0 ? "Given" : (i == count - 1 ? "Then" : keywords[rng.Next(keywords.Length)]);
            var verb = StepVerbs[rng.Next(StepVerbs.Length)];
            var param = isOutline ? "<param1>" : $"\"{GetDomainWord(rng)}\"";
            sb.AppendLine($"    {keyword} {verb} {param}");

            // Optional DataTable
            if (rng.NextDouble() < dataTableChance)
            {
                sb.AppendLine("      | column_a | column_b | column_c |");
                for (int r = 0; r < dataTableRows; r++)
                {
                    sb.AppendLine($"      | data_{r}_a | data_{r}_b | data_{r}_c |");
                }
            }
        }
    }

    private static void WriteNUnit3TestResults(string outputPath, List<(string Feature, string Scenario, bool Passed)> scenarios, Random rng)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine($"<test-run id=\"1\" testcasecount=\"{scenarios.Count}\" result=\"Failed\" " +
                      $"total=\"{scenarios.Count}\" passed=\"{scenarios.Count(s => s.Passed)}\" " +
                      $"failed=\"{scenarios.Count(s => !s.Passed)}\" " +
                      $"start-time=\"{DateTime.UtcNow:O}\" end-time=\"{DateTime.UtcNow.AddMinutes(5):O}\">");

        var byFeature = scenarios.GroupBy(s => s.Feature);
        foreach (var featureGroup in byFeature)
        {
            sb.AppendLine($"  <test-suite type=\"TestFixture\" name=\"{EscapeXml(featureGroup.Key)}\" " +
                          $"fullname=\"BDD.{EscapeXml(featureGroup.Key)}\" " +
                          $"testcasecount=\"{featureGroup.Count()}\" result=\"{(featureGroup.All(s => s.Passed) ? "Passed" : "Failed")}\">");

            foreach (var (_, scenario, passed) in featureGroup)
            {
                var duration = rng.NextDouble() * 2.0;
                var result = passed ? "Passed" : "Failed";
                sb.AppendLine($"    <test-case name=\"{EscapeXml(scenario)}\" " +
                              $"fullname=\"BDD.{EscapeXml(featureGroup.Key)}.{EscapeXml(scenario)}\" " +
                              $"result=\"{result}\" duration=\"{duration:F4}\">");

                if (!passed)
                {
                    sb.AppendLine($"      <failure><message>Assertion failed for {EscapeXml(scenario)}</message></failure>");
                }

                sb.AppendLine("    </test-case>");
            }

            sb.AppendLine("  </test-suite>");
        }

        sb.AppendLine("</test-run>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static readonly string[] DomainWords = new[]
    {
        "Checkout", "Payment", "Inventory", "Shipping", "Login", "Registration",
        "Dashboard", "Analytics", "Notification", "Search", "Cart", "Wishlist",
        "Order", "Refund", "Profile", "Settings", "Report", "Export", "Import",
        "Audit", "Catalog", "Pricing", "Discount", "Subscription", "Invoice"
    };

    private static string GetDomainWord(Random rng) => DomainWords[rng.Next(DomainWords.Length)];

    private static string EscapeXml(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

/// <summary>
/// Standard benchmark dataset sizes matching the design doc profiles.
/// </summary>
public enum BenchmarkProfile
{
    /// <summary>300 features, moderate tables</summary>
    Small,
    /// <summary>1200 features, mixed scenario outlines</summary>
    Medium,
    /// <summary>3000+ features with heavy data tables and examples</summary>
    Large
}
