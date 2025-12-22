using System.Collections.Generic;

namespace LivingDocGen.Parser.Core;

using LivingDocGen.Parser.Models;

/// <summary>
/// Interface for all framework-specific adapters
/// </summary>
public interface IFeatureParser
{
    /// <summary>
    /// Parse a single feature file
    /// </summary>
    UniversalFeature Parse(string featureFilePath);
    
    /// <summary>
    /// Parse multiple feature files from a directory
    /// </summary>
    List<UniversalFeature> ParseDirectory(string directoryPath, string searchPattern = "*.feature");
    
    /// <summary>
    /// The framework this parser supports
    /// </summary>
    BDDFramework SupportedFramework { get; }
    
    /// <summary>
    /// Validate if the parser can handle this file
    /// </summary>
    bool CanParse(string featureFilePath);
}
