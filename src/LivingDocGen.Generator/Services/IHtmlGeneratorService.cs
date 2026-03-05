namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;

/// <summary>
/// Interface for HTML generation service
/// </summary>
public interface IHtmlGeneratorService
{
    /// <summary>
    /// Generate HTML from living documentation (legacy single-file mode)
    /// </summary>
    string GenerateHtml(
        LivingDocumentation documentation,
        HtmlGenerationOptions options = null);

    /// <summary>
    /// Generate a lightweight shell HTML page for chunked output mode.
    /// The shell contains the header, controls, sidebar placeholder, and main content area,
    /// but no inline feature data. The runtime JavaScript fetches feature-manifest.json
    /// and loads feature chunks on demand.
    /// </summary>
    /// <param name="documentation">The enriched documentation (used for title, stats, theme).</param>
    /// <param name="options">HTML generation options.</param>
    /// <returns>Complete shell HTML string.</returns>
    string GenerateShellHtml(
        LivingDocumentation documentation,
        HtmlGenerationOptions options = null);
}
