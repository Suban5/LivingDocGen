namespace LivingDocGen.TestReporter.Core;

using LivingDocGen.TestReporter.Models;

/// <summary>
/// Interface for parsing test result files
/// </summary>
public interface ITestResultParser
{
    /// <summary>
    /// Framework supported by this parser
    /// </summary>
    TestFramework SupportedFramework { get; }
    
    /// <summary>
    /// Check if this parser can handle the given file
    /// </summary>
    bool CanParse(string filePath);
    
    /// <summary>
    /// Parse test result file and extract execution results
    /// </summary>
    TestExecutionReport Parse(string filePath);
}
