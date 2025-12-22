namespace LivingDocGen.TestReporter.Parsers;

using System.Text.Json;
using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Models;

/// <summary>
/// Parser for SpecFlow JSON test results (SpecFlow+ Runner, LivingDoc)
/// </summary>
public class SpecFlowJsonResultParser : ITestResultParser
{
    public TestFramework SupportedFramework => TestFramework.SpecFlow;

    public bool CanParse(string filePath)
    {
        if (!File.Exists(filePath) || !filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var content = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(content);
            
            // Check for SpecFlow/Cucumber JSON format
            return doc.RootElement.ValueKind == JsonValueKind.Array &&
                   doc.RootElement.EnumerateArray().Any(e => 
                       e.TryGetProperty("name", out _) && 
                       e.TryGetProperty("elements", out _));
        }
        catch
        {
            return false;
        }
    }

    public TestExecutionReport Parse(string filePath)
    {
        var content = File.ReadAllText(filePath);
        var features = JsonSerializer.Deserialize<List<CucumberJsonFeature>>(content) 
            ?? throw new InvalidOperationException("Invalid SpecFlow/Cucumber JSON format");

        var report = new TestExecutionReport
        {
            ReportName = Path.GetFileName(filePath),
            Framework = TestFramework.SpecFlow,
            GeneratedAt = DateTime.UtcNow
        };

        foreach (var feature in features)
        {
            var featureResult = ParseFeature(feature);
            report.Features.Add(featureResult);
            report.TotalDuration += featureResult.Duration;
        }

        // Calculate statistics
        report.Statistics = CalculateStatistics(report.Features);

        return report;
    }

    private FeatureExecutionResult ParseFeature(CucumberJsonFeature feature)
    {
        var featureResult = new FeatureExecutionResult
        {
            FeatureName = feature.Name ?? "Unknown Feature",
            FeatureFilePath = feature.Uri ?? "",
            Tags = feature.Tags?.Select(t => t.Name ?? "").ToList() ?? new List<string>()
        };

        foreach (var element in feature.Elements ?? Enumerable.Empty<CucumberJsonElement>())
        {
            var scenario = ParseScenario(element);
            featureResult.Scenarios.Add(scenario);
            featureResult.Duration += scenario.Duration;
        }

        // Determine overall feature status
        if (featureResult.Scenarios.Any(s => s.Status == ExecutionStatus.Failed))
            featureResult.Status = ExecutionStatus.Failed;
        else if (featureResult.Scenarios.All(s => s.Status == ExecutionStatus.Skipped))
            featureResult.Status = ExecutionStatus.Skipped;
        else if (featureResult.Scenarios.Any(s => s.Status == ExecutionStatus.Passed))
            featureResult.Status = ExecutionStatus.Passed;
        else
            featureResult.Status = ExecutionStatus.NotExecuted;

        return featureResult;
    }

    private ScenarioExecutionResult ParseScenario(CucumberJsonElement element)
    {
        var scenario = new ScenarioExecutionResult
        {
            ScenarioName = element.Name ?? "Unknown Scenario",
            Tags = element.Tags?.Select(t => t.Name ?? "").ToList() ?? new List<string>()
        };

        long totalNanoseconds = 0;

        foreach (var step in element.Steps ?? Enumerable.Empty<CucumberJsonStep>())
        {
            var stepResult = ParseStep(step);
            scenario.StepResults.Add(stepResult);
            totalNanoseconds += step.Result?.Duration ?? 0;

            // Track first failure
            if (stepResult.Status == ExecutionStatus.Failed && scenario.Status != ExecutionStatus.Failed)
            {
                scenario.Status = ExecutionStatus.Failed;
                scenario.ErrorMessage = stepResult.ErrorMessage;
                scenario.StackTrace = stepResult.StackTrace;
            }
        }

        scenario.Duration = TimeSpan.FromTicks(totalNanoseconds / 100); // Convert nanoseconds to ticks

        // Determine scenario status if not already failed
        if (scenario.Status == ExecutionStatus.NotExecuted)
        {
            if (scenario.StepResults.All(s => s.Status == ExecutionStatus.Passed))
                scenario.Status = ExecutionStatus.Passed;
            else if (scenario.StepResults.Any(s => s.Status == ExecutionStatus.Skipped))
                scenario.Status = ExecutionStatus.Skipped;
            else if (scenario.StepResults.Any(s => s.Status == ExecutionStatus.Undefined))
                scenario.Status = ExecutionStatus.Undefined;
        }

        return scenario;
    }

    private StepExecutionResult ParseStep(CucumberJsonStep step)
    {
        var stepResult = new StepExecutionResult
        {
            Keyword = step.Keyword ?? "",
            Text = step.Name ?? "",
            LineNumber = step.Line,
            Status = MapResultToStatus(step.Result?.Status),
            Duration = TimeSpan.FromTicks((step.Result?.Duration ?? 0) / 100) // nanoseconds to ticks
        };

        if (step.Result != null && step.Result.Status == "failed")
        {
            stepResult.ErrorMessage = step.Result.ErrorMessage;
            
            // Extract stack trace and line number from error message
            if (!string.IsNullOrEmpty(step.Result.ErrorMessage))
            {
                var lines = step.Result.ErrorMessage.Split('\n');
                stepResult.StackTrace = string.Join("\n", lines.Skip(1));
            }
        }

        // Handle embeddings (screenshots, attachments)
        if (step.Embeddings != null)
        {
            foreach (var embedding in step.Embeddings)
            {
                if (embedding.MimeType?.StartsWith("image/") == true)
                {
                    stepResult.Screenshots.Add($"data:{embedding.MimeType};base64,{embedding.Data}");
                }
            }
        }

        return stepResult;
    }

    private ExecutionStatus MapResultToStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "passed" => ExecutionStatus.Passed,
            "failed" => ExecutionStatus.Failed,
            "skipped" => ExecutionStatus.Skipped,
            "pending" => ExecutionStatus.Pending,
            "undefined" => ExecutionStatus.Undefined,
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
                }

                stats.TotalSteps += scenario.StepResults.Count;
                stats.PassedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Passed);
                stats.FailedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Failed);
                stats.SkippedSteps += scenario.StepResults.Count(s => s.Status == ExecutionStatus.Skipped);
            }
        }

        return stats;
    }

    // DTO classes for Cucumber/SpecFlow JSON format
    private class CucumberJsonFeature
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("uri")]
        public string? Uri { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("tags")]
        public List<CucumberJsonTag>? Tags { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("elements")]
        public List<CucumberJsonElement>? Elements { get; set; }
    }

    private class CucumberJsonElement
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("line")]
        public int Line { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("tags")]
        public List<CucumberJsonTag>? Tags { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("steps")]
        public List<CucumberJsonStep>? Steps { get; set; }
    }

    private class CucumberJsonStep
    {
        [System.Text.Json.Serialization.JsonPropertyName("keyword")]
        public string? Keyword { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("line")]
        public int Line { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("result")]
        public CucumberJsonResult? Result { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("embeddings")]
        public List<CucumberJsonEmbedding>? Embeddings { get; set; }
    }

    private class CucumberJsonResult
    {
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("duration")]
        public long Duration { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
    }

    private class CucumberJsonTag
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class CucumberJsonEmbedding
    {
        [System.Text.Json.Serialization.JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public string? Data { get; set; }
    }
}
