using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LivingDocGen.Parser.Parsers;

using LivingDocGen.Parser.Core;
using LivingDocGen.Parser.Models;
using LivingDocGen.Core.Exceptions;
using Gherkin;
using Gherkin.Ast;

/// <summary>
/// Universal Gherkin parser using the official Cucumber Gherkin library.
/// Works for Cucumber, SpecFlow, and ReqnRoll (all use standard Gherkin syntax)
/// </summary>
public class GherkinParser : IFeatureParser
{
    private readonly BDDFramework _framework;
    
    public GherkinParser(BDDFramework framework = BDDFramework.Cucumber)
    {
        _framework = framework;
    }
    
    public BDDFramework SupportedFramework => _framework;

    public bool CanParse(string featureFilePath)
    {
        return File.Exists(featureFilePath) && 
               Path.GetExtension(featureFilePath).Equals(".feature", StringComparison.OrdinalIgnoreCase);
    }

    public UniversalFeature Parse(string featureFilePath)
    {
        if (!CanParse(featureFilePath))
            throw new ParseException($"Invalid feature file: {featureFilePath}", featureFilePath);

        var parser = new Parser();
        GherkinDocument gherkinDocument;
        
        try
        {
            using (var reader = new StreamReader(featureFilePath))
            {
                gherkinDocument = parser.Parse(reader);
            }
        }
        catch (Exception ex)
        {
            throw new ParseException($"Failed to parse feature file: {ex.Message}", ex);
        }

        var feature = gherkinDocument.Feature;
        if (feature == null)
            throw new InvalidOperationException($"No feature found in {featureFilePath}");

        return MapToUniversalFeature(feature, featureFilePath, gherkinDocument.Comments);
    }

    public List<UniversalFeature> ParseDirectory(string directoryPath, string searchPattern = "*.feature")
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var featureFiles = Directory.GetFiles(directoryPath, searchPattern, SearchOption.AllDirectories);
        var features = new List<UniversalFeature>();

        foreach (var file in featureFiles)
        {
            try
            {
                features.Add(Parse(file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing {file}: {ex.Message}");
                // Continue with other files
            }
        }

        return features;
    }

    private UniversalFeature MapToUniversalFeature(Feature feature, string filePath, IEnumerable<Comment> comments)
    {
        // Group comments by their association with elements based on line numbers
        var commentsList = comments?.ToList() ?? new List<Comment>();
        var featureLineNumber = (int)feature.Location.Line;
        
        // Comments before the feature line belong to the feature
        var featureComments = commentsList
            .Where(c => (int)c.Location.Line < featureLineNumber)
            .Select(c => c.Text)
            .ToList();
        
        var universalFeature = new UniversalFeature
        {
            Name = feature.Name ?? string.Empty,
            Description = feature.Description ?? string.Empty,
            Language = feature.Language ?? "en",
            FilePath = filePath,
            Tags = feature.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            Comments = featureComments,
            Metadata = new FrameworkMetadata
            {
                Framework = _framework,
                ParsedAt = DateTime.UtcNow
            }
        };

        // Build a mapping of line numbers to comments for scenarios
        var commentsByLine = commentsList.ToDictionary(c => (int)c.Location.Line, c => c.Text);
        
        foreach (var child in feature.Children)
        {
            if (child is Background background)
            {
                universalFeature.Background = MapBackground(background);
            }
            else if (child is Scenario scenario)
            {
                universalFeature.Scenarios.Add(MapScenario(scenario, commentsList));
            }
            else if (child is Rule rule)
            {
                universalFeature.Rules.Add(MapRule(rule, commentsList));
            }
        }

        return universalFeature;
    }

    private UniversalRule MapRule(Rule rule, List<Comment> comments)
    {
        var universalRule = new UniversalRule
        {
            Name = rule.Name ?? string.Empty,
            Description = rule.Description ?? string.Empty,
            Tags = rule.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            LineNumber = (int)rule.Location.Line
        };

        foreach (var child in rule.Children)
        {
            if (child is Background background)
            {
                universalRule.Background = MapBackground(background);
            }
            else if (child is Scenario scenario)
            {
                universalRule.Scenarios.Add(MapScenario(scenario, comments));
            }
        }

        return universalRule;
    }

    private UniversalBackground MapBackground(Background background)
    {
        return new UniversalBackground
        {
            Name = background.Name ?? string.Empty,
            Description = background.Description ?? string.Empty,
            Steps = background.Steps?.Select(MapStep).ToList() ?? new List<UniversalStep>()
        };
    }

    private UniversalScenario MapScenario(Scenario scenario, List<Comment> allComments)
    {
        var scenarioLine = (int)scenario.Location.Line;
        
        // Find comments that appear right before this scenario (within 5 lines)
        // This captures comments that are meant to document the scenario
        var scenarioComments = allComments
            .Where(c => (int)c.Location.Line < scenarioLine && 
                       (int)c.Location.Line >= scenarioLine - 10) // Look back up to 10 lines
            .OrderBy(c => c.Location.Line)
            .Select(c => c.Text)
            .ToList();
        
        var universalScenario = new UniversalScenario
        {
            Name = scenario.Name ?? string.Empty,
            Description = scenario.Description ?? string.Empty,
            Tags = scenario.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            Comments = scenarioComments,
            Steps = scenario.Steps?.Select(MapStep).ToList() ?? new List<UniversalStep>(),
            LineNumber = (int)scenario.Location.Line,
            Type = scenario.Examples?.Any() == true ? ScenarioType.ScenarioOutline : ScenarioType.Scenario
        };

        if (scenario.Examples?.Any() == true)
        {
            universalScenario.Examples = scenario.Examples.Select(MapExamples).ToList();
        }

        return universalScenario;
    }

    private UniversalStep MapStep(Step step)
    {
        var universalStep = new UniversalStep
        {
            Keyword = step.Keyword.Trim(),
            Text = step.Text ?? string.Empty,
            LineNumber = (int)step.Location.Line
        };

        // Handle step argument (DocString or DataTable)
        if (step.Argument != null)
        {
            if (step.Argument is DocString docString)
            {
                universalStep.DocString = docString.Content;
            }
            else if (step.Argument is DataTable dataTable)
            {
                universalStep.DataTable = new UniversalDataTable
                {
                    Rows = dataTable.Rows
                        .Select(row => row.Cells.Select(cell => cell.Value).ToList())
                        .ToList()
                };
            }
        }

        return universalStep;
    }

    private UniversalExample MapExamples(Examples examples)
    {
        return new UniversalExample
        {
            Name = examples.Name ?? string.Empty,
            Tags = examples.Tags?.Select(t => t.Name).ToList() ?? new List<string>(),
            Headers = examples.TableHeader?.Cells.Select(c => c.Value).ToList() ?? new List<string>(),
            Rows = examples.TableBody?
                .Select(row => row.Cells.Select(cell => cell.Value).ToList())
                .ToList() ?? new List<List<string>>()
        };
    }
}
