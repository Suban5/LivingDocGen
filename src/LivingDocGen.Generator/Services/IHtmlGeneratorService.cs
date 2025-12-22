namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;

/// <summary>
/// Interface for HTML generation service
/// </summary>
public interface IHtmlGeneratorService
{
    /// <summary>
    /// Generate HTML from living documentation
    /// </summary>
    string GenerateHtml(
        LivingDocumentation documentation,
        HtmlGenerationOptions options = null);
}
