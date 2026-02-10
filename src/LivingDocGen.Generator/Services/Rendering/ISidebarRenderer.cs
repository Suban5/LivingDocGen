using System.Collections.Generic;
using LivingDocGen.Generator.Models;

namespace LivingDocGen.Generator.Services.Rendering;

/// <summary>
/// Interface for rendering sidebar navigation
/// </summary>
public interface ISidebarRenderer
{
    /// <summary>
    /// Renders the sidebar HTML with folder tree navigation
    /// </summary>
    /// <param name="documentation">The documentation data</param>
    /// <returns>Complete sidebar HTML</returns>
    string Render(LivingDocumentation documentation);
}
