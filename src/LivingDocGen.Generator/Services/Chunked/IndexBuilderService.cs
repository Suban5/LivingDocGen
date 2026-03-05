using System;
using System.Collections.Generic;
using System.Linq;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Models.Contracts;
using LivingDocGen.TestReporter.Models;

namespace LivingDocGen.Generator.Services.Chunked;

/// <summary>
/// Builds the <see cref="FeatureIndex"/> from enriched documentation,
/// constructing inverted token, status, and tag indexes for efficient search/filter.
/// </summary>
public interface IIndexBuilderService
{
    /// <summary>
    /// Builds a <see cref="FeatureIndex"/> from enriched features.
    /// </summary>
    /// <param name="documentation">The enriched living documentation.</param>
    /// <param name="buildId">Build identifier for this generation run.</param>
    /// <returns>A fully populated feature index with inverted indexes.</returns>
    FeatureIndex BuildIndex(LivingDocumentation documentation, string buildId);
}

/// <summary>
/// Default implementation that constructs the inverted indexes
/// using the <see cref="ITokenizerService"/> for token normalization.
/// </summary>
public class IndexBuilderService : IIndexBuilderService
{
    private readonly ITokenizerService _tokenizer;

    public IndexBuilderService(ITokenizerService tokenizer)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
    }

    /// <inheritdoc/>
    public FeatureIndex BuildIndex(LivingDocumentation documentation, string buildId)
    {
        if (documentation == null)
            throw new ArgumentNullException(nameof(documentation));

        var index = new FeatureIndex
        {
            BuildId = buildId,
            Normalization = new TokenNormalization
            {
                CaseFolding = true,
                UnicodeFolding = true,
                Delimiters = " _-/.@#",
                Locale = "invariant"
            }
        };

        var scenarioRecords = new List<ScenarioRecord>();
        var invertedTokenIndex = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var statusIndex = new StatusIndex();
        var tagIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        int scenarioOrdinal = 0;

        for (int featureOrdinal = 0; featureOrdinal < documentation.Features.Count; featureOrdinal++)
        {
            var feature = documentation.Features[featureOrdinal];
            var featureTokens = _tokenizer.Tokenize(feature.Feature.Name);
            // Also tokenize the feature description if available
            if (!string.IsNullOrWhiteSpace(feature.Feature.Description))
            {
                featureTokens = _tokenizer.TokenizeMultiple(new[]
                {
                    feature.Feature.Name,
                    feature.Feature.Description
                });
            }

            foreach (var scenario in feature.Scenarios)
            {
                // Build token list: feature name + feature description + scenario name + scenario description
                var textsToTokenize = new List<string> { feature.Feature.Name, scenario.Scenario.Name };
                if (!string.IsNullOrWhiteSpace(feature.Feature.Description))
                {
                    textsToTokenize.Add(feature.Feature.Description);
                }
                if (!string.IsNullOrWhiteSpace(scenario.Scenario.Description))
                {
                    textsToTokenize.Add(scenario.Scenario.Description);
                }
                var tokens = _tokenizer.TokenizeMultiple(textsToTokenize);

                // Collect tags: feature tags + scenario tags
                var tags = new List<string>();
                if (feature.Feature.Tags != null)
                    tags.AddRange(feature.Feature.Tags);
                if (scenario.Scenario.Tags != null)
                    tags.AddRange(scenario.Scenario.Tags);
                tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                tags.Sort(StringComparer.OrdinalIgnoreCase);

                // Determine status
                var status = MapStatus(scenario.Status);

                var record = new ScenarioRecord
                {
                    ScenarioOrdinal = scenarioOrdinal,
                    FeatureOrdinal = featureOrdinal,
                    Name = scenario.Scenario.Name,
                    Status = status,
                    Tokens = tokens,
                    Tags = tags
                };
                scenarioRecords.Add(record);

                // Populate inverted token index
                foreach (var token in tokens)
                {
                    if (!invertedTokenIndex.TryGetValue(token, out var postings))
                    {
                        postings = new List<int>();
                        invertedTokenIndex[token] = postings;
                    }
                    postings.Add(scenarioOrdinal);
                }

                // Populate status index
                AddToStatusIndex(statusIndex, status, scenarioOrdinal);

                // Populate tag index
                foreach (var tag in tags)
                {
                    var normalizedTag = tag.TrimStart('@').ToLowerInvariant();
                    if (!tagIndex.TryGetValue(normalizedTag, out var tagPostings))
                    {
                        tagPostings = new List<int>();
                        tagIndex[normalizedTag] = tagPostings;
                    }
                    tagPostings.Add(scenarioOrdinal);
                }

                scenarioOrdinal++;
            }
        }

        index.ScenarioRecords = scenarioRecords;
        index.InvertedTokenIndex = invertedTokenIndex;
        index.StatusIndex = statusIndex;
        index.TagIndex = tagIndex;

        return index;
    }

    /// <summary>
    /// Maps the enriched execution status to a string for the index contract.
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

    /// <summary>
    /// Adds a scenario ordinal to the appropriate status bucket.
    /// </summary>
    private static void AddToStatusIndex(StatusIndex statusIndex, string status, int ordinal)
    {
        switch (status)
        {
            case "passed":
                statusIndex.Passed.Add(ordinal);
                break;
            case "failed":
                statusIndex.Failed.Add(ordinal);
                break;
            case "skipped":
                statusIndex.Skipped.Add(ordinal);
                break;
            default:
                statusIndex.Untested.Add(ordinal);
                break;
        }
    }
}
