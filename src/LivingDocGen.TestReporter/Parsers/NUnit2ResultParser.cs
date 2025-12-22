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
/// Parser for NUnit 2.x XML test results (legacy format)
/// </summary>
public class NUnit2ResultParser : ITestResultParser
{
    public TestFramework SupportedFramework => TestFramework.NUnit;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var doc = XDocument.Load(filePath);
            // NUnit2 uses <test-results> root element
            return doc.Root?.Name.LocalName == "test-results";
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var testResults = doc.Root ?? throw new InvalidOperationException("Invalid NUnit2 XML format");

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.NUnit,
            GeneratedAt = DateTime.Parse(testResults.Attribute("date")?.Value + " " + testResults.Attribute("time")?.Value ?? DateTime.UtcNow.ToString()),
            TotalDuration = TimeSpan.FromSeconds(0) // NUnit2 doesn't have overall duration
        };

        // Parse environment info
        var environment = testResults.Element("environment");
        if (environment != null)
        {
            report.Environment["Framework"] = environment.Attribute("nunit-version")?.Value ?? "NUnit 2.x";
            report.Environment["CLR"] = environment.Attribute("clr-version")?.Value ?? "Unknown";
            report.Environment["OS"] = environment.Attribute("os-version")?.Value ?? "Unknown";
            report.Environment["Platform"] = environment.Attribute("platform")?.Value ?? "Unknown";
        }

        // Parse test suites (features)
        var testSuites = testResults.Descendants("test-suite")
            .Where(ts => ts.Attribute("type")?.Value == "TestFixture");

        foreach (var suite in testSuites)
        {
            var feature = ParseFeature(suite);
            if (feature.Scenarios.Any())
            {
                report.Features.Add(feature);
                report.TotalDuration += feature.Duration;
            }
        }

        // Calculate statistics
        report.Statistics = CalculateStatistics(report.Features);

        return report;
    }

    private FeatureExecutionResult ParseFeature(XElement testSuite)
    {
        var feature = new FeatureExecutionResult
        {
            FeatureName = testSuite.Attribute("name")?.Value ?? "Unknown Feature",
            FeatureFilePath = testSuite.Descendants("property")
                .FirstOrDefault(p => p.Attribute("name")?.Value == "Description")
                ?.Attribute("value")?.Value ?? "",
            Duration = TimeSpan.FromSeconds(double.Parse(testSuite.Attribute("time")?.Value ?? "0")),
            Status = MapResultToStatus(testSuite.Attribute("result")?.Value, testSuite.Attribute("success")?.Value)
        };

        // Extract tags from categories
        var categories = testSuite.Descendants("category");
        feature.Tags = categories.Select(c => c.Attribute("name")?.Value ?? "").ToList();

        // Parse test cases (scenarios) from results element
        var results = testSuite.Element("results");
        if (results != null)
        {
            var testCases = results.Elements("test-case");
            foreach (var testCase in testCases)
            {
                feature.Scenarios.Add(ParseScenario(testCase));
            }
        }

        return feature;
    }

    private ScenarioExecutionResult ParseScenario(XElement testCase)
    {
        var executed = testCase.Attribute("executed")?.Value == "True";
        var success = testCase.Attribute("success")?.Value == "True";
        var scenarioStatus = MapResultToStatus(
            testCase.Attribute("result")?.Value,
            testCase.Attribute("success")?.Value
        );

        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = testCase.Attribute("name")?.Value ?? "Unknown Scenario",
            Status = scenarioStatus,
            Duration = TimeSpan.FromSeconds(double.Parse(testCase.Attribute("time")?.Value ?? "0")),
            StartTime = DateTime.UtcNow, // NUnit2 doesn't provide start time
            EndTime = DateTime.UtcNow
        };

        // Extract tags from categories
        var categories = testCase.Descendants("category");
        scenario.Tags = categories.Select(c => c.Attribute("name")?.Value ?? "").ToList();

        // Extract failure info
        var failure = testCase.Element("failure");
        if (failure != null)
        {
            var message = failure.Element("message")?.Value ?? "";
            scenario.ErrorMessage = message;
            scenario.StackTrace = failure.Element("stack-trace")?.Value;
        }

        // Extract reason for skipped tests
        var reason = testCase.Element("reason");
        if (reason != null && scenarioStatus == ExecutionStatus.Skipped)
        {
            scenario.ErrorMessage = reason.Element("message")?.Value ?? "Test skipped";
        }

        return scenario;
    }

    private ExecutionStatus MapResultToStatus(string result, string success)
    {
        // NUnit2 uses both 'result' and 'success' attributes
        if (result == "Ignored" || result == "NotRunnable")
            return ExecutionStatus.Skipped;

        if (success == "True")
            return ExecutionStatus.Passed;

        if (success == "False")
            return ExecutionStatus.Failed;

        // Fallback to result attribute
        return result?.ToLower() switch
        {
            "success" => ExecutionStatus.Passed,
            "failure" => ExecutionStatus.Failed,
            "error" => ExecutionStatus.Failed,
            "ignored" => ExecutionStatus.Skipped,
            "inconclusive" => ExecutionStatus.Skipped,
            _ => ExecutionStatus.NotExecuted
        };
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
}
