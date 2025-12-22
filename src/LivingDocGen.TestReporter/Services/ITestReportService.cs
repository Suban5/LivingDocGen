using System.Collections.Generic;

namespace LivingDocGen.TestReporter.Services;

using LivingDocGen.TestReporter.Models;

/// <summary>
/// Interface for the test report service
/// </summary>
public interface ITestReportService
{
    /// <summary>
    /// Parse a test result file using the appropriate parser
    /// </summary>
    TestExecutionReport ParseTestResults(string filePath);

    /// <summary>
    /// Parse multiple test result files and merge into a single report
    /// </summary>
    TestExecutionReport ParseMultipleTestResults(params string[] filePaths);

    /// <summary>
    /// Detect test framework from file
    /// </summary>
    TestFramework DetectFramework(string filePath);
}
