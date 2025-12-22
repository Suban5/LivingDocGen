namespace LivingDocGen.TestReporter.Parsers;

using System.Xml.Linq;
using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Parser for NUnit 3 XML test results
/// </summary>
public class NUnitResultParser : ITestResultParser
{
    public TestFramework SupportedFramework => TestFramework.NUnit;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var doc = XDocument.Load(filePath);
            // Accept test-run root with either 'name' or 'id' attribute, or just test-run element
            // This supports different NUnit3/NUnit4 output formats
            return doc.Root?.Name.LocalName == "test-run";
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var testRun = doc.Root ?? throw new InvalidOperationException("Invalid NUnit XML format");

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.NUnit,
            GeneratedAt = DateTime.Parse(testRun.Attribute("start-time")?.Value ?? DateTime.UtcNow.ToString()),
            TotalDuration = TimeSpan.FromSeconds(double.Parse(testRun.Attribute("duration")?.Value ?? "0"))
        };

        // Parse environment info
        var environment = testRun.Element("test-suite")?.Element("environment");
        if (environment != null)
        {
            report.Environment["Framework"] = environment.Attribute("framework-version")?.Value ?? "Unknown";
            report.Environment["CLR"] = environment.Attribute("clr-version")?.Value ?? "Unknown";
            report.Environment["OS"] = environment.Attribute("os-version")?.Value ?? "Unknown";
            report.Environment["Platform"] = environment.Attribute("platform")?.Value ?? "Unknown";
        }

        // Parse test suites (features)
        var testSuites = testRun.Descendants("test-suite")
            .Where(ts => ts.Attribute("type")?.Value == "TestFixture");

        foreach (var suite in testSuites)
        {
            var feature = ParseFeature(suite);
            if (feature.Scenarios.Any())
                report.Features.Add(feature);
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
                .FirstOrDefault(p => p.Attribute("name")?.Value == "FeatureFile")
                ?.Attribute("value")?.Value ?? "",
            Duration = TimeSpan.FromSeconds(double.Parse(testSuite.Attribute("duration")?.Value ?? "0")),
            Status = MapResultToStatus(testSuite.Attribute("result")?.Value)
        };

        // Extract tags from categories
        var categories = testSuite.Descendants("property")
            .Where(p => p.Attribute("name")?.Value == "Category");
        feature.Tags = categories.Select(c => c.Attribute("value")?.Value ?? "").ToList();

        // Parse test cases (scenarios)
        var testCases = testSuite.Elements("test-case");
        foreach (var testCase in testCases)
        {
            feature.Scenarios.Add(ParseScenario(testCase));
        }

        return feature;
    }

    private ScenarioExecutionResult ParseScenario(XElement testCase)
    {
        var scenarioStatus = MapResultToStatus(testCase.Attribute("result")?.Value);
        
        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = testCase.Attribute("name")?.Value ?? "Unknown Scenario",
            Status = scenarioStatus,
            Duration = TimeSpan.FromSeconds(double.Parse(testCase.Attribute("duration")?.Value ?? "0")),
            StartTime = DateTime.Parse(testCase.Attribute("start-time")?.Value ?? DateTime.UtcNow.ToString()),
            EndTime = DateTime.Parse(testCase.Attribute("end-time")?.Value ?? DateTime.UtcNow.ToString())
        };

        // Extract tags
        var categories = testCase.Descendants("property")
            .Where(p => p.Attribute("name")?.Value == "Category");
        scenario.Tags = categories.Select(c => c.Attribute("value")?.Value ?? "").ToList();

        // Extract failure info
        var failure = testCase.Element("failure");
        if (failure != null)
        {
            var message = failure.Element("message")?.Value ?? "";
            scenario.ErrorMessage = message;
            scenario.StackTrace = failure.Element("stack-trace")?.Value;
            
            // Extract line number from error message if present
            var lineMatch = System.Text.RegularExpressions.Regex.Match(message, @"line (\d+)");
            if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var lineNum))
            {
                // Store for later use in step marking
                scenario.Tags.Add($"FailedAtLine:{lineNum}");
            }
        }

        // Extract attachments
        var attachments = testCase.Descendants("attachment");
        scenario.Attachments = attachments.Select(a => a.Element("filePath")?.Value ?? "").ToList();

        // Parse steps from output (if available)
        var output = testCase.Element("output")?.Value;
        if (!string.IsNullOrEmpty(output))
        {
            scenario.StepResults = ParseStepsFromOutput(output);
            
            // If scenario failed but no step was marked as failed, mark the last step
            if (scenarioStatus == ExecutionStatus.Failed && 
                scenario.StepResults.Count > 0 &&
                !scenario.StepResults.Any(s => s.Status == ExecutionStatus.Failed))
            {
                scenario.StepResults[scenario.StepResults.Count - 1].Status = ExecutionStatus.Failed;
                if (!string.IsNullOrEmpty(scenario.ErrorMessage))
                {
                    scenario.StepResults[scenario.StepResults.Count - 1].ErrorMessage = scenario.ErrorMessage;
                }
            }
        }

        return scenario;
    }

    private List<StepExecutionResult> ParseStepsFromOutput(string output)
    {
        var steps = new List<StepExecutionResult>();
        var lines = output.Split('\n');
        var failedStepFound = false;

        foreach (var line in lines)
        {
            // Look for common step patterns in output
            var trimmed = line.Trim();
            
            // Check for failed step indicator
            if (trimmed.Contains("<-- FAILED") || trimmed.Contains("FAILED AT THIS STEP"))
            {
                failedStepFound = true;
                // Mark previous step as failed if it exists
                if (steps.Count > 0)
                {
                    steps[steps.Count - 1].Status = ExecutionStatus.Failed;
                    
                    // Extract line number if present
                    var lineMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"line (\d+)");
                    if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out var lineNum))
                    {
                        steps[steps.Count - 1].LineNumber = lineNum;
                    }
                }
                continue;
            }
            
            if (trimmed.StartsWith("Given ") || trimmed.StartsWith("When ") || 
                trimmed.StartsWith("Then ") || trimmed.StartsWith("And ") || 
                trimmed.StartsWith("But "))
            {
                // Remove any inline comments or failure markers
                var cleanLine = trimmed.Split(new[] { "<--", "//" }, StringSplitOptions.None)[0].Trim();
                
                var parts = cleanLine.Split(new[] { ' ' }, 2);
                if (parts.Length == 2)
                {
                    var step = new StepExecutionResult
                    {
                        Keyword = parts[0],
                        Text = parts[1],
                        Status = failedStepFound ? ExecutionStatus.Skipped : ExecutionStatus.Passed
                    };
                    steps.Add(step);
                }
            }
        }

        return steps;
    }

    private ExecutionStatus MapResultToStatus(string? result)
    {
        return result?.ToLower() switch
        {
            "passed" => ExecutionStatus.Passed,
            "failed" => ExecutionStatus.Failed,
            "skipped" => ExecutionStatus.Skipped,
            "inconclusive" => ExecutionStatus.Inconclusive,
            "ignored" => ExecutionStatus.Skipped,
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
