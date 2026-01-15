using System;
using System.Collections.Generic;

namespace LivingDocGen.Generator.Models;

using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Enriched feature with combined parser and test execution data
/// </summary>
public class EnrichedFeature
{
    public UniversalFeature Feature { get; set; } = new UniversalFeature();
    public FeatureExecutionResult TestResults { get; set; }
    public List<EnrichedScenario> Scenarios { get; set; } = new List<EnrichedScenario>();
    public ExecutionStatus OverallStatus { get; set; }
    public DateTime LastExecuted { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public double PassRate => Scenarios.Count > 0 ? (double)PassedCount / Scenarios.Count * 100 : 0;
}

/// <summary>
/// Enriched scenario with test execution data
/// </summary>
public class EnrichedScenario
{
    public UniversalScenario Scenario { get; set; } = new UniversalScenario();
    public ScenarioExecutionResult TestResult { get; set; }
    public ExecutionStatus Status { get; set; }
    public List<EnrichedStep> Steps { get; set; } = new List<EnrichedStep>();
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
    public int FailedAtLine { get; set; }
    
    /// <summary>
    /// For scenario outlines: test results for each example row
    /// Key: example row index (0-based), Value: test execution result
    /// </summary>
    public Dictionary<int, ScenarioExecutionResult> ExampleResults { get; set; } = new Dictionary<int, ScenarioExecutionResult>();
}

/// <summary>
/// Enriched step with execution status
/// </summary>
public class EnrichedStep
{
    public UniversalStep Step { get; set; } = new UniversalStep();
    public StepExecutionResult TestResult { get; set; }
    public ExecutionStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string ErrorMessage { get; set; }
    public List<string> Screenshots { get; set; } = new List<string>();
}

/// <summary>
/// Complete living documentation with all data
/// </summary>
public class LivingDocumentation
{
    public string Title { get; set; } = "LivingDoc Living Documentation";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<EnrichedFeature> Features { get; set; } = new List<EnrichedFeature>();
    public DocumentStatistics Statistics { get; set; } = new DocumentStatistics();
    public Dictionary<string, int> TagDistribution { get; set; } = new Dictionary<string, int>();
    public Dictionary<BDDFramework, int> FrameworkDistribution { get; set; } = new Dictionary<BDDFramework, int>();
}

/// <summary>
/// Overall documentation statistics
/// </summary>
public class DocumentStatistics
{
    public int TotalFeatures { get; set; }
    public int TotalScenarios { get; set; }
    public int TotalSteps { get; set; }
    public int PassedScenarios { get; set; }
    public int FailedScenarios { get; set; }
    public int SkippedScenarios { get; set; }
    public int UntestedScenarios { get; set; }

    // Total executed scenarios (excluding untested)
    public int ExecutedScenarios => PassedScenarios + FailedScenarios + SkippedScenarios;
    
    // Rates based on executed scenarios only
    public double PassRate => ExecutedScenarios > 0 ? (double)PassedScenarios / ExecutedScenarios * 100 : 0;
    public double FailRate => ExecutedScenarios > 0 ? (double)FailedScenarios / ExecutedScenarios * 100 : 0;
    public double SkipRate => ExecutedScenarios > 0 ? (double)SkippedScenarios / ExecutedScenarios * 100 : 0;
    
    // Coverage: percentage of scenarios that have been tested (executed vs total)
    public double Coverage => TotalScenarios > 0 ? (double)ExecutedScenarios / TotalScenarios * 100 : 0;
}
