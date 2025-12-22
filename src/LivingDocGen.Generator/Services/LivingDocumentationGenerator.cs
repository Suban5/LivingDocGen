using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Services;
using LivingDocGen.TestReporter.Services;

/// <summary>
/// Main orchestration service for generating living documentation
/// </summary>
public class LivingDocumentationGenerator : ILivingDocumentationGenerator
{
    private readonly UniversalParserService _parser;
    private readonly TestReportService _testReporter;
    private readonly DocumentEnrichmentService _enrichmentService;
    private readonly HtmlGeneratorService _htmlGenerator;

    public LivingDocumentationGenerator()
    {
        _parser = new UniversalParserService();
        _testReporter = new TestReportService();
        _enrichmentService = new DocumentEnrichmentService();
        _htmlGenerator = new HtmlGeneratorService();
    }

    /// <summary>
    /// Generate living documentation from feature files and test results
    /// </summary>
    /// <param name="featureFiles">Paths to feature files</param>
    /// <param name="testResultFiles">Paths to test result files (NUnit, xUnit, JUnit, Cucumber JSON)</param>
    /// <param name="outputPath">Path for generated HTML file</param>
    /// <param name="options">HTML generation options</param>
    public Task<string> GenerateAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string title = null,
        HtmlGenerationOptions options = null)
    {
        // Step 1: Parse all feature files
        var parsedFeatures = new List<LivingDocGen.Parser.Models.UniversalFeature>();
        foreach (var featurePath in featureFiles)
        {
            if (File.Exists(featurePath))
            {
                var feature = _parser.ParseFeature(featurePath);
                parsedFeatures.Add(feature);
            }
        }

        // Step 2: Parse all test result files
        var testExecutionReports = new List<LivingDocGen.TestReporter.Models.TestExecutionReport>();
        foreach (var testResultPath in testResultFiles)
        {
            if (File.Exists(testResultPath))
            {
                try
                {
                    var report = _testReporter.ParseTestResults(testResultPath);
                    if (report != null)
                    {
                        testExecutionReports.Add(report);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse test results from {testResultPath}: {ex.Message}");
                }
            }
        }

        // Step 3: Merge test results
        var mergedTestResults = testExecutionReports.Count > 0
            ? _testReporter.ParseMultipleTestResults(testResultFiles.ToArray())
            : new LivingDocGen.TestReporter.Models.TestExecutionReport();

        // Step 4: Enrich documentation with test results
        var enrichedDoc = _enrichmentService.EnrichDocumentation(parsedFeatures, mergedTestResults);
        enrichedDoc.Title = title ?? "BDD Living Documentation";
        enrichedDoc.GeneratedAt = DateTime.Now;

        // Step 5: Generate HTML
        var html = _htmlGenerator.GenerateHtml(enrichedDoc, options);

        return Task.FromResult(html);
    }

    /// <summary>
    /// Generate living documentation and save to file
    /// </summary>
    public async Task GenerateToFileAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string outputPath,
        string title = null,
        HtmlGenerationOptions options = null)
    {
        var html = await GenerateAsync(featureFiles, testResultFiles, title, options);
        
        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        await File.WriteAllTextAsync(outputPath, html);
        Console.WriteLine($"✓ Generated living documentation: {outputPath}");
        Console.WriteLine($"  Size: {new FileInfo(outputPath).Length / 1024.0:F2} KB");
    }

    /// <summary>
    /// Generate from directories (auto-discover files)
    /// </summary>
    public async Task GenerateFromDirectoriesAsync(
        string featureDirectory,
        string testResultsDirectory,
        string outputPath,
        string title = null,
        HtmlGenerationOptions options = null)
    {
        // Find all feature files
        var featureFiles = Directory.Exists(featureDirectory)
            ? Directory.GetFiles(featureDirectory, "*.feature", SearchOption.AllDirectories)
            : Array.Empty<string>();

        // Find all test result files
        var testResultFiles = Directory.Exists(testResultsDirectory)
            ? Directory.GetFiles(testResultsDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".xml") || f.EndsWith(".json"))
                .ToList()
            : new List<string>();

        await GenerateToFileAsync(featureFiles, testResultFiles, outputPath, title, options);
    }
}
