using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LivingDocGen.Parser.Services;

using LivingDocGen.Parser.Core;
using LivingDocGen.Parser.Models;
using LivingDocGen.Parser.Parsers;
using LivingDocGen.Core.Exceptions;
using LivingDocGen.Core.Validators;

/// <summary>
/// Orchestrates parsing across multiple frameworks
/// </summary>
public class UniversalParserService : IUniversalParserService
{
    private readonly Dictionary<BDDFramework, IFeatureParser> _parsers;

    public UniversalParserService()
    {
        _parsers = new Dictionary<BDDFramework, IFeatureParser>
        {
            { BDDFramework.Cucumber, new GherkinParser(BDDFramework.Cucumber) },
            { BDDFramework.SpecFlow, new GherkinParser(BDDFramework.SpecFlow) },
            { BDDFramework.ReqnRoll, new GherkinParser(BDDFramework.ReqnRoll) }
            // JBehave parser can be added later if syntax differs
        };
    }

    /// <summary>
    /// Auto-detect framework and parse
    /// </summary>
    public UniversalFeature ParseFeature(string featureFilePath, BDDFramework? framework = null)
    {
        var detectedFramework = framework ?? DetectFramework(featureFilePath);
        
        if (!_parsers.ContainsKey(detectedFramework))
            throw new NotSupportedException($"Framework {detectedFramework} is not yet supported");

        return _parsers[detectedFramework].Parse(featureFilePath);
    }

    /// <summary>
    /// Parse entire directory
    /// </summary>
    public List<UniversalFeature> ParseDirectory(string directoryPath, BDDFramework? framework = null)
    {
        var detectedFramework = framework ?? DetectFrameworkFromDirectory(directoryPath);
        
        if (!_parsers.ContainsKey(detectedFramework))
            throw new NotSupportedException($"Framework {detectedFramework} is not yet supported");

        return _parsers[detectedFramework].ParseDirectory(directoryPath);
    }

    /// <summary>
    /// Heuristic framework detection based on project structure
    /// </summary>
    private BDDFramework DetectFramework(string featureFilePath)
    {
        var directory = Path.GetDirectoryName(featureFilePath) ?? string.Empty;
        return DetectFrameworkFromDirectory(directory);
    }

    private BDDFramework DetectFrameworkFromDirectory(string directoryPath)
    {
        // Look for clues in the project structure
        var parentDirs = new DirectoryInfo(directoryPath);
        
        while (parentDirs != null)
        {
            // Check for .csproj files
            var csprojFiles = parentDirs.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
            foreach (var csproj in csprojFiles)
            {
                var content = File.ReadAllText(csproj.FullName);
                if (content.Contains("ReqnRoll")) return BDDFramework.ReqnRoll;
                if (content.Contains("SpecFlow")) return BDDFramework.SpecFlow;
            }

            // Check for pom.xml (Maven - Java)
            if (File.Exists(Path.Combine(parentDirs.FullName, "pom.xml")))
            {
                var pomContent = File.ReadAllText(Path.Combine(parentDirs.FullName, "pom.xml"));
                if (pomContent.Contains("cucumber")) return BDDFramework.Cucumber;
                if (pomContent.Contains("jbehave")) return BDDFramework.JBehave;
            }

            // Check for package.json (Node.js)
            if (File.Exists(Path.Combine(parentDirs.FullName, "package.json")))
            {
                var packageContent = File.ReadAllText(Path.Combine(parentDirs.FullName, "package.json"));
                if (packageContent.Contains("@cucumber/cucumber")) return BDDFramework.Cucumber;
            }

            parentDirs = parentDirs.Parent;
        }

        // Default to Cucumber (most common)
        return BDDFramework.Cucumber;
    }

    /// <summary>
    /// Get statistics about parsed features
    /// </summary>
    public ParsingStatistics GetStatistics(List<UniversalFeature> features)
    {
        return new ParsingStatistics
        {
            TotalFeatures = features.Count,
            TotalScenarios = features.Sum(f => f.Scenarios.Count),
            TotalSteps = features.Sum(f => f.Scenarios.Sum(s => s.Steps.Count)),
            FrameworkDistribution = features
                .GroupBy(f => f.Metadata.Framework)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

public class ParsingStatistics
{
    public int TotalFeatures { get; set; }
    public int TotalScenarios { get; set; }
    public int TotalSteps { get; set; }
    public Dictionary<BDDFramework, int> FrameworkDistribution { get; set; } = new();
}
