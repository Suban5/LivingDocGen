using System;
using System.Security.Cryptography;
using System.Text;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Provides SHA-256 hash computation and validation for contract files.
/// Used to verify integrity of manifest, index, and chunk files at load time.
/// </summary>
public static class ContractHashValidator
{
    /// <summary>
    /// Computes a SHA-256 hash of the given content and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="content">The string content to hash.</param>
    /// <returns>Lowercase hex-encoded SHA-256 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    public static string ComputeHash(string content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hashBytes = sha256.ComputeHash(bytes);
            return BytesToHex(hashBytes);
        }
    }

    /// <summary>
    /// Computes a SHA-256 hash of the given byte array and returns it as a lowercase hex string.
    /// </summary>
    /// <param name="content">The byte content to hash.</param>
    /// <returns>Lowercase hex-encoded SHA-256 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
    public static string ComputeHash(byte[] content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(content);
            return BytesToHex(hashBytes);
        }
    }

    /// <summary>
    /// Validates that the given content matches the expected hash.
    /// </summary>
    /// <param name="content">The content to validate.</param>
    /// <param name="expectedHash">The expected SHA-256 hash (lowercase hex).</param>
    /// <returns>True if the content hash matches the expected hash.</returns>
    public static bool ValidateHash(string content, string expectedHash)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(expectedHash))
            return false;

        var actualHash = ComputeHash(content);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the given byte content matches the expected hash.
    /// </summary>
    /// <param name="content">The byte content to validate.</param>
    /// <param name="expectedHash">The expected SHA-256 hash (lowercase hex).</param>
    /// <returns>True if the content hash matches the expected hash.</returns>
    public static bool ValidateHash(byte[] content, string expectedHash)
    {
        if (content == null || string.IsNullOrEmpty(expectedHash))
            return false;

        var actualHash = ComputeHash(content);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a deterministic feature ID from the feature file path and name.
    /// Uses SHA-256 truncated to 12 hex characters for compact IDs.
    /// </summary>
    /// <param name="filePath">The relative file path of the feature.</param>
    /// <param name="featureName">The name of the feature.</param>
    /// <returns>A 12-character hex string as a deterministic feature ID.</returns>
    public static string GenerateFeatureId(string filePath, string featureName)
    {
        var input = $"{NormalizePath(filePath)}::{featureName ?? string.Empty}";
        var fullHash = ComputeHash(input);
        // Truncate to 12 hex chars for compact IDs (48 bits of entropy)
        return fullHash.Substring(0, 12);
    }

    /// <summary>
    /// Generates a deterministic scenario ID from feature ID, scenario name, and line number.
    /// </summary>
    /// <param name="featureId">The owning feature's deterministic ID.</param>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="lineNumber">The line number of the scenario in the feature file.</param>
    /// <returns>A 12-character hex string as a deterministic scenario ID.</returns>
    public static string GenerateScenarioId(string featureId, string scenarioName, int lineNumber)
    {
        var input = $"{featureId}::{scenarioName ?? string.Empty}::{lineNumber}";
        var fullHash = ComputeHash(input);
        return fullHash.Substring(0, 12);
    }

    /// <summary>
    /// Normalizes a file path for consistent hashing (forward slashes, lowercase).
    /// </summary>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        return path.Replace('\\', '/').ToLowerInvariant().TrimStart('/');
    }

    /// <summary>
    /// Converts a byte array to a lowercase hex string.
    /// </summary>
    private static string BytesToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
