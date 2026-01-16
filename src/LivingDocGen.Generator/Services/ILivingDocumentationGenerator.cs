using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;

/// <summary>
/// Interface for the main living documentation generator
/// </summary>
public interface ILivingDocumentationGenerator
{
    /// <summary>
    /// Generate living documentation from feature files and test results
    /// </summary>
    /// <param name="featureFiles">Collection of absolute paths to Gherkin feature files (.feature)</param>
    /// <param name="testResultFiles">Collection of absolute paths to test result files (NUnit XML, xUnit XML, TRX, Cucumber JSON)</param>
    /// <param name="title">Title for the generated documentation. Defaults to "BDD Living Documentation"</param>
    /// <param name="options">HTML generation options including theme selection and formatting preferences</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>HTML string containing the complete living documentation</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="System.ArgumentException">Thrown when no valid feature files are provided</exception>
    /// <exception cref="System.OperationCanceledException">Thrown when operation is cancelled via cancellationToken</exception>
    Task<string> GenerateAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string title = null,
        HtmlGenerationOptions options = null,
        CancellationToken cancellationToken = default);
}
