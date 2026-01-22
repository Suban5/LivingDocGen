using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LivingDocGen.TestReporter.Parsers;

using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Parser for Microsoft Test Results (TRX) format
/// This is the default format for NUnit 4, MSTest, and VSTest
/// </summary>
public class TrxResultParser : ITestResultParser
{
    /// <summary>
    /// Creates secure XML reader settings to prevent XXE attacks
    /// </summary>
    private static System.Xml.XmlReaderSettings CreateSecureXmlReaderSettings()
    {
        return new System.Xml.XmlReaderSettings
        {
            DtdProcessing = System.Xml.DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 1024 * 1024, // 1 MB limit
        };
    }
    private static readonly XNamespace TrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public TestFramework SupportedFramework => TestFramework.MSTest;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".trx", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            // Use secure XML settings to prevent XXE attacks
            using var stream = File.OpenRead(filePath);
            using var reader = System.Xml.XmlReader.Create(stream, CreateSecureXmlReaderSettings());
            var doc = XDocument.Load(reader);
            return doc.Root?.Name.LocalName == "TestRun" &&
                   doc.Root?.Name.Namespace == TrxNamespace;
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        // Use secure XML settings to prevent XXE attacks
        using var stream = File.OpenRead(filePath);
        using var reader = System.Xml.XmlReader.Create(stream, CreateSecureXmlReaderSettings());
        var doc = XDocument.Load(reader);
        var testRun = doc.Root ?? throw new InvalidOperationException("Invalid TRX format");

        var times = testRun.Element(TrxNamespace + "Times");
        var startTime = DateTime.Parse(times?.Attribute("start")?.Value ?? DateTime.UtcNow.ToString());
        var finishTime = DateTime.Parse(times?.Attribute("finish")?.Value ?? DateTime.UtcNow.ToString());

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.MSTest,
            GeneratedAt = startTime,
            TotalDuration = finishTime - startTime
        };

        // Parse environment info from TestSettings
        var testSettings = testRun.Element(TrxNamespace + "TestSettings");
        if (testSettings != null)
        {
            report.Environment["TestSettings"] = testSettings.Attribute("name")?.Value ?? "Unknown";
        }

        // Get test definitions and results
        var testDefinitions = testRun.Element(TrxNamespace + "TestDefinitions")?
            .Elements(TrxNamespace + "UnitTest")
            .ToDictionary(
                t => t.Attribute("id")?.Value ?? "",
                t => (ClassName: t.Element(TrxNamespace + "TestMethod")?.Attribute("className")?.Value ?? "Unknown",
                     TestName: t.Attribute("name")?.Value ?? "Unknown")
            );

        var results = testRun.Element(TrxNamespace + "Results")?
            .Elements(TrxNamespace + "UnitTestResult")
            .ToList() ?? new List<XElement>();

        // Group by feature (className)
        var featureGroups = results
            .GroupBy(r =>
            {
                var testId = r.Attribute("testId")?.Value ?? "";
                return testDefinitions?.TryGetValue(testId, out var def) == true ? def.ClassName : "Unknown";
            });

        foreach (var featureGroup in featureGroups)
        {
            var feature = ParseFeature(featureGroup.Key, featureGroup.ToList(), testDefinitions);
            if (feature.Scenarios.Any())
                report.Features.Add(feature);
        }

        // Calculate statistics
        report.Statistics = CalculateStatistics(report.Features);

        return report;
    }

    private FeatureExecutionResult ParseFeature(string className, List<XElement> testResults, Dictionary<string, (string ClassName, string TestName)> testDefinitions)
    {
        // Extract feature name from className (e.g., "SampleReqnroll.Tests.Features.UserLoginFeature" -> "UserLogin")
        var parts = className.Split('.');
        var featureName = parts.LastOrDefault()?.Replace("Feature", "") ?? "Unknown Feature";
        
        // Add spaces before capital letters for readability
        featureName = System.Text.RegularExpressions.Regex.Replace(featureName, "([a-z])([A-Z])", "$1 $2");

        var feature = new FeatureExecutionResult
        {
            FeatureName = featureName,
            FeatureFilePath = className,
            Duration = TimeSpan.Zero
        };

        foreach (var result in testResults)
        {
            var scenario = ParseScenario(result, testDefinitions);
            feature.Scenarios.Add(scenario);
            feature.Duration += scenario.Duration;
        }

        // Calculate status based on scenarios
        if (feature.Scenarios.All(s => s.Status == ExecutionStatus.Passed))
            feature.Status = ExecutionStatus.Passed;
        else if (feature.Scenarios.Any(s => s.Status == ExecutionStatus.Failed))
            feature.Status = ExecutionStatus.Failed;
        else if (feature.Scenarios.Any(s => s.Status == ExecutionStatus.Skipped))
            feature.Status = ExecutionStatus.Skipped;

        return feature;
    }

    private ScenarioExecutionResult ParseScenario(XElement testResult, Dictionary<string, (string ClassName, string TestName)> testDefinitions)
    {
        var testId = testResult.Attribute("testId")?.Value ?? "";
        var testName = testResult.Attribute("testName")?.Value ?? "Unknown Scenario";
        var outcome = testResult.Attribute("outcome")?.Value ?? "Unknown";
        var duration = testResult.Attribute("duration")?.Value ?? "00:00:00";

        // Get detailed info from test definition
        if (testDefinitions?.TryGetValue(testId, out var def) == true)
        {
            testName = def.TestName;
        }

        // Add spaces before capital letters for readability
        var scenarioName = System.Text.RegularExpressions.Regex.Replace(testName, "([a-z])([A-Z])", "$1 $2");

        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = scenarioName,
            Status = MapOutcomeToStatus(outcome),
            Duration = TimeSpan.Parse(duration),
            StartTime = DateTime.Parse(testResult.Attribute("startTime")?.Value ?? DateTime.UtcNow.ToString()),
            EndTime = DateTime.Parse(testResult.Attribute("endTime")?.Value ?? DateTime.UtcNow.ToString())
        };

        // Parse error messages if failed
        var output = testResult.Element(TrxNamespace + "Output");
        if (output != null)
        {
            var errorInfo = output.Element(TrxNamespace + "ErrorInfo");
            if (errorInfo != null)
            {
                scenario.ErrorMessage = errorInfo.Element(TrxNamespace + "Message")?.Value;
                scenario.StackTrace = errorInfo.Element(TrxNamespace + "StackTrace")?.Value;
            }

            // Parse console output for step information
            var stdOut = output.Element(TrxNamespace + "StdOut")?.Value;
            if (!string.IsNullOrEmpty(stdOut))
            {
                ParseStepsFromOutput(stdOut, scenario);
            }
        }

        return scenario;
    }

    private void ParseStepsFromOutput(string output, ScenarioExecutionResult scenario)
    {
        // Parse Reqnroll/SpecFlow step output format:
        // "Given I am on the login page\n-> done: LoginSteps.GivenIAmOnTheLoginPage() (0.0s)"
        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Match Gherkin keywords
            if (trimmed.StartsWith("Given ") || trimmed.StartsWith("When ") || 
                trimmed.StartsWith("Then ") || trimmed.StartsWith("And ") || 
                trimmed.StartsWith("But "))
            {
                var stepText = trimmed;
                
                scenario.StepResults.Add(new StepExecutionResult
                {
                    Text = stepText,
                    Status = scenario.Status, // Inherit from scenario
                    Duration = TimeSpan.Zero
                });
            }
        }
    }

    private ExecutionStatus MapOutcomeToStatus(string outcome)
    {
        return outcome.ToLower() switch
        {
            "passed" => ExecutionStatus.Passed,
            "failed" => ExecutionStatus.Failed,
            "notexecuted" => ExecutionStatus.Skipped,
            "inconclusive" => ExecutionStatus.Skipped,
            _ => ExecutionStatus.Skipped
        };
    }

    private TestStatistics CalculateStatistics(List<FeatureExecutionResult> features)
    {
        var allScenarios = features.SelectMany(f => f.Scenarios).ToList();
        
        return new TestStatistics
        {
            TotalScenarios = allScenarios.Count,
            PassedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Passed),
            FailedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Failed),
            SkippedScenarios = allScenarios.Count(s => s.Status == ExecutionStatus.Skipped),
            TotalSteps = allScenarios.Sum(s => s.StepResults.Count),
            PassedSteps = allScenarios.SelectMany(s => s.StepResults).Count(st => st.Status == ExecutionStatus.Passed),
            FailedSteps = allScenarios.SelectMany(s => s.StepResults).Count(st => st.Status == ExecutionStatus.Failed),
            SkippedSteps = allScenarios.SelectMany(s => s.StepResults).Count(st => st.Status == ExecutionStatus.Skipped)
        };
    }
}
