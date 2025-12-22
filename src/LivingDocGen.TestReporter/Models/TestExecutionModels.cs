using System;
using System.Collections.Generic;

namespace LivingDocGen.TestReporter.Models;

/// <summary>
/// Execution status for scenarios and steps
/// </summary>
public enum ExecutionStatus
{
    NotExecuted,
    Passed,
    Failed,
    Skipped,
    Pending,
    Undefined,
    Inconclusive
}

/// <summary>
/// Test execution result for a scenario
/// </summary>
public class ScenarioExecutionResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
    public ExecutionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
    public List<StepExecutionResult> StepResults { get; set; } = new List<StepExecutionResult>();
    public List<string> Screenshots { get; set; } = new List<string>();
    public List<string> Attachments { get; set; } = new List<string>();
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Test execution result for a step
/// </summary>
public class StepExecutionResult
{
    public string Keyword { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public ExecutionStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
    public List<string> Screenshots { get; set; } = new List<string>();
}

/// <summary>
/// Complete test execution report
/// </summary>
public class TestExecutionReport
{
    public string ReportName { get; set; } = string.Empty;
    public TestFramework Framework { get; set; }
    public DateTime GeneratedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TestStatistics Statistics { get; set; } = new TestStatistics();
    public List<FeatureExecutionResult> Features { get; set; } = new List<FeatureExecutionResult>();
    public Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Feature-level execution result
/// </summary>
public class FeatureExecutionResult
{
    public string FeatureName { get; set; } = string.Empty;
    public string FeatureFilePath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
    public List<ScenarioExecutionResult> Scenarios { get; set; } = new List<ScenarioExecutionResult>();
    public TimeSpan Duration { get; set; }
    public ExecutionStatus Status { get; set; }
}

/// <summary>
/// Test execution statistics
/// </summary>
public class TestStatistics
{
    public int TotalScenarios { get; set; }
    public int PassedScenarios { get; set; }
    public int FailedScenarios { get; set; }
    public int SkippedScenarios { get; set; }
    public int InconclusiveScenarios { get; set; }
    public int TotalSteps { get; set; }
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
    public int SkippedSteps { get; set; }
    public double PassRate => TotalScenarios > 0 ? (double)PassedScenarios / TotalScenarios * 100 : 0;
    public double FailRate => TotalScenarios > 0 ? (double)FailedScenarios / TotalScenarios * 100 : 0;
}

/// <summary>
/// Supported test frameworks
/// </summary>
public enum TestFramework
{
    NUnit,
    XUnit,
    JUnit,
    MSTest,
    SpecFlow,
    Cucumber,
    Unknown
}
