# LivingDocGen.TestReporter

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](../../LICENSE)

Test result parser for multiple BDD/unit testing frameworks. Normalizes test execution results from NUnit, xUnit, JUnit, MSTest, and SpecFlow into a unified format.

---

## 🎯 Purpose

This library bridges the gap between different test result formats (XML, JSON, TRX) and the LivingDocGen ecosystem. It parses test execution results and provides normalized models containing:
- ✅ Execution status (Pass/Fail/Skip/Inconclusive)
- ⏱️ Execution duration and timestamps
- 📋 Error messages and stack traces
- 📊 Aggregated statistics (pass rate, fail rate)
- 🏷️ Tags and metadata

The generator uses these results to overlay execution status onto feature documentation, creating a living document that shows both specification and current test status.

---

## 🚀 Supported Formats

The library includes auto-detecting parsers for:

| Framework | Format | Extension | Notes |
|-----------|--------|-----------|-------|
| **NUnit 3/4** | XML | `.xml` | Standard NUnit 3+ format (`<test-run>` root) |
| **NUnit 2** | XML | `.xml` | Legacy format (`<test-results>` root) |
| **NUnit 4** | TRX | `.trx` | Default runner output |
| **xUnit** | XML | `.xml` | xUnit v2+ format |
| **JUnit** | XML | `.xml` | CI-friendly format |
| **MSTest / VSTest** | TRX | `.trx` | Visual Studio Test Results |
| **SpecFlow** | JSON | `.json` | SpecFlow execution report |

**Auto-Detection:** The service automatically identifies the format by inspecting file content (root elements, schema patterns).

---

## 🏗️ Architecture

### Core Components

**1. `ITestReportService`** - Main entry point
- Automatically detects file format
- Selects appropriate parser
- Merges multiple test result files
- Provides directory scanning

**2. `ITestResultParser`** - Parser interface
- Implemented by each format-specific parser
- `CanParse(filePath)` - Format detection
- `Parse(filePath)` - Result extraction
- `SupportedFramework` - Framework identifier

**3. `TestExecutionReport`** - Normalized model
- `Statistics` - Aggregated test metrics
- `Features` - List of `FeatureExecutionResult`
- `Framework` - Detected framework type
- `TotalDuration` - Overall execution time

**4. Model Hierarchy**
```
TestExecutionReport
├── TestStatistics (overall metrics)
├── List<FeatureExecutionResult>
│   ├── FeatureName, Tags, Duration
│   └── List<ScenarioExecutionResult>
│       ├── ScenarioName, Status, Duration
│       ├── ErrorMessage, StackTrace
│       └── List<StepExecutionResult>
│           ├── Keyword, Text, Status
│           └── Duration, ErrorMessage
```

### Smart Merging

The `TestReportService` intelligently merges multiple test result files:
- **Scenario-level merging** - Combines results by scenario name
- **Latest-wins strategy** - Most recent execution takes precedence
- **Deduplication** - Removes duplicate scenario executions
- **Statistics aggregation** - Combines counts from all files

**Use Cases:**
- Multi-assembly test suites
- Re-running failed tests (retry logic)
- Parallel test execution with multiple result files
- Splitting tests across CI jobs

---

## 💻 Usage

### Basic Usage

```csharp
using LivingDocGen.TestReporter.Services;

// 1. Initialize the service
var reportService = new TestReportService();

// 2. Parse a single file (auto-detects format)
var report = reportService.ParseTestResults("TestResults/results.trx");

// 3. Access execution statistics
Console.WriteLine($"Total Scenarios: {report.Statistics.TotalScenarios}");
Console.WriteLine($"Passed: {report.Statistics.PassedScenarios}");
Console.WriteLine($"Failed: {report.Statistics.FailedScenarios}");
Console.WriteLine($"Pass Rate: {report.Statistics.PassRate:F2}%");

// 4. Iterate through features and scenarios
foreach (var feature in report.Features)
{
    Console.WriteLine($"\nFeature: {feature.FeatureName}");
    foreach (var scenario in feature.Scenarios)
    {
        Console.WriteLine($"  {scenario.Status} - {scenario.ScenarioName}");
        if (scenario.Status == ExecutionStatus.Failed)
        {
            Console.WriteLine($"    Error: {scenario.ErrorMessage}");
        }
    }
}
```

### Merging Multiple Files

```csharp
// Parse and merge multiple test result files
var mergedReport = reportService.ParseMultipleTestResults(
    "TestResults/api-tests.xml",
    "TestResults/ui-tests.xml",
    "TestResults/retries.trx"
);

// Latest results take precedence for duplicate scenarios
Console.WriteLine($"Total Features: {mergedReport.Features.Count}");
Console.WriteLine($"Total Scenarios: {mergedReport.Statistics.TotalScenarios}");
```

### Directory Scanning

```csharp
// Parse all test result files in a directory
var report = reportService.ParseDirectory("TestResults");

// Parse with custom search pattern
var xmlOnlyReport = reportService.ParseDirectory("TestResults", "*.xml");
```

### Manual Framework Detection

```csharp
// Detect framework before parsing
var framework = reportService.DetectFramework("TestResults/results.xml");
Console.WriteLine($"Detected framework: {framework}");

// Parse based on detection result
if (framework == TestFramework.NUnit)
{
    var report = reportService.ParseTestResults("TestResults/results.xml");
    // Process NUnit-specific logic...
}
```

---

##  Advanced Usage

### Custom Parser Registration

```csharp
// Access available parsers
var service = new TestReportService();

// Parsers are auto-registered:
// - NUnit2ResultParser
// - NUnitResultParser (3.x/4.x)
// - XUnitResultParser
// - JUnitResultParser
// - TrxResultParser
// - SpecFlowJsonResultParser
```

### Error Handling

```csharp
try
{
    var report = reportService.ParseTestResults("invalid-file.xml");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.Message}");
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"Unsupported format: {ex.Message}");
}
```

---

## 🌍 Compatibility

**Target Framework:** .NET Standard 2.0

**Compatible With:**
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 5, 6, 7, 8, 9+
- Xamarin, Mono, Unity

**Dependencies:**
- `System.Text.Json` 8.0.5 (JSON parsing)
- `LivingDocGen.Core` (shared exceptions and utilities)
- `LivingDocGen.Parser` (Gherkin parsing support)

---

## 📊 Model Reference

### ExecutionStatus Enum
```csharp
public enum ExecutionStatus
{
    NotExecuted,    // Test not run
    Passed,         // Test succeeded
    Failed,         // Test failed with error
    Skipped,        // Test explicitly skipped
    Pending,        // Test marked as pending
    Undefined,      // Step definition missing
    Inconclusive    // Test result unclear
}
```

### TestStatistics Properties
```csharp
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
    
    // Calculated properties
    public double PassRate { get; }  // Percentage (0-100)
    public double FailRate { get; }  // Percentage (0-100)
}
```

---

## 📚 Additional Resources

- 📖 [Main Documentation](../../README.md)
- 🏗️ [Architecture Guide](../../docs/ARCHITECTURE.md)
- 📝 [API Reference](../../docs/API_REFERENCE.md)
- 🐛 [Report Issues](https://github.com/suban5/LivingDocGen/issues)

---

## 📄 License

MIT License - Copyright (c) 2024-2026 Suban Dhyako  
See [LICENSE](../../LICENSE) for full details.

---

**Part of the LivingDocGen ecosystem** 🚀

[⬆ Back to Top](#livingdocgentestreporter)