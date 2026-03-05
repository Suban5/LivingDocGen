using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LivingDocGen.Generator.Services.Chunked;

/// <summary>
/// Normalizes feature/scenario names into search tokens for the inverted index.
/// Applies case folding, Unicode normalization, and delimiter-based splitting.
/// </summary>
public interface ITokenizerService
{
    /// <summary>
    /// Tokenizes a text string into normalized search tokens.
    /// </summary>
    /// <param name="text">The text to tokenize (feature name, scenario name, description).</param>
    /// <returns>List of normalized tokens (lowercase, deduplicated, sorted).</returns>
    List<string> Tokenize(string text);

    /// <summary>
    /// Tokenizes multiple text strings and merges tokens into a single deduplicated list.
    /// </summary>
    /// <param name="texts">The texts to tokenize.</param>
    /// <returns>Merged, deduplicated, sorted list of normalized tokens.</returns>
    List<string> TokenizeMultiple(IEnumerable<string> texts);
}

/// <summary>
/// Default implementation of <see cref="ITokenizerService"/> using delimiter-based
/// splitting, Unicode normalization, and case folding.
/// </summary>
public class TokenizerService : ITokenizerService
{
    /// <summary>
    /// Delimiters used to split text into tokens.
    /// Matches the <see cref="Models.Contracts.TokenNormalization.Delimiters"/> contract.
    /// </summary>
    private static readonly char[] Delimiters = { ' ', '_', '-', '/', '.', '@', '#', '\t', '\r', '\n', '(', ')', '[', ']', '{', '}', ':', ',', ';', '"', '\'' };

    /// <summary>
    /// Minimum token length to include (avoids noise from single-char tokens).
    /// </summary>
    private const int MinTokenLength = 2;

    /// <inheritdoc/>
    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Step 1: Unicode normalize (NFC)
        var normalized = text.Normalize(NormalizationForm.FormC);

        // Step 2: Lowercase (invariant culture)
        var lowered = normalized.ToLowerInvariant();

        // Step 3: Split on delimiters
        var rawTokens = lowered.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

        // Step 4: Filter by minimum length and deduplicate
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in rawTokens)
        {
            var trimmed = token.Trim();
            if (trimmed.Length >= MinTokenLength)
            {
                tokens.Add(trimmed);
            }
        }

        // Step 5: Return sorted for deterministic output
        var result = tokens.ToList();
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    /// <inheritdoc/>
    public List<string> TokenizeMultiple(IEnumerable<string> texts)
    {
        if (texts == null)
            return new List<string>();

        var merged = new HashSet<string>(StringComparer.Ordinal);
        foreach (var text in texts)
        {
            var tokens = Tokenize(text);
            foreach (var token in tokens)
            {
                merged.Add(token);
            }
        }

        var result = merged.ToList();
        result.Sort(StringComparer.Ordinal);
        return result;
    }
}
