# LivingDocGen.TestReporter

**LivingDocGen.TestReporter** is the component responsible for parsing test execution results from various testing frameworks and normalizing them into a standard format.

## 🎯 Purpose

This library bridges the gap between different test result formats (XML, JSON, TRX) and the LivingDocGen ecosystem. It allows the generator to overlay execution status (Pass/Fail/Skip), error messages, and duration onto the static feature documentation.

## 🚀 Supported Formats

The library includes parsers for the following formats:

*   **NUnit 3/4** (`.xml`): Standard NUnit 3+ XML output.
*   **NUnit 2** (`.xml`): Legacy NUnit 2 XML format.
*   **xUnit** (`.xml`): Standard xUnit v2 XML output.
*   **JUnit** (`.xml`): Common format used by many CI tools and Java-based runners.
*   **MSTest / VSTest** (`.trx`): Microsoft Test Results format (also used by NUnit 4 default runner).
*   **SpecFlow** (`.json`): SpecFlow execution report JSON format.

## 🏗 Architecture

### Core Components

1.  **`ITestReportService`**: The main entry point. It automatically detects the file format and selects the appropriate parser.
2.  **`ITestResultParser`**: The interface implemented by all specific parsers.
3.  **`TestExecutionReport`**: The normalized model containing execution statistics and a list of `FeatureExecutionResult` objects.

### Smart Merging

The `TestReportService` includes logic to **merge multiple test result files**. This is useful when:
*   Tests are split across multiple assemblies.
*   Tests are re-run (e.g., re-running failed tests). The service intelligently merges results, prioritizing the most recent execution for each test case.

## 💻 Usage

```csharp
using LivingDocGen.TestReporter.Services;

// 1. Initialize the service
var reportService = new TestReportService();

// 2. Parse a single file (auto-detects format)
var report = reportService.ParseTestResults("TestResults/results.trx");

Console.WriteLine($"Total Scenarios: {report.Statistics.TotalScenarios}");
Console.WriteLine($"Passed: {report.Statistics.PassedScenarios}");
Console.WriteLine($"Failed: {report.Statistics.FailedScenarios}");

// 3. Parse and merge multiple files
var mergedReport = reportService.ParseMultipleTestResults(new[] 
{
    "TestResults/run1.xml",
    "TestResults/run2-retries.xml"
});
```

## 📝 Todo List

- [ ] Add support for **Cucumber JSON** output format.
- [ ] Improve error message extraction for complex stack traces.
