namespace LivingDocGen.Generator.Models;

using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Enriched feature with combined parser and test execution data
/// </summary>
public class EnrichedFeature
{
    public UniversalFeature Feature { get; set; } = new();
    public FeatureExecutionResult? TestResults { get; set; }
    public List<EnrichedScenario> Scenarios { get; set; } = new();
    public ExecutionStatus OverallStatus { get; set; }
    public DateTime? LastExecuted { get; set; }
    public TimeSpan? TotalDuration { get; set; }
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
    public UniversalScenario Scenario { get; set; } = new();
    public ScenarioExecutionResult? TestResult { get; set; }
    public ExecutionStatus Status { get; set; }
    public List<EnrichedStep> Steps { get; set; } = new();
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public int? FailedAtLine { get; set; }
    
    /// <summary>
    /// For scenario outlines: test results for each example row
    /// Key: example row index (0-based), Value: test execution result
    /// </summary>
    public Dictionary<int, ScenarioExecutionResult> ExampleResults { get; set; } = new();
}

/// <summary>
/// Enriched step with execution status
/// </summary>
public class EnrichedStep
{
    public UniversalStep Step { get; set; } = new();
    public StepExecutionResult? TestResult { get; set; }
    public ExecutionStatus Status { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Screenshots { get; set; } = new();
}

/// <summary>
/// Complete living documentation with all data
/// </summary>
public class LivingDocumentation
{
    public string Title { get; set; } = "LivingDoc Living Documentation";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<EnrichedFeature> Features { get; set; } = new();
    public DocumentStatistics Statistics { get; set; } = new();
    public Dictionary<string, int> TagDistribution { get; set; } = new();
    public Dictionary<BDDFramework, int> FrameworkDistribution { get; set; } = new();
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
    public int UntestdScenarios { get; set; }
    public double PassRate => TotalScenarios > 0 ? (double)PassedScenarios / TotalScenarios * 100 : 0;
    public double Coverage => TotalScenarios > 0 ? (double)(TotalScenarios - UntestdScenarios) / TotalScenarios * 100 : 0;
}
