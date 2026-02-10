namespace LivingDocGen.Generator.Services.Assets;

using LivingDocGen.Generator.Models;

/// <summary>
/// Interface for JavaScript generation for HTML reports
/// </summary>
public interface IJavaScriptGenerator
{
    /// <summary>
    /// Generates JavaScript code for the HTML report
    /// </summary>
    /// <param name="documentation">The documentation data</param>
    /// <param name="useLazyRendering">Whether lazy rendering is enabled</param>
    /// <returns>Complete JavaScript code wrapped in script tags</returns>
    string Generate(LivingDocumentation documentation, bool useLazyRendering = false);
}
