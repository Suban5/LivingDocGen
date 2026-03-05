using System.Collections.Generic;

namespace LivingDocGen.Generator.Models.Contracts;

/// <summary>
/// Normalized search/filter index for the entire report.
/// Contains precomputed inverted indexes for token, status, and tag queries.
/// Loaded by the search worker at initialization.
/// </summary>
public class FeatureIndex : ContractBase
{
    /// <summary>
    /// Flat list of all scenario records with their metadata.
    /// Ordinals in this list correspond to ScenarioRange in the manifest.
    /// </summary>
    public List<ScenarioRecord> ScenarioRecords { get; set; } = new List<ScenarioRecord>();

    /// <summary>
    /// Inverted index mapping normalized tokens to scenario ordinal lists.
    /// Token keys are lowercase, Unicode-folded, and delimiter-normalized.
    /// Values are sorted integer ordinals into ScenarioRecords.
    /// </summary>
    public Dictionary<string, List<int>> InvertedTokenIndex { get; set; } = new Dictionary<string, List<int>>();

    /// <summary>
    /// Precomputed status -> scenario ordinals mapping.
    /// </summary>
    public StatusIndex StatusIndex { get; set; } = new StatusIndex();

    /// <summary>
    /// Inverted index mapping tag names to scenario ordinal lists.
    /// </summary>
    public Dictionary<string, List<int>> TagIndex { get; set; } = new Dictionary<string, List<int>>();

    /// <summary>
    /// Token normalization metadata for diagnostics and compatibility.
    /// </summary>
    public TokenNormalization Normalization { get; set; } = new TokenNormalization();
}

/// <summary>
/// Individual scenario record for indexing.
/// Uses integer ordinals instead of string IDs for compact representation.
/// </summary>
public class ScenarioRecord
{
    /// <summary>
    /// Zero-based ordinal of this scenario (position in ScenarioRecords list).
    /// </summary>
    public int ScenarioOrdinal { get; set; }

    /// <summary>
    /// Ordinal of the owning feature (position in manifest FeatureMap).
    /// </summary>
    public int FeatureOrdinal { get; set; }

    /// <summary>
    /// Display name of the scenario (for result rendering).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Execution status: passed, failed, skipped, or untested.
    /// </summary>
    public string Status { get; set; } = "untested";

    /// <summary>
    /// Normalized search tokens derived from feature name, scenario name, and description.
    /// </summary>
    public List<string> Tokens { get; set; } = new List<string>();

    /// <summary>
    /// Tags applied to this scenario (including inherited feature/rule tags).
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
}

/// <summary>
/// Precomputed status -> scenario ordinals index.
/// </summary>
public class StatusIndex
{
    /// <summary>Ordinals of passed scenarios.</summary>
    public List<int> Passed { get; set; } = new List<int>();

    /// <summary>Ordinals of failed scenarios.</summary>
    public List<int> Failed { get; set; } = new List<int>();

    /// <summary>Ordinals of skipped scenarios.</summary>
    public List<int> Skipped { get; set; } = new List<int>();

    /// <summary>Ordinals of untested scenarios.</summary>
    public List<int> Untested { get; set; } = new List<int>();
}

/// <summary>
/// Token normalization metadata describing how tokens were produced.
/// </summary>
public class TokenNormalization
{
    /// <summary>Whether tokens are lowercased.</summary>
    public bool CaseFolding { get; set; } = true;

    /// <summary>Whether Unicode normalization was applied.</summary>
    public bool UnicodeFolding { get; set; } = true;

    /// <summary>Delimiter characters used for tokenization.</summary>
    public string Delimiters { get; set; } = " _-/.@#";

    /// <summary>Locale used for case folding (invariant by default).</summary>
    public string Locale { get; set; } = "invariant";
}
