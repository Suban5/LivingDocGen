using System.Collections.Generic;
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
    Task<string> GenerateAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string title = null,
        HtmlGenerationOptions options = null);
}
