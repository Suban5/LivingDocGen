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
/// Parser for JUnit XML test results (Cucumber Java, Maven Surefire, etc.)
/// </summary>
public class JUnitResultParser : ITestResultParser
{
    public TestFramework SupportedFramework => TestFramework.JUnit;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var doc = XDocument.Load(filePath);
            return doc.Root?.Name.LocalName == "testsuites" || doc.Root?.Name.LocalName == "testsuite";
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid JUnit XML format");

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.JUnit,
            GeneratedAt = DateTime.UtcNow
        };

        // Handle both <testsuites> and single <testsuite> root
        var testSuites = root.Name.LocalName == "testsuites" 
            ? root.Elements("testsuite") 
            : new[] { root };

        foreach (var testSuite in testSuites)
        {
            // Extract environment info from first suite
            if (report.Environment.Count == 0)
            {
                report.Environment["TestSuite"] = testSuite.Attribute("name")?.Value ?? "Unknown";
                report.Environment["Hostname"] = testSuite.Attribute("hostname")?.Value ?? "Unknown";
                report.Environment["Timestamp"] = testSuite.Attribute("timestamp")?.Value ?? DateTime.UtcNow.ToString();
                
                // Extract properties
                var properties = testSuite.Element("properties")?.Elements("property");
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        var name = prop.Attribute("name")?.Value;
                        var value = prop.Attribute("value")?.Value;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            report.Environment[name] = value;
                        }
                    }
                }
            }

            var time = double.Parse(testSuite.Attribute("time")?.Value ?? "0");
            report.TotalDuration += TimeSpan.FromSeconds(time);

            // Parse feature
            var feature = ParseFeature(testSuite);
            if (feature.Scenarios.Any())
                report.Features.Add(feature);
        }

        // Calculate statistics
        report.Statistics = CalculateStatistics(report.Features);

        return report;
    }

    private FeatureExecutionResult ParseFeature(XElement testSuite)
    {
        var testSuiteName = testSuite.Attribute("name")?.Value ?? "Unknown Feature";
        
        var feature = new FeatureExecutionResult
        {
            FeatureName = testSuiteName,
            Duration = TimeSpan.FromSeconds(double.Parse(testSuite.Attribute("time")?.Value ?? "0")),
            Status = DetermineFeatureStatus(testSuite)
        };

        // Try to extract feature metadata from properties
        var properties = testSuite.Element("properties")?.Elements("property");
        if (properties != null)
        {
            // Extract feature file path (supports both "feature.file" and "featureFile")
            var featureFileProp = properties.FirstOrDefault(p => 
                p.Attribute("name")?.Value == "feature.file" || 
                p.Attribute("name")?.Value == "featureFile");
            if (featureFileProp != null)
            {
                feature.FeatureFilePath = featureFileProp.Attribute("value")?.Value ?? "";
            }
            
            // Extract feature name from property (more accurate than testsuite name)
            var featureNameProp = properties.FirstOrDefault(p => p.Attribute("name")?.Value == "feature.name");
            if (featureNameProp != null)
            {
                var featureName = featureNameProp.Attribute("value")?.Value;
                if (!string.IsNullOrEmpty(featureName))
                {
                    feature.FeatureName = featureName;
                }
            }
            
            // Extract tags from property
            var featureTagsProp = properties.FirstOrDefault(p => p.Attribute("name")?.Value == "feature.tags");
            if (featureTagsProp != null)
            {
                var tags = featureTagsProp.Attribute("value")?.Value ?? "";
                feature.Tags = tags.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
            }
        }

        // Parse test cases (scenarios)
        var testCases = testSuite.Elements("testcase");
        foreach (var testCase in testCases)
        {
            feature.Scenarios.Add(ParseScenario(testCase));
        }

        return feature;
    }

    private ScenarioExecutionResult ParseScenario(XElement testCase)
    {
        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = testCase.Attribute("name")?.Value ?? "Unknown Scenario",
            Duration = TimeSpan.FromSeconds(double.Parse(testCase.Attribute("time")?.Value ?? "0")),
            Status = DetermineScenarioStatus(testCase)
        };

        // Extract class name (often contains tags or feature info)
        var className = testCase.Attribute("classname")?.Value;
        if (!string.IsNullOrEmpty(className))
        {
            scenario.Metadata["ClassName"] = className;
            
            // Try to extract tags from class name (common pattern: Feature.Scenario[@tag1,@tag2])
            var tagMatch = System.Text.RegularExpressions.Regex.Match(className, @"\[@(.+?)\]");
            if (tagMatch.Success)
            {
                scenario.Tags = tagMatch.Groups[1].Value
                    .Split(',')
                    .Select(t => t.Trim())
                    .ToList();
            }
        }

        // Extract failure info
        var failure = testCase.Element("failure");
        if (failure != null)
        {
            scenario.ErrorMessage = failure.Attribute("message")?.Value;
            scenario.StackTrace = failure.Value;
            
            // Try to extract failed step line number from message
            ExtractFailedStepInfo(scenario);
        }

        // Extract error info
        var error = testCase.Element("error");
        if (error != null)
        {
            scenario.ErrorMessage = error.Attribute("message")?.Value;
            scenario.StackTrace = error.Value;
        }

        // Extract skip info
        var skipped = testCase.Element("skipped");
        if (skipped != null)
        {
            scenario.ErrorMessage = skipped.Attribute("message")?.Value ?? "Test skipped";
        }

        // Parse system-out for step information
        var systemOut = testCase.Element("system-out")?.Value;
        if (!string.IsNullOrEmpty(systemOut))
        {
            scenario.StepResults = ParseStepsFromOutput(systemOut);
        }

        return scenario;
    }

    private void ExtractFailedStepInfo(ScenarioExecutionResult scenario)
    {
        if (string.IsNullOrEmpty(scenario.StackTrace))
            return;

        // Look for patterns like "at step line 45" or "feature:45"
        var lineMatch = System.Text.RegularExpressions.Regex.Match(
            scenario.StackTrace, 
            @"(?:line|:)\s*(\d+)|\.feature:(\d+)"
        );
        
        if (lineMatch.Success)
        {
            var lineNum = lineMatch.Groups[1].Success 
                ? lineMatch.Groups[1].Value 
                : lineMatch.Groups[2].Value;
            
            if (int.TryParse(lineNum, out int line))
            {
                scenario.Metadata["FailedAtLine"] = line.ToString();
            }
        }
    }

    private List<StepExecutionResult> ParseStepsFromOutput(string output)
    {
        var steps = new List<StepExecutionResult>();
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Cucumber Java output patterns
            if (trimmed.StartsWith("Given ") || trimmed.StartsWith("When ") || 
                trimmed.StartsWith("Then ") || trimmed.StartsWith("And ") || 
                trimmed.StartsWith("But ") || trimmed.StartsWith("* "))
            {
                var parts = trimmed.Split(new[] { ' ' }, 2);
                if (parts.Length >= 1)
                {
                    // Try to detect status from output formatting
                    var status = ExecutionStatus.Passed;
                    if (trimmed.Contains("✓") || trimmed.Contains("passed")) status = ExecutionStatus.Passed;
                    else if (trimmed.Contains("✗") || trimmed.Contains("failed")) status = ExecutionStatus.Failed;
                    else if (trimmed.Contains("-") || trimmed.Contains("skipped")) status = ExecutionStatus.Skipped;

                    var step = new StepExecutionResult
                    {
                        Keyword = parts[0],
                        Text = parts.Length > 1 ? parts[1] : parts[0],
                        Status = status
                    };

                    // Try to extract line number
                    var lineMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"#\s*(\d+)");
                    if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out int lineNum))
                    {
                        step.LineNumber = lineNum;
                    }

                    steps.Add(step);
                }
            }
        }

        return steps;
    }

    private ExecutionStatus DetermineScenarioStatus(XElement testCase)
    {
        if (testCase.Element("failure") != null) return ExecutionStatus.Failed;
        if (testCase.Element("error") != null) return ExecutionStatus.Failed;
        if (testCase.Element("skipped") != null) return ExecutionStatus.Skipped;
        return ExecutionStatus.Passed;
    }

    private ExecutionStatus DetermineFeatureStatus(XElement testSuite)
    {
        var failures = int.Parse(testSuite.Attribute("failures")?.Value ?? "0");
        var errors = int.Parse(testSuite.Attribute("errors")?.Value ?? "0");
        var skipped = int.Parse(testSuite.Attribute("skipped")?.Value ?? "0");
        var tests = int.Parse(testSuite.Attribute("tests")?.Value ?? "0");

        if (failures > 0 || errors > 0) return ExecutionStatus.Failed;
        if (skipped > 0 && skipped == tests) return ExecutionStatus.Skipped;
        if (tests > 0) return ExecutionStatus.Passed;
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
