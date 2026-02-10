namespace LivingDocGen.Generator.Services.Assets;

/// <summary>
/// Interface for CSS generation with theme support and caching
/// </summary>
public interface ICssGenerator
{
    /// <summary>
    /// Gets CSS content for the specified theme with caching
    /// </summary>
    /// <param name="options">HTML generation options containing theme</param>
    /// <returns>Complete CSS stylesheet as string</returns>
    string GetCSS(HtmlGenerationOptions options);
}
