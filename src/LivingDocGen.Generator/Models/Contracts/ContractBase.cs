using System;
using System.Collections.Generic;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Base class shared by all contract files (manifest, index, chunk).
/// Contains versioning, build identity, and optional extension metadata.
/// </summary>
public abstract class ContractBase
{
    /// <summary>
    /// Schema version of this contract file (e.g. "1.0").
    /// </summary>
    public string SchemaVersion { get; set; } = ContractVersion.SchemaVersion;

    /// <summary>
    /// Version of the generator that produced this file.
    /// </summary>
    public string GeneratorVersion { get; set; } = ContractVersion.GeneratorVersion;

    /// <summary>
    /// Minimum runtime schema version required to consume this file.
    /// </summary>
    public string CompatibilityMinVersion { get; set; } = ContractVersion.CompatibilityMinVersion;

    /// <summary>
    /// Unique identifier for the build run that produced this artifact.
    /// Used for cache invalidation and diagnostics.
    /// </summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>
    /// Forward-compatible extension envelope.
    /// Consumers should ignore unknown keys without failing.
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
}
