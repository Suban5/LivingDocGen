using System;

namespace LivingDocGen.Generator.Models;

/// <summary>
/// Defines the output mode for the documentation generator.
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// Legacy single self-contained HTML file output.
    /// All CSS, JS, and feature data are embedded inline.
    /// Deprecated since v2.1.0 — use <see cref="Chunked"/> for better scalability.
    /// Will be removed in a future major release.
    /// </summary>
    [Obsolete("Legacy output mode is deprecated. Use Chunked mode for better performance with large reports.")]
    Legacy,

    /// <summary>
    /// Chunked output mode producing manifest, index, and per-feature JSON chunks.
    /// Optimized for 3000+ features with on-demand loading.
    /// Produces: feature-manifest.json, feature-index.json, features/*.json, and shell HTML.
    /// Default since v2.1.0.
    /// </summary>
    Chunked
}
