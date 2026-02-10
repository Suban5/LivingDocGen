using System.Collections.Generic;
using LivingDocGen.Generator.Models;

namespace LivingDocGen.Generator.Services.Rendering;

/// <summary>
/// Interface for rendering feature content in HTML
/// </summary>
public interface IFeatureRenderer
{
    /// <summary>
    /// Renders a single feature to HTML
    /// </summary>
    /// <param name="feature">The enriched feature to render</param>
    /// <param name="index">The feature index</param>
    /// <param name="forLazyLoading">Whether rendering for lazy loading</param>
    /// <returns>HTML string for the feature</returns>
    string Render(EnrichedFeature feature, int index = 0, bool forLazyLoading = false);
    
    /// <summary>
    /// Generates JSON data for lazy loading features
    /// </summary>
    /// <param name="documentation">The documentation to serialize</param>
    /// <returns>JSON string with feature data</returns>
    string GenerateFeatureDataJson(LivingDocumentation documentation);
    
    /// <summary>
    /// Gets or sets whether to include comments in output
    /// </summary>
    bool IncludeComments { get; set; }
}
