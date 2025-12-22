using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LivingDocGen.TestReporter.Services;

using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Models;
using LivingDocGen.TestReporter.Parsers;
using LivingDocGen.Core.Exceptions;

/// <summary>
/// Service for parsing test execution results from various frameworks
/// </summary>
public class TestReportService : ITestReportService
{
    private readonly List<ITestResultParser> _parsers;

    public TestReportService()
    {
        _parsers = new List<ITestResultParser>
        {
            new NUnit2ResultParser(),     // NUnit 2.x legacy format (test-results root)
            new NUnitResultParser(),       // NUnit 3.x/4.x format (test-run root)
            new XUnitResultParser(),
            new JUnitResultParser(),
            new SpecFlowJsonResultParser(),
            new TrxResultParser()          // Microsoft TRX format (NUnit 4, MSTest, VSTest)
        };
    }

    /// <summary>
    /// Parse a test result file using the appropriate parser
    /// </summary>
    public TestExecutionReport ParseTestResults(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Test result file not found: {filePath}");

        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));
        if (parser == null)
        {
            Console.WriteLine($"   ❌ No parser found for: {Path.GetFileName(filePath)}");
            Console.WriteLine($"      Tried parsers: {string.Join(", ", _parsers.Select(p => p.SupportedFramework))}");
            throw new NotSupportedException($"No parser found for file: {filePath}");
        }

        Console.WriteLine($"   ✓ Parsing {Path.GetFileName(filePath)} with {parser.SupportedFramework} parser");
        var report = parser.Parse(filePath);
        Console.WriteLine($"      Found {report.Features.Count} features, {report.Statistics.TotalScenarios} scenarios");
        
        return report;
    }

    /// <summary>
    /// Parse multiple test result files and merge into a single report
    /// </summary>
    public TestExecutionReport ParseMultipleTestResults(params string[] filePaths)
    {
        var reports = filePaths.Select(ParseTestResults).ToList();
        return MergeReports(reports);
    }

    /// <summary>
    /// Parse all test result files in a directory
    /// </summary>
    public TestExecutionReport ParseDirectory(string directoryPath, string searchPattern = "*.*")
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var files = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories)
            .Where(f => _parsers.Any(p => p.CanParse(f)))
            .ToList();

        if (!files.Any())
            throw new InvalidOperationException($"No test result files found in: {directoryPath}");

        var reports = files.Select(ParseTestResults).ToList();
        return MergeReports(reports);
    }

    /// <summary>
    /// Auto-detect framework and parse test results
    /// </summary>
    public TestFramework DetectFramework(string filePath)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath));
        return parser?.SupportedFramework ?? TestFramework.Unknown;
    }

    /// <summary>
    /// Merge multiple test execution reports into one
    /// Strategy: For duplicate scenarios, keep the most recent test result (latest StartTime)
    /// This handles regression testing where failed tests are re-executed
    /// </summary>
    private TestExecutionReport MergeReports(List<TestExecutionReport> reports)
    {
        if (!reports.Any())
            throw new ArgumentException("No reports to merge");

        if (reports.Count == 1)
            return reports[0];

        Console.WriteLine($"\n🔄 Merging {reports.Count} test reports...");

        var mergedReport = new TestExecutionReport
        {
            ReportName = "Merged Test Report",
            Framework = reports.First().Framework,
            GeneratedAt = DateTime.UtcNow,
            TotalDuration = TimeSpan.FromTicks(reports.Sum(r => r.TotalDuration.Ticks))
        };

        // Dictionary to track unique features and scenarios
        var featureMap = new Dictionary<string, FeatureExecutionResult>();

        // Process reports in order
        foreach (var report in reports)
        {
            foreach (var feature in report.Features)
            {
                var featureKey = GetFeatureKey(feature);

                if (!featureMap.ContainsKey(featureKey))
                {
                    // New feature - add it
                    featureMap[featureKey] = feature;
                    Console.WriteLine($"   ➕ Added feature: {feature.FeatureName}");
                }
                else
                {
                    // Feature exists - merge scenarios intelligently
                    Console.WriteLine($"   🔀 Merging scenarios for feature: {feature.FeatureName}");
                    var existingFeature = featureMap[featureKey];
                    MergeFeatureScenarios(existingFeature, feature);
                }
            }
            
            // Merge environment info
            foreach (var env in report.Environment)
            {
                if (!mergedReport.Environment.ContainsKey(env.Key))
                    mergedReport.Environment[env.Key] = env.Value;
            }
        }

        // Add all merged features to the report
        mergedReport.Features.AddRange(featureMap.Values);

        // Recalculate statistics
        mergedReport.Statistics = CalculateStatistics(mergedReport.Features);

        Console.WriteLine($"   ✓ Merge complete: {mergedReport.Features.Count} features, {mergedReport.Statistics.TotalScenarios} scenarios");

        return mergedReport;
    }

    /// <summary>
    /// Generate a unique key for a feature based on name and file path
    /// </summary>
    private string GetFeatureKey(FeatureExecutionResult feature)
    {
        var normalizedName = NormalizeName(feature.FeatureName);
        var normalizedPath = feature.FeatureFilePath?.Replace("\\", "/").ToLowerInvariant() ?? "";
        return $"{normalizedName}|{normalizedPath}";
    }

    /// <summary>
    /// Generate a unique key for a scenario based on name
    /// </summary>
    private string GetScenarioKey(ScenarioExecutionResult scenario)
    {
        return NormalizeName(scenario.ScenarioName);
    }

    /// <summary>
    /// Normalize a name for comparison (lowercase, no spaces/dashes/underscores)
    /// </summary>
    private string NormalizeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;
        
        return name.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
    }

    /// <summary>
    /// Merge scenarios from newFeature into existingFeature
    /// Strategy: Keep the most recent execution for duplicate scenarios
    /// </summary>
    private void MergeFeatureScenarios(FeatureExecutionResult existingFeature, FeatureExecutionResult newFeature)
    {
        var scenarioMap = existingFeature.Scenarios.ToDictionary(
            s => GetScenarioKey(s),
            s => s
        );

        foreach (var newScenario in newFeature.Scenarios)
        {
            var scenarioKey = GetScenarioKey(newScenario);

            if (!scenarioMap.ContainsKey(scenarioKey))
            {
                // New scenario - add it
                existingFeature.Scenarios.Add(newScenario);
                Console.WriteLine($"      ➕ Added scenario: {newScenario.ScenarioName}");
            }
            else
            {
                // Duplicate scenario - keep the most recent one
                var existingScenario = scenarioMap[scenarioKey];
                
                // Compare by StartTime (most recent wins)
                // If StartTime is default/empty, consider Duration or keep existing
                bool shouldReplace = false;
                
                if (newScenario.StartTime != default(DateTime) && existingScenario.StartTime != default(DateTime))
                {
                    shouldReplace = newScenario.StartTime > existingScenario.StartTime;
                }
                else if (newScenario.StartTime != default(DateTime) && existingScenario.StartTime == default(DateTime))
                {
                    shouldReplace = true; // New has timestamp, existing doesn't
                }
                // If neither has StartTime, keep existing (first-come-first-served)

                if (shouldReplace)
                {
                    var index = existingFeature.Scenarios.IndexOf(existingScenario);
                    existingFeature.Scenarios[index] = newScenario;
                    
                    var statusChange = existingScenario.Status != newScenario.Status 
                        ? $" ({existingScenario.Status} → {newScenario.Status})" 
                        : "";
                    
                    Console.WriteLine($"      🔄 Updated scenario: {newScenario.ScenarioName}{statusChange} (newer: {newScenario.StartTime:HH:mm:ss})");
                }
                else
                {
                    Console.WriteLine($"      ⏭️  Kept existing: {existingScenario.ScenarioName} (older result discarded)");
                }
            }
        }

        // Update feature-level aggregates
        existingFeature.Duration = TimeSpan.FromTicks(
            existingFeature.Scenarios.Sum(s => s.Duration.Ticks)
        );
    }

    private TestStatistics CalculateStatistics(List<FeatureExecutionResult> features)
    {
        var stats = new TestStatistics();
        
        foreach (var feature in features)
        {
            foreach (var scenario in feature.Scenarios)
            {
                stats.TotalScenarios++;
                switch (scenario.Status)
                {
                    case ExecutionStatus.Passed:
                        stats.PassedScenarios++;
                        break;
                    case ExecutionStatus.Failed:
                        stats.FailedScenarios++;
                        break;
                    case ExecutionStatus.Skipped:
                        stats.SkippedScenarios++;
                        break;
                    case ExecutionStatus.Inconclusive:
                        stats.InconclusiveScenarios++;
                        break;
                }

                stats.TotalSteps += scenario.StepResults.Count;
                stats.PassedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Passed);
                stats.FailedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Failed);
                stats.SkippedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Skipped);
            }
        }

        return stats;
    }

    /// <summary>
    /// Get summary of test results
    /// </summary>
    public string GetSummary(TestExecutionReport report)
    {
        return $@"Test Execution Report Summary
================================
Framework: {report.Framework}
Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}
Total Duration: {report.TotalDuration:hh\:mm\:ss\.fff}

Features: {report.Features.Count}
Scenarios: {report.Statistics.TotalScenarios} (✓ {report.Statistics.PassedScenarios} | ✗ {report.Statistics.FailedScenarios} | ⊝ {report.Statistics.SkippedScenarios})
Steps: {report.Statistics.TotalSteps} (✓ {report.Statistics.PassedSteps} | ✗ {report.Statistics.FailedSteps} | ⊝ {report.Statistics.SkippedSteps})

Pass Rate: {report.Statistics.PassRate:F2}%
Fail Rate: {report.Statistics.FailRate:F2}%";
    }
}
