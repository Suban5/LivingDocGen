namespace LivingDocGen.Parser.Services;

using LivingDocGen.Parser.Models;

/// <summary>
/// Interface for the universal parser service
/// </summary>
public interface IUniversalParserService
{
    /// <summary>
    /// Auto-detect framework and parse a single feature file
    /// </summary>
    UniversalFeature ParseFeature(string featureFilePath, BDDFramework? framework = null);

    /// <summary>
    /// Parse entire directory of feature files
    /// </summary>
    List<UniversalFeature> ParseDirectory(string directoryPath, BDDFramework? framework = null);

    /// <summary>
    /// Get statistics about parsed features
    /// </summary>
    ParsingStatistics GetStatistics(List<UniversalFeature> features);
}
