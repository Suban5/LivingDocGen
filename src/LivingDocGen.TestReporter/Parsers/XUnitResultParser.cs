using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LivingDocGen.TestReporter.Parsers;

using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Parser for xUnit XML test results
/// </summary>
public class XUnitResultParser : ITestResultParser
{
    public TestFramework SupportedFramework => TestFramework.XUnit;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var doc = XDocument.Load(filePath);
            return doc.Root?.Name.LocalName == "assemblies" || doc.Root?.Name.LocalName == "assembly";
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid xUnit XML format");

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.XUnit,
            GeneratedAt = DateTime.UtcNow
        };

        // Handle both <assemblies> and single <assembly> root
        var assemblies = root.Name.LocalName == "assemblies" 
            ? root.Elements("assembly") 
            : new[] { root };

        foreach (var assembly in assemblies)
        {
            // Extract environment info
            if (report.Environment.Count == 0)
            {
                report.Environment["AssemblyName"] = assembly.Attribute("name")?.Value ?? "Unknown";
                report.Environment["Framework"] = assembly.Attribute("test-framework")?.Value ?? "Unknown";
                report.Environment["Environment"] = assembly.Attribute("environment")?.Value ?? "Unknown";
                report.Environment["TestTime"] = assembly.Attribute("run-date")?.Value + " " + assembly.Attribute("run-time")?.Value;
            }

            var totalTime = double.Parse(assembly.Attribute("time")?.Value ?? "0");
            report.TotalDuration += TimeSpan.FromSeconds(totalTime);

            // Parse collections (features)
            var collections = assembly.Elements("collection");
            foreach (var collection in collections)
            {
                var feature = ParseFeature(collection);
                if (feature.Scenarios.Any())
                    report.Features.Add(feature);
            }
        }

        // Calculate statistics
        report.Statistics = CalculateStatistics(report.Features);

        return report;
    }

    private FeatureExecutionResult ParseFeature(XElement collection)
    {
        var collectionName = collection.Attribute("name")?.Value ?? "Unknown Feature";
        
        var feature = new FeatureExecutionResult
        {
            FeatureName = collectionName,
            Duration = TimeSpan.FromSeconds(double.Parse(collection.Attribute("time")?.Value ?? "0")),
            Status = DetermineFeatureStatus(collection)
        };

        // Parse test cases (scenarios) and extract feature metadata from first test's traits
        var testCases = collection.Elements("test");
        foreach (var testCase in testCases)
        {
            // Extract metadata from the first test case's traits if not already set
            if (string.IsNullOrEmpty(feature.FeatureFilePath))
            {
                var traits = testCase.Element("traits")?.Elements("trait");
                if (traits != null)
                {
                    var featureFileTrait = traits.FirstOrDefault(t => t.Attribute("name")?.Value == "FeatureFile");
                    if (featureFileTrait != null)
                    {
                        feature.FeatureFilePath = featureFileTrait.Attribute("value")?.Value ?? "";
                    }
                    
                    // Also try to get the feature name from Feature trait (more accurate than collection name)
                    var featureNameTrait = traits.FirstOrDefault(t => t.Attribute("name")?.Value == "Feature");
                    if (featureNameTrait != null)
                    {
                        var featureName = featureNameTrait.Attribute("value")?.Value;
                        if (!string.IsNullOrEmpty(featureName))
                        {
                            feature.FeatureName = featureName;
                        }
                    }
                }
            }
            
            feature.Scenarios.Add(ParseScenario(testCase));
        }

        return feature;
    }

    private ScenarioExecutionResult ParseScenario(XElement test)
    {
        var testName = test.Attribute("name")?.Value ?? "Unknown Scenario";
        
        // For xUnit, test names are already human-readable
        // Extract base scenario name (remove parameters for scenario outlines)
        // e.g., "Login with multiple users(username: "admin@test.com", ...)" -> "Login with multiple users"
        var scenarioName = testName;
        var parenIndex = testName.IndexOf('(');
        if (parenIndex > 0)
        {
            scenarioName = testName.Substring(0, parenIndex).Trim();
        }
        
        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = scenarioName,
            Duration = TimeSpan.FromSeconds(double.Parse(test.Attribute("time")?.Value ?? "0")),
            Status = MapResultToStatus(test.Attribute("result")?.Value)
        };

        // Extract traits (tags)
        var traits = test.Element("traits")?.Elements("trait");
        if (traits != null)
        {
            scenario.Tags = traits
                .Where(t => t.Attribute("name")?.Value == "Category")
                .Select(t => t.Attribute("value")?.Value ?? "")
                .ToList();
        }

        // Extract failure info
        var failure = test.Element("failure");
        if (failure != null)
        {
            scenario.ErrorMessage = failure.Element("message")?.Value;
            scenario.StackTrace = failure.Element("stack-trace")?.Value;
        }

        // Extract reason for skip
        var reason = test.Element("reason");
        if (reason != null && scenario.Status == ExecutionStatus.Skipped)
        {
            scenario.ErrorMessage = reason.Element("message")?.Value;
        }

        // Parse output for step information
        var output = test.Element("output")?.Value;
        if (!string.IsNullOrEmpty(output))
        {
            scenario.StepResults = ParseStepsFromOutput(output);
        }

        return scenario;
    }

    private List<StepExecutionResult> ParseStepsFromOutput(string output)
    {
        var steps = new List<StepExecutionResult>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Look for step execution patterns
            if (trimmed.StartsWith("→ ") || trimmed.StartsWith("✓ ") || trimmed.StartsWith("✗ "))
            {
                var status = trimmed.StartsWith("✓") ? ExecutionStatus.Passed :
                            trimmed.StartsWith("✗") ? ExecutionStatus.Failed :
                            ExecutionStatus.NotExecuted;

                var text = trimmed.TrimStart('→', '✓', '✗', ' ');
                
                // Try to extract keyword
                var parts = text.Split(new[] { ' ' }, 2);
                if (parts.Length >= 1)
                {
                    var step = new StepExecutionResult
                    {
                        Keyword = parts.Length == 2 && IsGherkinKeyword(parts[0]) ? parts[0] : "Given",
                        Text = parts.Length == 2 && IsGherkinKeyword(parts[0]) ? parts[1] : text,
                        Status = status
                    };
                    steps.Add(step);
                }
            }
        }

        return steps;
    }

    private bool IsGherkinKeyword(string word)
    {
        return word == "Given" || word == "When" || word == "Then" || 
               word == "And" || word == "But" || word == "*";
    }

    private ExecutionStatus MapResultToStatus(string result)
    {
        return result?.ToLower() switch
        {
            "pass" => ExecutionStatus.Passed,
            "fail" => ExecutionStatus.Failed,
            "skip" => ExecutionStatus.Skipped,
            _ => ExecutionStatus.NotExecuted
        };
    }

    private ExecutionStatus DetermineFeatureStatus(XElement collection)
    {
        var failed = int.Parse(collection.Attribute("failed")?.Value ?? "0");
        var skipped = int.Parse(collection.Attribute("skipped")?.Value ?? "0");
        var passed = int.Parse(collection.Attribute("passed")?.Value ?? "0");

        if (failed > 0) return ExecutionStatus.Failed;
        if (skipped > 0 && passed == 0) return ExecutionStatus.Skipped;
        if (passed > 0) return ExecutionStatus.Passed;
        return ExecutionStatus.NotExecuted;
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
                }

                stats.TotalSteps += scenario.StepResults.Count;
                stats.PassedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Passed);
                stats.FailedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Failed);
                stats.SkippedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Skipped);
            }
        }

        return stats;
    }
}
