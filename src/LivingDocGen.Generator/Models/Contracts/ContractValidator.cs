using System;
using LivingDocGen.Core.Exceptions;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Validates contract files for schema compatibility, required fields, and hash integrity.
/// Used at both generation time (post-emit validation) and runtime (pre-load validation).
/// </summary>
public static class ContractValidator
{
    /// <summary>
    /// Validates a manifest contract for completeness and integrity.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateManifest(FeatureManifest manifest)
    {
        if (manifest == null)
            throw new ValidationException("Manifest cannot be null.");

        ValidateContractBase(manifest, "Manifest");

        if (string.IsNullOrWhiteSpace(manifest.IndexFile))
            throw new ValidationException("Manifest.IndexFile is required.");

        if (manifest.TotalFeatures < 0)
            throw new ValidationException("Manifest.TotalFeatures cannot be negative.");

        if (manifest.TotalScenarios < 0)
            throw new ValidationException("Manifest.TotalScenarios cannot be negative.");

        if (manifest.ChunkCount < 0)
            throw new ValidationException("Manifest.ChunkCount cannot be negative.");

        if (manifest.FeatureMap == null)
            throw new ValidationException("Manifest.FeatureMap cannot be null.");

        if (manifest.FeatureMap.Count != manifest.TotalFeatures)
            throw new ValidationException(
                $"Manifest.FeatureMap count ({manifest.FeatureMap.Count}) does not match TotalFeatures ({manifest.TotalFeatures}).");

        for (int i = 0; i < manifest.FeatureMap.Count; i++)
        {
            ValidateManifestEntry(manifest.FeatureMap[i], i);
        }
    }

    /// <summary>
    /// Validates a single manifest entry.
    /// </summary>
    private static void ValidateManifestEntry(FeatureManifestEntry entry, int index)
    {
        if (entry == null)
            throw new ValidationException($"Manifest.FeatureMap[{index}] cannot be null.");

        if (string.IsNullOrWhiteSpace(entry.FeatureId))
            throw new ValidationException($"Manifest.FeatureMap[{index}].FeatureId is required.");

        if (string.IsNullOrWhiteSpace(entry.Name))
            throw new ValidationException($"Manifest.FeatureMap[{index}].Name is required.");

        if (string.IsNullOrWhiteSpace(entry.ChunkFile))
            throw new ValidationException($"Manifest.FeatureMap[{index}].ChunkFile is required.");

        var validStatuses = new[] { "passed", "failed", "skipped", "untested" };
        if (Array.IndexOf(validStatuses, entry.Status) < 0)
            throw new ValidationException(
                $"Manifest.FeatureMap[{index}].Status '{entry.Status}' is invalid. Must be one of: passed, failed, skipped, untested.");
    }

    /// <summary>
    /// Validates a feature index contract for completeness.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateIndex(FeatureIndex index)
    {
        if (index == null)
            throw new ValidationException("Index cannot be null.");

        ValidateContractBase(index, "Index");

        if (index.ScenarioRecords == null)
            throw new ValidationException("Index.ScenarioRecords cannot be null.");

        if (index.InvertedTokenIndex == null)
            throw new ValidationException("Index.InvertedTokenIndex cannot be null.");

        if (index.StatusIndex == null)
            throw new ValidationException("Index.StatusIndex cannot be null.");

        if (index.TagIndex == null)
            throw new ValidationException("Index.TagIndex cannot be null.");

        // Validate ordinal consistency
        for (int i = 0; i < index.ScenarioRecords.Count; i++)
        {
            var record = index.ScenarioRecords[i];
            if (record.ScenarioOrdinal != i)
                throw new ValidationException(
                    $"Index.ScenarioRecords[{i}].ScenarioOrdinal ({record.ScenarioOrdinal}) does not match list position ({i}).");
        }
    }

    /// <summary>
    /// Validates a feature chunk contract for completeness and optional hash integrity.
    /// </summary>
    /// <param name="chunk">The chunk to validate.</param>
    /// <param name="expectedHash">Optional expected hash for integrity check. Null to skip.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateChunk(FeatureChunk chunk, string expectedHash = null)
    {
        if (chunk == null)
            throw new ValidationException("Chunk cannot be null.");

        ValidateContractBase(chunk, "Chunk");

        if (string.IsNullOrWhiteSpace(chunk.FeatureId))
            throw new ValidationException("Chunk.FeatureId is required.");

        if (string.IsNullOrWhiteSpace(chunk.Name))
            throw new ValidationException("Chunk.Name is required.");

        if (chunk.Html == null)
            throw new ValidationException("Chunk.Html cannot be null.");

        if (chunk.ScenarioSummary == null)
            throw new ValidationException("Chunk.ScenarioSummary cannot be null.");

        var validStatuses = new[] { "passed", "failed", "skipped", "untested" };
        if (Array.IndexOf(validStatuses, chunk.Status) < 0)
            throw new ValidationException(
                $"Chunk.Status '{chunk.Status}' is invalid. Must be one of: passed, failed, skipped, untested.");

        // Validate hash integrity if expected hash is provided
        if (!string.IsNullOrEmpty(expectedHash) && !string.IsNullOrEmpty(chunk.ChunkHash))
        {
            if (!string.Equals(chunk.ChunkHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new ValidationException(
                    $"Chunk hash mismatch for feature '{chunk.FeatureId}'. Expected: {expectedHash}, Actual: {chunk.ChunkHash}");
        }
    }

    /// <summary>
    /// Validates the base contract fields shared by all contract types.
    /// </summary>
    /// <param name="contract">The contract base to validate.</param>
    /// <param name="contractName">Name for error messages.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public static void ValidateContractBase(ContractBase contract, string contractName)
    {
        if (string.IsNullOrWhiteSpace(contract.SchemaVersion))
            throw new ValidationException($"{contractName}.SchemaVersion is required.");

        if (string.IsNullOrWhiteSpace(contract.GeneratorVersion))
            throw new ValidationException($"{contractName}.GeneratorVersion is required.");

        if (string.IsNullOrWhiteSpace(contract.BuildId))
            throw new ValidationException($"{contractName}.BuildId is required.");

        if (!ContractVersion.IsCompatible(contract.SchemaVersion))
            throw new ValidationException(
                $"{contractName}.SchemaVersion '{contract.SchemaVersion}' is not compatible with runtime. " +
                $"Minimum required: {ContractVersion.CompatibilityMinVersion}, Current: {ContractVersion.SchemaVersion}.");
    }

    /// <summary>
    /// Validates that an index file's content matches its expected hash from the manifest.
    /// </summary>
    /// <param name="indexContent">The raw JSON content of the index file.</param>
    /// <param name="expectedHash">The hash from the manifest's IndexHash field.</param>
    /// <returns>True if the hash matches or if expectedHash is empty (no hash to validate).</returns>
    public static bool ValidateIndexHash(string indexContent, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return true; // No hash to validate against

        return ContractHashValidator.ValidateHash(indexContent, expectedHash);
    }

    /// <summary>
    /// Validates that a chunk file's content matches its expected hash from the manifest entry.
    /// </summary>
    /// <param name="chunkContent">The raw JSON content of the chunk file.</param>
    /// <param name="expectedHash">The hash from the manifest entry's ChunkHash field.</param>
    /// <returns>True if the hash matches or if expectedHash is empty.</returns>
    public static bool ValidateChunkHash(string chunkContent, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return true;

        return ContractHashValidator.ValidateHash(chunkContent, expectedHash);
    }
}
