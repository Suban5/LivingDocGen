using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Parser.Services;
using LivingDocGen.TestReporter.Services;

/// <summary>
/// Main orchestration service for generating living documentation from BDD feature files and test results
/// </summary>
public class LivingDocumentationGenerator : ILivingDocumentationGenerator
{
    private readonly IUniversalParserService _parser;
    private readonly ITestReportService _testReporter;
    private readonly IDocumentEnrichmentService _enrichmentService;
    private readonly IHtmlGeneratorService _htmlGenerator;
    private readonly ILogger<LivingDocumentationGenerator> _logger;

    /// <summary>
    /// Initializes a new instance with dependency injection
    /// </summary>
    public LivingDocumentationGenerator(
        IUniversalParserService parser,
        ITestReportService testReporter,
        IDocumentEnrichmentService enrichmentService,
        IHtmlGeneratorService htmlGenerator,
        ILogger<LivingDocumentationGenerator> logger = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _testReporter = testReporter ?? throw new ArgumentNullException(nameof(testReporter));
        _enrichmentService = enrichmentService ?? throw new ArgumentNullException(nameof(enrichmentService));
        _htmlGenerator = htmlGenerator ?? throw new ArgumentNullException(nameof(htmlGenerator));
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance with default service implementations (for backward compatibility)
    /// </summary>
    public LivingDocumentationGenerator() 
        : this(
            new UniversalParserService(),
            new TestReportService(),
            new DocumentEnrichmentService(),
            new HtmlGeneratorService(),
            null)
    {
    }

    /// <summary>
    /// Generate living documentation from feature files and test results
    /// </summary>
    /// <param name="featureFiles">Collection of absolute paths to Gherkin feature files (.feature)</param>
    /// <param name="testResultFiles">Collection of absolute paths to test result files (NUnit XML, xUnit XML, TRX, Cucumber JSON)</param>
    /// <param name="title">Title for the generated documentation. Defaults to "BDD Living Documentation"</param>
    /// <param name="options">HTML generation options including theme selection and formatting preferences</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>HTML string containing the complete living documentation</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentException">Thrown when no valid feature files are provided</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled via cancellationToken</exception>
    public async Task<string> GenerateAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string title = null,
        HtmlGenerationOptions options = null,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (featureFiles == null)
            throw new ArgumentNullException(nameof(featureFiles));
        
        if (testResultFiles == null)
            throw new ArgumentNullException(nameof(testResultFiles));
        
        var featureFilesList = featureFiles.ToList();
        var testResultFilesList = testResultFiles.ToList();
        
        if (!featureFilesList.Any())
            throw new ArgumentException("At least one feature file path must be provided", nameof(featureFiles));
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Filter to existing files
        var existingFeatureFiles = featureFilesList.Where(File.Exists).ToList();
        var existingTestFiles = testResultFilesList.Where(File.Exists).ToList();
        
        if (!existingFeatureFiles.Any())
        {
            var missingCount = featureFilesList.Count;
            _logger?.LogError("None of the {Count} provided feature files exist", missingCount);
            throw new ArgumentException($"None of the {missingCount} provided feature files exist", nameof(featureFiles));
        }
        
        var missingFeatureCount = featureFilesList.Count - existingFeatureFiles.Count;
        if (missingFeatureCount > 0)
        {
            _logger?.LogWarning("{MissingCount} feature file(s) not found and will be skipped", missingFeatureCount);
        }
        
        _logger?.LogInformation("Processing {FeatureCount} feature files and {TestCount} test result files",
            existingFeatureFiles.Count, existingTestFiles.Count);
        
        // Step 1: Parse all feature files in parallel
        var parseFeatureTasks = existingFeatureFiles.Select(async featurePath =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _parser.ParseFeature(featurePath), cancellationToken)
                .ConfigureAwait(false);
        });
        
        var parsedFeatures = (await Task.WhenAll(parseFeatureTasks).ConfigureAwait(false)).ToList();
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Step 2 & 3: Parse and merge test results (fixed bug - avoid redundant parsing)
        var mergedTestResults = existingTestFiles.Any()
            ? await Task.Run(() => 
                _testReporter.ParseMultipleTestResults(existingTestFiles.ToArray()), 
                cancellationToken).ConfigureAwait(false)
            : new LivingDocGen.TestReporter.Models.TestExecutionReport();
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Step 4: Enrich documentation with test results
        var enrichedDoc = await Task.Run(() => 
            _enrichmentService.EnrichDocumentation(parsedFeatures, mergedTestResults, cancellationToken),
            cancellationToken).ConfigureAwait(false);
        
        enrichedDoc.Title = title ?? "BDD Living Documentation";
        enrichedDoc.GeneratedAt = DateTime.Now;
        
        cancellationToken.ThrowIfCancellationRequested();
        
        // Step 5: Generate HTML
        var html = await Task.Run(() => 
            _htmlGenerator.GenerateHtml(enrichedDoc, options),
            cancellationToken).ConfigureAwait(false);
        
        _logger?.LogInformation("Successfully generated living documentation with {FeatureCount} features", 
            enrichedDoc.Features.Count);
        
        return html;
    }

    /// <summary>
    /// Generate living documentation and save to file
    /// </summary>
    /// <param name="featureFiles">Collection of absolute paths to feature files</param>
    /// <param name="testResultFiles">Collection of absolute paths to test result files</param>
    /// <param name="outputPath">Absolute path where HTML file will be saved</param>
    /// <param name="title">Title for the documentation</param>
    /// <param name="options">HTML generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task GenerateToFileAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string outputPath,
        string title = null,
        HtmlGenerationOptions options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        
        var html = await GenerateAsync(featureFiles, testResultFiles, title, options, cancellationToken)
            .ConfigureAwait(false);
        
        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
            _logger?.LogDebug("Created output directory: {Directory}", outputDir);
        }

        await File.WriteAllTextAsync(outputPath, html, cancellationToken).ConfigureAwait(false);
        
        var fileSize = new FileInfo(outputPath).Length / 1024.0;
        _logger?.LogInformation("Generated living documentation: {OutputPath} ({FileSize:F2} KB)", 
            outputPath, fileSize);
    }

    /// <summary>
    /// Generate from directories by auto-discovering feature and test result files
    /// </summary>
    /// <param name="featureDirectory">Directory to search for .feature files (recursive)</param>
    /// <param name="testResultsDirectory">Directory to search for test result files (recursive)</param>
    /// <param name="outputPath">Path where HTML file will be saved</param>
    /// <param name="title">Title for the documentation</param>
    /// <param name="options">HTML generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task GenerateFromDirectoriesAsync(
        string featureDirectory,
        string testResultsDirectory,
        string outputPath,
        string title = null,
        HtmlGenerationOptions options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureDirectory))
            throw new ArgumentException("Feature directory cannot be null or empty", nameof(featureDirectory));
        
        if (string.IsNullOrWhiteSpace(testResultsDirectory))
            throw new ArgumentException("Test results directory cannot be null or empty", nameof(testResultsDirectory));
        
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));
        
        // Find all feature files (using EnumerateFiles for better performance)
        var featureFiles = Directory.Exists(featureDirectory)
            ? Directory.EnumerateFiles(featureDirectory, "*.feature", SearchOption.AllDirectories).ToList()
            : new List<string>();

        // Find all test result files
        var testResultFiles = Directory.Exists(testResultsDirectory)
            ? Directory.EnumerateFiles(testResultsDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || 
                           f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".trx", StringComparison.OrdinalIgnoreCase))
                .ToList()
            : new List<string>();
        
        _logger?.LogInformation("Discovered {FeatureCount} feature files in {FeatureDir} and {TestCount} test result files in {TestDir}",
            featureFiles.Count, featureDirectory, testResultFiles.Count, testResultsDirectory);

        await GenerateToFileAsync(featureFiles, testResultFiles, outputPath, title, options, cancellationToken)
            .ConfigureAwait(false);
    }
}
