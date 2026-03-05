using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LivingDocGen.Generator.Services;

using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Generator.Services.Telemetry;
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
    private readonly IChunkedOutputPipeline _chunkedPipeline;
    private readonly ILogger<LivingDocumentationGenerator> _logger;

    /// <summary>
    /// When non-null, phase timings and memory snapshots are recorded during generation.
    /// Set before calling GenerateAsync to enable telemetry collection.
    /// </summary>
    public GenerationTelemetry Telemetry { get; set; }

    /// <summary>
    /// Initializes a new instance with dependency injection
    /// </summary>
    public LivingDocumentationGenerator(
        IUniversalParserService parser,
        ITestReportService testReporter,
        IDocumentEnrichmentService enrichmentService,
        IHtmlGeneratorService htmlGenerator,
        IChunkedOutputPipeline chunkedPipeline = null,
        ILogger<LivingDocumentationGenerator> logger = null)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _testReporter = testReporter ?? throw new ArgumentNullException(nameof(testReporter));
        _enrichmentService = enrichmentService ?? throw new ArgumentNullException(nameof(enrichmentService));
        _htmlGenerator = htmlGenerator ?? throw new ArgumentNullException(nameof(htmlGenerator));
        _chunkedPipeline = chunkedPipeline;
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
            null,
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
        
        // --- Telemetry: start ---
        Telemetry?.Start(existingFeatureFiles.Count, existingTestFiles.Count);
        Telemetry?.MarkPhaseEnd("FileDiscovery");

        // Step 1: Parse all feature files in parallel
        var parseFeatureTasks = existingFeatureFiles.Select(async featurePath =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _parser.ParseFeature(featurePath), cancellationToken)
                .ConfigureAwait(false);
        });
        
        var parsedFeatures = (await Task.WhenAll(parseFeatureTasks).ConfigureAwait(false)).ToList();
        
        cancellationToken.ThrowIfCancellationRequested();

        // --- Telemetry: parsing complete ---
        Telemetry?.MarkPhaseEnd("Parsing");
        
        // Step 2 & 3: Parse and merge test results (fixed bug - avoid redundant parsing)
        var mergedTestResults = existingTestFiles.Any()
            ? await Task.Run(() => 
                _testReporter.ParseMultipleTestResults(existingTestFiles.ToArray()), 
                cancellationToken).ConfigureAwait(false)
            : new LivingDocGen.TestReporter.Models.TestExecutionReport();
        
        cancellationToken.ThrowIfCancellationRequested();

        // --- Telemetry: test result parsing complete ---
        Telemetry?.MarkPhaseEnd("TestResultParsing");
        
        // Step 4: Enrich documentation with test results
        var enrichedDoc = await Task.Run(() => 
            _enrichmentService.EnrichDocumentation(parsedFeatures, mergedTestResults, cancellationToken),
            cancellationToken).ConfigureAwait(false);
        
        enrichedDoc.Title = title ?? "BDD Living Documentation";
        enrichedDoc.GeneratedAt = DateTime.Now;
        
        cancellationToken.ThrowIfCancellationRequested();

        // --- Telemetry: enrichment complete ---
        Telemetry?.MarkPhaseEnd("Enrichment");
        
        // Step 5: Generate HTML
        var html = await Task.Run(() => 
            _htmlGenerator.GenerateHtml(enrichedDoc, options),
            cancellationToken).ConfigureAwait(false);

        // --- Telemetry: HTML generation complete ---
        Telemetry?.MarkPhaseEnd("HtmlGeneration");

        var totalScenarios = enrichedDoc.Features.Sum(f => f.Scenarios.Count);
        var totalSteps = enrichedDoc.Features.Sum(f => f.Scenarios.Sum(s => s.Steps.Count));
        Telemetry?.RecordOutputDimensions(
            enrichedDoc.Features.Count,
            totalScenarios,
            totalSteps,
            (long)(html.Length * sizeof(char)));
        
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

        // --- Telemetry: file write complete ---
        var fileSize = new FileInfo(outputPath).Length;
        Telemetry?.MarkPhaseEnd("FileWrite");
        Telemetry?.Complete(fileSize);
        
        _logger?.LogInformation("Generated living documentation: {OutputPath} ({FileSize:F2} KB)", 
            outputPath, fileSize / 1024.0);
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

    /// <summary>
    /// Generates chunked output artifacts (manifest, index, per-feature JSON chunks) to a directory.
    /// This is the dual-mode alternative to single HTML generation.
    /// </summary>
    /// <param name="featureFiles">Collection of absolute paths to feature files.</param>
    /// <param name="testResultFiles">Collection of absolute paths to test result files.</param>
    /// <param name="outputDirectory">Directory where chunked artifacts will be written.</param>
    /// <param name="title">Title for the documentation.</param>
    /// <param name="options">HTML generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing paths to all emitted artifacts.</returns>
    /// <exception cref="InvalidOperationException">Thrown when chunked pipeline is not configured.</exception>
    public async Task<ChunkedOutputResult> GenerateChunkedAsync(
        IEnumerable<string> featureFiles,
        IEnumerable<string> testResultFiles,
        string outputDirectory,
        string title = null,
        HtmlGenerationOptions options = null,
        CancellationToken cancellationToken = default)
    {
        if (_chunkedPipeline == null)
            throw new InvalidOperationException(
                "Chunked output pipeline is not configured. Register IChunkedOutputPipeline in dependency injection.");

        if (featureFiles == null)
            throw new ArgumentNullException(nameof(featureFiles));
        if (testResultFiles == null)
            throw new ArgumentNullException(nameof(testResultFiles));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

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
            _logger?.LogError("None of the {Count} provided feature files exist", featureFilesList.Count);
            throw new ArgumentException(
                $"None of the {featureFilesList.Count} provided feature files exist", nameof(featureFiles));
        }

        _logger?.LogInformation(
            "Generating chunked output for {FeatureCount} feature files and {TestCount} test result files",
            existingFeatureFiles.Count, existingTestFiles.Count);

        Telemetry?.Start(existingFeatureFiles.Count, existingTestFiles.Count);
        Telemetry?.MarkPhaseEnd("FileDiscovery");

        // Step 1: Parse feature files
        var parseFeatureTasks = existingFeatureFiles.Select(async featurePath =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => _parser.ParseFeature(featurePath), cancellationToken)
                .ConfigureAwait(false);
        });

        var parsedFeatures = (await Task.WhenAll(parseFeatureTasks).ConfigureAwait(false)).ToList();
        Telemetry?.MarkPhaseEnd("Parsing");

        cancellationToken.ThrowIfCancellationRequested();

        // Step 2: Parse test results
        var mergedTestResults = existingTestFiles.Any()
            ? await Task.Run(() =>
                _testReporter.ParseMultipleTestResults(existingTestFiles.ToArray()),
                cancellationToken).ConfigureAwait(false)
            : new LivingDocGen.TestReporter.Models.TestExecutionReport();

        Telemetry?.MarkPhaseEnd("TestResultParsing");

        cancellationToken.ThrowIfCancellationRequested();

        // Step 3: Enrich
        var enrichedDoc = await Task.Run(() =>
            _enrichmentService.EnrichDocumentation(parsedFeatures, mergedTestResults, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        enrichedDoc.Title = title ?? "BDD Living Documentation";
        enrichedDoc.GeneratedAt = DateTime.Now;

        Telemetry?.MarkPhaseEnd("Enrichment");

        cancellationToken.ThrowIfCancellationRequested();

        // Step 4: Execute chunked output pipeline
        options ??= new HtmlGenerationOptions();
        var result = await _chunkedPipeline.ExecuteAsync(enrichedDoc, outputDirectory, options, cancellationToken)
            .ConfigureAwait(false);

        Telemetry?.MarkPhaseEnd("ChunkedGeneration");

        _logger?.LogInformation(
            "Successfully generated chunked output: {FeatureCount} features, {ScenarioCount} scenarios, {ChunkCount} chunks",
            result.TotalFeatures, result.TotalScenarios, result.ChunkPaths.Count);

        return result;
    }
}
