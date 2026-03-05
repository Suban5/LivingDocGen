using System;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Defines schema version constants and compatibility rules for chunked output contracts.
/// All manifest, index, and chunk files include these fields for runtime compatibility checks.
/// </summary>
public static class ContractVersion
{
    /// <summary>
    /// Current schema version for all contracts (manifest, index, chunk, worker).
    /// Bump major on breaking changes, minor on backward-compatible additions.
    /// </summary>
    public const string SchemaVersion = "1.0";

    /// <summary>
    /// Minimum schema version that the current runtime can consume.
    /// Used for forward-compatibility checks: if a file's schemaVersion is below this,
    /// the runtime should hard-fail with a clear error.
    /// </summary>
    public const string CompatibilityMinVersion = "1.0";

    /// <summary>
    /// Generator version that produced the contracts (matches assembly version).
    /// Used for diagnostics and cache invalidation.
    /// </summary>
    public static string GeneratorVersion { get; } =
        typeof(ContractVersion).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    /// <summary>
    /// Validates that a given schema version is compatible with the current runtime.
    /// </summary>
    /// <param name="schemaVersion">The schema version from the contract file.</param>
    /// <returns>True if compatible; false otherwise.</returns>
    public static bool IsCompatible(string schemaVersion)
    {
        if (string.IsNullOrWhiteSpace(schemaVersion))
            return false;

        if (!TryParseVersion(schemaVersion, out var major, out var minor))
            return false;

        if (!TryParseVersion(CompatibilityMinVersion, out var minMajor, out var minMinor))
            return false;

        // Major version must match (breaking change boundary)
        if (major != minMajor)
            return false;

        // Minor must be >= minimum
        return minor >= minMinor;
    }

    /// <summary>
    /// Parses a "major.minor" version string.
    /// </summary>
    private static bool TryParseVersion(string version, out int major, out int minor)
    {
        major = 0;
        minor = 0;

        var parts = version.Split('.');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
    }
}
