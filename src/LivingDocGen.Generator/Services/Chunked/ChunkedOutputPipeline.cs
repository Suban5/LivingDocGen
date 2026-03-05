using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;

namespace LivingDocGen.Generator.Services.Chunked;

/// <summary>
/// Result of a chunked output pipeline run, containing paths to all emitted artifacts.
/// </summary>
public class ChunkedOutputResult
{
    /// <summary>Absolute path to the emitted feature-manifest.json.</summary>
    public string ManifestPath { get; set; } = string.Empty;

    /// <summary>Absolute path to the emitted feature-index.json.</summary>
    public string IndexPath { get; set; } = string.Empty;

    /// <summary>Absolute paths to all emitted feature chunk JSON files.</summary>
    public List<string> ChunkPaths { get; set; } = new List<string>();

    /// <summary>The build ID used for this generation run.</summary>
    public string BuildId { get; set; } = string.Empty;

    /// <summary>Total number of features processed.</summary>
    public int TotalFeatures { get; set; }

    /// <summary>Total number of scenarios processed.</summary>
    public int TotalScenarios { get; set; }

    /// <summary>Absolute path to the emitted shell HTML file (index.html).</summary>
    public string ShellHtmlPath { get; set; } = string.Empty;
}

/// <summary>
/// Orchestrates the chunked output pipeline: builds chunks, index, manifest,
/// and writes all artifacts to disk.
/// </summary>
public interface IChunkedOutputPipeline
{
    /// <summary>
    /// Executes the full chunked output pipeline.
    /// </summary>
    /// <param name="documentation">Enriched living documentation.</param>
    /// <param name="outputDirectory">Directory to write artifacts into.</param>
    /// <param name="options">HTML generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing paths to all emitted artifacts.</returns>
    Task<ChunkedOutputResult> ExecuteAsync(
        LivingDocumentation documentation,
        string outputDirectory,
        HtmlGenerationOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of the chunked output pipeline.
/// Coordinates chunk emission → index building → manifest building → file writes.
/// </summary>
public class ChunkedOutputPipeline : IChunkedOutputPipeline
{
    private readonly IChunkEmitterService _chunkEmitter;
    private readonly IIndexBuilderService _indexBuilder;
    private readonly IManifestBuilderService _manifestBuilder;
    private readonly IHtmlGeneratorService _htmlGenerator;

    public ChunkedOutputPipeline(
        IChunkEmitterService chunkEmitter,
        IIndexBuilderService indexBuilder,
        IManifestBuilderService manifestBuilder,
        IHtmlGeneratorService htmlGenerator = null)
    {
        _chunkEmitter = chunkEmitter ?? throw new ArgumentNullException(nameof(chunkEmitter));
        _indexBuilder = indexBuilder ?? throw new ArgumentNullException(nameof(indexBuilder));
        _manifestBuilder = manifestBuilder ?? throw new ArgumentNullException(nameof(manifestBuilder));
        _htmlGenerator = htmlGenerator;
    }

    /// <inheritdoc/>
    public async Task<ChunkedOutputResult> ExecuteAsync(
        LivingDocumentation documentation,
        string outputDirectory,
        HtmlGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (documentation == null)
            throw new ArgumentNullException(nameof(documentation));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        options ??= new HtmlGenerationOptions();

        var buildId = ContractSerializer.GenerateBuildId();

        // Ensure output directories exist
        Directory.CreateDirectory(outputDirectory);
        var featuresDir = Path.Combine(outputDirectory, "features");
        Directory.CreateDirectory(featuresDir);

        var result = new ChunkedOutputResult
        {
            BuildId = buildId,
            TotalFeatures = documentation.Features.Count
        };

        // Step 1: Build and serialize all feature chunks
        var chunkMetadata = new Dictionary<string, ChunkMetadata>();

        for (int i = 0; i < documentation.Features.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var feature = documentation.Features[i];
            var chunk = _chunkEmitter.BuildChunk(feature, i, buildId, options);
            var (chunkJson, chunkHash) = _chunkEmitter.SerializeChunk(chunk);

            // Write chunk file
            var chunkFileName = $"{chunk.FeatureId}.json";
            var chunkPath = Path.Combine(featuresDir, chunkFileName);
            await WriteFileAsync(chunkPath, chunkJson, cancellationToken).ConfigureAwait(false);
            result.ChunkPaths.Add(chunkPath);

            // Track metadata for manifest
            chunkMetadata[chunk.FeatureId] = new ChunkMetadata
            {
                ChunkHash = chunkHash,
                EstimatedBytes = Encoding.UTF8.GetByteCount(chunkJson)
            };
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Build and serialize the feature index
        var featureIndex = _indexBuilder.BuildIndex(documentation, buildId);
        var (indexJson, indexHash) = ContractSerializer.SerializeIndex(featureIndex);

        var indexPath = Path.Combine(outputDirectory, "feature-index.json");
        await WriteFileAsync(indexPath, indexJson, cancellationToken).ConfigureAwait(false);
        result.IndexPath = indexPath;

        cancellationToken.ThrowIfCancellationRequested();

        // Step 3: Build and serialize the manifest (needs chunk metadata + index hash)
        var manifest = _manifestBuilder.BuildManifest(documentation, buildId, chunkMetadata);
        manifest.IndexHash = indexHash;
        var manifestJson = ContractSerializer.SerializeManifest(manifest, indented: true);

        var manifestPath = Path.Combine(outputDirectory, "feature-manifest.json");
        await WriteFileAsync(manifestPath, manifestJson, cancellationToken).ConfigureAwait(false);
        result.ManifestPath = manifestPath;

        result.TotalScenarios = manifest.TotalScenarios;

        // Step 4: Generate and write the shell HTML (index.html)
        if (_htmlGenerator != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var shellHtml = _htmlGenerator.GenerateShellHtml(documentation, options);
            var shellPath = Path.Combine(outputDirectory, "index.html");
            await WriteFileAsync(shellPath, shellHtml, cancellationToken).ConfigureAwait(false);
            result.ShellHtmlPath = shellPath;
        }

        return result;
    }

    /// <summary>
    /// Writes content to a file asynchronously, creating parent directories as needed.
    /// </summary>
    private static async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }
}
