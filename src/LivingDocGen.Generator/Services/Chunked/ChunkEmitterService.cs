using System;
using System.Collections.Generic;
using System.Linq;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.Generator.Services.Rendering;
using LivingDocGen.Parser.Models;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Services.Chunked;

/// <summary>
/// Builds individual <see cref="FeatureChunk"/> objects from enriched features,
/// containing the pre-rendered HTML and metadata for on-demand loading.
/// </summary>
public interface IChunkEmitterService
{
    /// <summary>
    /// Builds a <see cref="FeatureChunk"/> for a single enriched feature.
    /// </summary>
    /// <param name="feature">The enriched feature to chunk.</param>
    /// <param name="featureIndex">Zero-based index of the feature in the documentation.</param>
    /// <param name="buildId">Build identifier for this generation run.</param>
    /// <param name="options">HTML generation options for rendering.</param>
    /// <returns>A fully populated feature chunk with rendered HTML.</returns>
    FeatureChunk BuildChunk(EnrichedFeature feature, int featureIndex, string buildId, HtmlGenerationOptions options);

    /// <summary>
    /// Serializes a <see cref="FeatureChunk"/> to JSON and returns the JSON string and its hash.
    /// </summary>
    /// <param name="chunk">The chunk to serialize.</param>
    /// <returns>Tuple of (JSON string, SHA-256 hash).</returns>
    (string Json, string Hash) SerializeChunk(FeatureChunk chunk);
}

/// <summary>
/// Default implementation that renders feature HTML via <see cref="IFeatureRenderer"/>
/// and computes content statistics for virtualization hints.
/// </summary>
public class ChunkEmitterService : IChunkEmitterService
{
    private readonly IFeatureRenderer _featureRenderer;

    public ChunkEmitterService(IFeatureRenderer featureRenderer)
    {
        _featureRenderer = featureRenderer ?? throw new ArgumentNullException(nameof(featureRenderer));
    }

    /// <inheritdoc/>
    public FeatureChunk BuildChunk(EnrichedFeature feature, int featureIndex, string buildId, HtmlGenerationOptions options)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));

        var featureId = ContractHashValidator.GenerateFeatureId(
            feature.Feature.FilePath ?? string.Empty,
            feature.Feature.Name);

        // Configure renderer options
        if (options != null)
        {
            _featureRenderer.IncludeComments = options.IncludeComments;
        }

        // Render the feature HTML using the existing renderer
        var html = _featureRenderer.Render(feature, featureIndex, forLazyLoading: true);

        // Compute content statistics
        var contentStats = ComputeContentStats(feature);

        var chunk = new FeatureChunk
        {
            BuildId = buildId,
            FeatureId = featureId,
            Name = feature.Feature.Name,
            Status = MapStatus(feature.OverallStatus),
            Html = html,
            ScenarioSummary = new ChunkScenarioSummary
            {
                Total = feature.Scenarios.Count,
                Passed = feature.PassedCount,
                Failed = feature.FailedCount,
                Skipped = feature.SkippedCount,
                Untested = feature.UntestedCount
            },
            ContentStats = contentStats,
            RenderHints = new ChunkRenderHints
            {
                VirtualizationThreshold = 200,
                TableChunkingThreshold = 200
            }
        };

        return chunk;
    }

    /// <inheritdoc/>
    public (string Json, string Hash) SerializeChunk(FeatureChunk chunk)
    {
        return ContractSerializer.SerializeChunk(chunk);
    }

    /// <summary>
    /// Computes content statistics for a feature (max table rows/columns, outline examples).
    /// Used by the runtime to decide virtualization thresholds.
    /// </summary>
    private static ChunkContentStats ComputeContentStats(EnrichedFeature feature)
    {
        int maxTableRows = 0;
        int maxTableColumns = 0;
        int outlineExamples = 0;

        foreach (var scenario in feature.Scenarios)
        {
            // Check steps for data tables
            foreach (var step in scenario.Steps)
            {
                if (step.Step.DataTable != null && step.Step.DataTable.Rows != null && step.Step.DataTable.Rows.Count > 0)
                {
                    var rows = step.Step.DataTable.Rows.Count;
                    var cols = step.Step.DataTable.Rows.Max(r => r.Count);
                    if (rows > maxTableRows) maxTableRows = rows;
                    if (cols > maxTableColumns) maxTableColumns = cols;
                }
            }

            // Check for scenario outline examples
            if (scenario.Scenario.Examples != null)
            {
                foreach (var exampleTable in scenario.Scenario.Examples)
                {
                    if (exampleTable.Rows != null)
                    {
                        outlineExamples += exampleTable.Rows.Count;
                        var rows = exampleTable.Rows.Count;
                        var cols = exampleTable.Headers != null ? exampleTable.Headers.Count : 0;
                        if (rows > maxTableRows) maxTableRows = rows;
                        if (cols > maxTableColumns) maxTableColumns = cols;
                    }
                }
            }
        }

        return new ChunkContentStats
        {
            MaxTableRows = maxTableRows,
            MaxTableColumns = maxTableColumns,
            OutlineExamples = outlineExamples
        };
    }

    /// <summary>
    /// Maps the enriched execution status to a string status.
    /// </summary>
    private static string MapStatus(ExecutionStatus status)
    {
        return status switch
        {
            ExecutionStatus.Passed => "passed",
            ExecutionStatus.Failed => "failed",
            ExecutionStatus.Skipped => "skipped",
            _ => "untested"
        };
    }
}
