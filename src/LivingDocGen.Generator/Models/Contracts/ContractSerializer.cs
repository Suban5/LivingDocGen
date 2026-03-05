using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Serialization and deserialization service for contract files (manifest, index, chunk).
/// Handles JSON serialization with consistent options, schema validation, and hash computation.
/// </summary>
public static class ContractSerializer
{
    /// <summary>
    /// Default JSON serializer options for contract files.
    /// Uses camelCase naming to match JavaScript runtime conventions.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Indented JSON options for human-readable output (debugging/diagnostics).
    /// </summary>
    public static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes a manifest to JSON and computes its build fingerprint.
    /// </summary>
    /// <param name="manifest">The manifest to serialize.</param>
    /// <param name="indented">Whether to produce indented output.</param>
    /// <returns>JSON string representation of the manifest.</returns>
    public static string SerializeManifest(FeatureManifest manifest, bool indented = false)
    {
        if (manifest == null) throw new ArgumentNullException(nameof(manifest));
        var options = indented ? IndentedOptions : DefaultOptions;
        return JsonSerializer.Serialize(manifest, options);
    }

    /// <summary>
    /// Deserializes a manifest from JSON and validates schema compatibility.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="validate">Whether to run structural validation after deserialization.</param>
    /// <returns>Deserialized and optionally validated manifest.</returns>
    public static FeatureManifest DeserializeManifest(string json, bool validate = true)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be null or empty.", nameof(json));

        var manifest = JsonSerializer.Deserialize<FeatureManifest>(json, DefaultOptions);
        if (manifest == null)
            throw new InvalidOperationException("Failed to deserialize manifest: result was null.");

        if (validate)
            ContractValidator.ValidateManifest(manifest);

        return manifest;
    }

    /// <summary>
    /// Serializes a feature index to JSON.
    /// </summary>
    /// <param name="index">The index to serialize.</param>
    /// <param name="indented">Whether to produce indented output.</param>
    /// <returns>JSON string and its SHA-256 hash.</returns>
    public static (string Json, string Hash) SerializeIndex(FeatureIndex index, bool indented = false)
    {
        if (index == null) throw new ArgumentNullException(nameof(index));
        var options = indented ? IndentedOptions : DefaultOptions;
        var json = JsonSerializer.Serialize(index, options);
        var hash = ContractHashValidator.ComputeHash(json);
        return (json, hash);
    }

    /// <summary>
    /// Deserializes a feature index from JSON with optional validation and hash check.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="expectedHash">Optional hash for integrity validation.</param>
    /// <param name="validate">Whether to run structural validation after deserialization.</param>
    /// <returns>Deserialized and optionally validated index.</returns>
    public static FeatureIndex DeserializeIndex(string json, string expectedHash = null, bool validate = true)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be null or empty.", nameof(json));

        // Hash integrity check before deserialization
        if (!string.IsNullOrEmpty(expectedHash) && !ContractHashValidator.ValidateHash(json, expectedHash))
            throw new InvalidOperationException(
                $"Index file hash mismatch. Expected: {expectedHash}, Actual: {ContractHashValidator.ComputeHash(json)}");

        var index = JsonSerializer.Deserialize<FeatureIndex>(json, DefaultOptions);
        if (index == null)
            throw new InvalidOperationException("Failed to deserialize index: result was null.");

        if (validate)
            ContractValidator.ValidateIndex(index);

        return index;
    }

    /// <summary>
    /// Serializes a feature chunk to JSON and computes its content hash.
    /// </summary>
    /// <param name="chunk">The chunk to serialize.</param>
    /// <param name="indented">Whether to produce indented output.</param>
    /// <returns>JSON string and its SHA-256 hash.</returns>
    public static (string Json, string Hash) SerializeChunk(FeatureChunk chunk, bool indented = false)
    {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));

        // Temporarily clear the hash field to compute the hash of the content without it
        var originalHash = chunk.ChunkHash;
        chunk.ChunkHash = string.Empty;

        var options = indented ? IndentedOptions : DefaultOptions;
        var json = JsonSerializer.Serialize(chunk, options);
        var hash = ContractHashValidator.ComputeHash(json);

        // Restore and set the computed hash
        chunk.ChunkHash = hash;

        // Re-serialize with the hash included
        json = JsonSerializer.Serialize(chunk, options);
        return (json, hash);
    }

    /// <summary>
    /// Deserializes a feature chunk from JSON with optional validation and hash check.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="expectedHash">Optional hash for integrity validation (from manifest entry).</param>
    /// <param name="validate">Whether to run structural validation after deserialization.</param>
    /// <returns>Deserialized and optionally validated chunk.</returns>
    public static FeatureChunk DeserializeChunk(string json, string expectedHash = null, bool validate = true)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be null or empty.", nameof(json));

        var chunk = JsonSerializer.Deserialize<FeatureChunk>(json, DefaultOptions);
        if (chunk == null)
            throw new InvalidOperationException("Failed to deserialize chunk: result was null.");

        if (validate)
            ContractValidator.ValidateChunk(chunk, expectedHash);

        return chunk;
    }

    /// <summary>
    /// Serializes a worker message to JSON.
    /// </summary>
    public static string SerializeWorkerMessage(WorkerMessage message, bool indented = false)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var options = indented ? IndentedOptions : DefaultOptions;
        return JsonSerializer.Serialize(message, options);
    }

    /// <summary>
    /// Deserializes a worker message from JSON.
    /// </summary>
    public static WorkerMessage DeserializeWorkerMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON content cannot be null or empty.", nameof(json));

        var message = JsonSerializer.Deserialize<WorkerMessage>(json, DefaultOptions);
        if (message == null)
            throw new InvalidOperationException("Failed to deserialize worker message: result was null.");

        return message;
    }

    /// <summary>
    /// Generates a unique build ID for this generation run.
    /// Format: "yyyyMMddHHmmss-{random8hex}"
    /// </summary>
    /// <returns>A unique build identifier string.</returns>
    public static string GenerateBuildId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8);
        return $"{timestamp}-{random}";
    }
}
