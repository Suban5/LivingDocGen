using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LivingDocGen.Parser.Services;
using LivingDocGen.Parser.Models;
using LivingDocGen.Parser.Core;
using LivingDocGen.Parser.Parsers;
using LivingDocGen.TestReporter.Services;
using LivingDocGen.TestReporter.Core;
using LivingDocGen.TestReporter.Parsers;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Services.Chunked;
using LivingDocGen.Generator.Services.Rendering;
using LivingDocGen.Generator.Services.Telemetry;
using LivingDocGen.Generator.Models;
using LivingDocGen.CLI.Services;
using LivingDocGen.CLI.Models;
using Newtonsoft.Json;

namespace LivingDocGen.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Setup dependency injection
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        var rootCommand = new RootCommand("Universal BDD Living Documentation Generator");

        // Parse command
        var parseCommand = CreateParseCommand(serviceProvider);
        rootCommand.AddCommand(parseCommand);

        // Test Results command
        var testResultsCommand = CreateTestResultsCommand(serviceProvider);
        rootCommand.AddCommand(testResultsCommand);

        // Generate command
        var generateCommand = CreateGenerateCommand(serviceProvider);
        rootCommand.AddCommand(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(configure => configure.AddConsole());

        // Parser services
        services.AddSingleton<IFeatureParser>(sp => new GherkinParser(BDDFramework.Cucumber));
        services.AddSingleton<IUniversalParserService, UniversalParserService>();

        // Test Reporter services
        services.AddTransient<ITestResultParser, NUnit2ResultParser>();
        services.AddTransient<ITestResultParser, NUnitResultParser>();
        services.AddTransient<ITestResultParser, XUnitResultParser>();
        services.AddTransient<ITestResultParser, JUnitResultParser>();
        services.AddTransient<ITestResultParser, SpecFlowJsonResultParser>();
        services.AddTransient<ITestResultParser, TrxResultParser>();
        services.AddSingleton<ITestReportService, TestReportService>();

        // Generator services
        services.AddSingleton<IDocumentEnrichmentService, DocumentEnrichmentService>();
        services.AddSingleton<IHtmlGeneratorService, HtmlGeneratorService>();

        // Chunked output services
        services.AddSingleton<ITokenizerService, TokenizerService>();
        services.AddSingleton<IFeatureRenderer, FeatureRenderer>();
        services.AddSingleton<IIndexBuilderService, IndexBuilderService>();
        services.AddSingleton<IManifestBuilderService, ManifestBuilderService>();
        services.AddSingleton<IChunkEmitterService, ChunkEmitterService>();
        services.AddSingleton<IChunkedOutputPipeline, ChunkedOutputPipeline>();

        services.AddSingleton<ILivingDocumentationGenerator, LivingDocumentationGenerator>();

        return services;
    }

    static Command CreateParseCommand(ServiceProvider serviceProvider)
    {
        var parseCommand = new Command("parse", "Parse BDD feature files");
        
        var pathArgument = new Argument<string>(
            name: "path",
            description: "Path to feature file or directory");
        
        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output JSON file path");
        
        var frameworkOption = new Option<string?>(
            aliases: new[] { "--framework", "-f" },
            description: "BDD Framework (cucumber, specflow, reqnroll, jbehave)");
        
        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output");

        parseCommand.AddArgument(pathArgument);
        parseCommand.AddOption(outputOption);
        parseCommand.AddOption(frameworkOption);
        parseCommand.AddOption(verboseOption);

        parseCommand.SetHandler(async (path, output, framework, verbose) =>
        {
            var service = serviceProvider.GetRequiredService<IUniversalParserService>();
            var exitCode = await ParseHandler(service, path, output, framework, verbose);
            Environment.ExitCode = exitCode;
        }, pathArgument, outputOption, frameworkOption, verboseOption);

        return parseCommand;
    }

    static Command CreateTestResultsCommand(ServiceProvider serviceProvider)
    {
        var testResultsCommand = new Command("test-results", "Parse test execution results");
        
        var testPathArgument = new Argument<string>(
            name: "path",
            description: "Path to test result file or directory");
        
        var testOutputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output JSON file path");
        
        var testVerboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output");

        testResultsCommand.AddArgument(testPathArgument);
        testResultsCommand.AddOption(testOutputOption);
        testResultsCommand.AddOption(testVerboseOption);

        testResultsCommand.SetHandler(async (path, output, verbose) =>
        {
            var service = serviceProvider.GetRequiredService<ITestReportService>();
            var exitCode = await TestResultsHandler(service, path, output, verbose);
            Environment.ExitCode = exitCode;
        }, testPathArgument, testOutputOption, testVerboseOption);

        return testResultsCommand;
    }

    static Command CreateGenerateCommand(ServiceProvider serviceProvider)
    {
        var generateCommand = new Command("generate", "Generate living documentation HTML");
        
        var featuresArgument = new Argument<string?>(
            name: "features",
            description: "Path to feature files or directory (optional if using config file)",
            getDefaultValue: () => null);
        
        var testResultsArgument = new Argument<string?>(
            name: "test-results",
            description: "Path to test results or directory (optional)",
            getDefaultValue: () => null);
        
        var configOption = new Option<string?>(
            aliases: new[] { "--config" },
            description: "Path to bdd-livingdoc.json config file");
        
        var generateOutputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output HTML file path");
        
        var titleOption = new Option<string?>(
            aliases: new[] { "--title", "-t" },
            description: "Documentation title");
        
        var colorOption = new Option<string?>(
            aliases: new[] { "--color", "-c" },
            description: "Primary color (hex)");
        
        var themeOption = new Option<string?>(
            aliases: new[] { "--theme", "-th" },
            description: "Theme (purple, blue, green, dark, light, pickles)");
        
        var outputModeOption = new Option<string?>(
            aliases: new[] { "--output-mode" },
            description: "Output mode: 'chunked' (manifest + index + per-feature JSON, default) or 'legacy' (single HTML, deprecated)");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Show detailed output");

        generateCommand.AddArgument(featuresArgument);
        generateCommand.AddArgument(testResultsArgument);
        generateCommand.AddOption(configOption);
        generateCommand.AddOption(generateOutputOption);
        generateCommand.AddOption(titleOption);
        generateCommand.AddOption(colorOption);
        generateCommand.AddOption(themeOption);
        generateCommand.AddOption(outputModeOption);
        generateCommand.AddOption(verboseOption);

        generateCommand.SetHandler(async (context) =>
        {
            var features = context.ParseResult.GetValueForArgument(featuresArgument);
            var testResults = context.ParseResult.GetValueForArgument(testResultsArgument);
            var configPath = context.ParseResult.GetValueForOption(configOption);
            var output = context.ParseResult.GetValueForOption(generateOutputOption);
            var title = context.ParseResult.GetValueForOption(titleOption);
            var color = context.ParseResult.GetValueForOption(colorOption);
            var theme = context.ParseResult.GetValueForOption(themeOption);
            var outputMode = context.ParseResult.GetValueForOption(outputModeOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var generator = serviceProvider.GetRequiredService<ILivingDocumentationGenerator>();
            var exitCode = await GenerateHandler(generator, features, testResults, configPath, output, title, color, theme, outputMode, verbose);
            Environment.ExitCode = exitCode;
        });

        return generateCommand;
    }

    static async Task<int> ParseHandler(IUniversalParserService service, string path, string? output, string? frameworkStr, bool verbose)
    {
        try
        {
            // Validate input path
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("❌ Error: Path cannot be empty");
                return 1;
            }

            Console.WriteLine($"🔍 Parsing: {path}");
            
            BDDFramework? framework = frameworkStr?.ToLower() switch
            {
                "cucumber" => BDDFramework.Cucumber,
                "specflow" => BDDFramework.SpecFlow,
                "reqnroll" => BDDFramework.ReqnRoll,
                "jbehave" => BDDFramework.JBehave,
                _ => null
            };

            List<UniversalFeature> features;

            if (File.Exists(path))
            {
                var feature = service.ParseFeature(path, framework);
                features = new List<UniversalFeature> { feature };
            }
            else if (Directory.Exists(path))
            {
                features = service.ParseDirectory(path, framework);
            }
            else
            {
                Console.WriteLine("❌ Invalid path");
                return 1; // Error
            }

            // Get statistics
            var stats = service.GetStatistics(features);
            
            Console.WriteLine("\n✅ Parsing complete!");
            Console.WriteLine($"   Features: {stats.TotalFeatures}");
            Console.WriteLine($"   Scenarios: {stats.TotalScenarios}");
            Console.WriteLine($"   Steps: {stats.TotalSteps}");
            
            if (verbose)
            {
                Console.WriteLine("\n📊 Framework Distribution:");
                foreach (var (fw, count) in stats.FrameworkDistribution)
                {
                    Console.WriteLine($"   {fw}: {count}");
                }
            }

            // Serialize to JSON
            var json = JsonConvert.SerializeObject(features, Formatting.Indented);

            if (output != null)
            {
                await File.WriteAllTextAsync(output, json);
                Console.WriteLine($"\n💾 Output saved to: {output}");
            }
            else if (verbose)
            {
                Console.WriteLine("\n📄 JSON Output:");
                Console.WriteLine(json);
            }
            
            return 0; // Success
        }
        catch (Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"❌ Validation Error: {ex.Message}");
            return 1;
        }
        catch (Core.Exceptions.ParseException ex)
        {
            Console.WriteLine($"❌ Parse Error: {ex.Message}");
            if (verbose && ex.InnerException != null)
            {
                Console.WriteLine($"   Details: {ex.InnerException.Message}");
            }
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"❌ Access Denied: {ex.Message}");
            Console.WriteLine("   Check file permissions and try again.");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ I/O Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static async Task<int> TestResultsHandler(ITestReportService service, string path, string? output, bool verbose)
    {
        try
        {
            // Validate input path
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("❌ Error: Path cannot be empty");
                return 1;
            }

            Console.WriteLine($"🔍 Parsing test results: {path}");
            
            LivingDocGen.TestReporter.Models.TestExecutionReport report;

            if (File.Exists(path))
            {
                var framework = service.DetectFramework(path);
                Console.WriteLine($"📋 Detected framework: {framework}");
                report = service.ParseTestResults(path);
            }
            else if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                report = service.ParseMultipleTestResults(files);
            }
            else
            {
                Console.WriteLine("❌ Invalid path");
                return 1; // Error
            }

            // Display summary
            Console.WriteLine($"\n✅ Parsing complete!");
            Console.WriteLine($"   Total Features: {report.Features.Count}");
            Console.WriteLine($"   Total Scenarios: {report.Statistics.TotalScenarios}");
            Console.WriteLine($"   Passed: {report.Statistics.PassedScenarios}");
            Console.WriteLine($"   Failed: {report.Statistics.FailedScenarios}");
            Console.WriteLine($"   Skipped: {report.Statistics.SkippedScenarios}");
            
            if (verbose && report.Features.Any())
            {
                Console.WriteLine("\n📊 Detailed Results:");
                
                foreach (var feature in report.Features)
                {
                    Console.WriteLine($"\n   📁 {feature.FeatureName} ({feature.Status})");
                    Console.WriteLine($"      Duration: {feature.Duration:mm\\:ss\\.fff}");
                    Console.WriteLine($"      Scenarios: {feature.Scenarios.Count}");
                    
                    foreach (var scenario in feature.Scenarios)
                    {
                        var icon = scenario.Status switch
                        {
                            LivingDocGen.TestReporter.Models.ExecutionStatus.Passed => "✓",
                            LivingDocGen.TestReporter.Models.ExecutionStatus.Failed => "✗",
                            LivingDocGen.TestReporter.Models.ExecutionStatus.Skipped => "⊝",
                            _ => "○"
                        };
                        
                        Console.WriteLine($"      {icon} {scenario.ScenarioName} ({scenario.Duration:ss\\.fff}s)");
                        
                        if (scenario.Status == LivingDocGen.TestReporter.Models.ExecutionStatus.Failed && !string.IsNullOrEmpty(scenario.ErrorMessage))
                        {
                            Console.WriteLine($"         Error: {scenario.ErrorMessage.Truncate(100)}");
                        }
                    }
                }
            }

            // Serialize to JSON
            var json = JsonConvert.SerializeObject(report, Formatting.Indented);

            if (output != null)
            {
                await File.WriteAllTextAsync(output, json);
                Console.WriteLine($"\n💾 Output saved to: {output}");
            }
            else if (verbose)
            {
                Console.WriteLine("\n📄 JSON Output:");
                Console.WriteLine(json);
            }
            
            return 0; // Success
        }
        catch (Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"❌ Validation Error: {ex.Message}");
            return 1;
        }
        catch (Core.Exceptions.ParseException ex)
        {
            Console.WriteLine($"❌ Parse Error: {ex.Message}");
            if (verbose && ex.InnerException != null)
            {
                Console.WriteLine($"   Details: {ex.InnerException.Message}");
            }
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"❌ Access Denied: {ex.Message}");
            Console.WriteLine("   Check file permissions and try again.");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ I/O Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    static async Task<int> GenerateHandler(ILivingDocumentationGenerator generator, string? featuresPath, string? testResultsPath, string? configPath, string? output, string? title, string? color, string? theme, string? outputMode, bool verbose)
    {
        try
        {
            Console.WriteLine("🚀 Generating Living Documentation...\n");
            
            // Load configuration
            BddConfiguration? config = null;
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // Use specified config file
                config = ConfigurationService.LoadConfiguration(configPath);
                if (config != null)
                {
                    Console.WriteLine($"📄 Using config file: {configPath}");
                }
                else
                {
                    Console.WriteLine($"⚠️  Config file not found or invalid: {configPath}");
                }
            }
            else
            {
                // Try to find config file automatically
                var foundConfigPath = ConfigurationService.FindConfigurationFile();
                if (foundConfigPath != null)
                {
                    config = ConfigurationService.LoadConfiguration(foundConfigPath);
                    if (config != null)
                    {
                        Console.WriteLine($"📄 Using config file: {foundConfigPath}");
                    }
                }
            }
            
            // Check if disabled in config
            if (config?.Enabled == false)
            {
                Console.WriteLine("⏸️  Living documentation generation is disabled in config file.");
                return 0; // Not an error, just disabled
            }
            
            // Merge configuration with CLI arguments
            var (resolvedFeatures, resolvedTestResults, resolvedOutput, resolvedTitle, resolvedColor, resolvedTheme, resolvedVerbose) =
                ConfigurationService.MergeConfiguration(
                    config,
                    featuresPath,
                    testResultsPath,
                    output,
                    title,
                    color,
                    theme,
                    verbose);
            
            // Validate configuration
            ConfigurationService.ValidateConfiguration(resolvedFeatures, resolvedTestResults, resolvedOutput);
            
            if (resolvedVerbose)
            {
                Console.WriteLine($"\n🔧 Configuration:");
                Console.WriteLine($"   Features: {resolvedFeatures}");
                Console.WriteLine($"   Test Results: {resolvedTestResults ?? "(none)"}");
                Console.WriteLine($"   Output: {resolvedOutput}");
                Console.WriteLine($"   Title: {resolvedTitle ?? "(default)"}");
                Console.WriteLine($"   Theme: {resolvedTheme}");
                Console.WriteLine($"   Color: {resolvedColor}\n");
            }
            
            // Collect feature files
            var featureFiles = new List<string>();
            if (File.Exists(resolvedFeatures))
            {
                featureFiles.Add(resolvedFeatures);
            }
            else if (Directory.Exists(resolvedFeatures))
            {
                featureFiles.AddRange(Directory.GetFiles(resolvedFeatures, "*.feature", SearchOption.AllDirectories));
            }
            else
            {
                Console.WriteLine($"❌ Invalid features path: {resolvedFeatures}");
                Console.WriteLine($"   Current directory: {Directory.GetCurrentDirectory()}");
                return 1; // Error
            }

            Console.WriteLine($"📁 Found {featureFiles.Count} feature files");

            // Collect test result files
            var testResultFiles = new List<string>();
            if (!string.IsNullOrEmpty(resolvedTestResults))
            {
                if (File.Exists(resolvedTestResults))
                {
                    testResultFiles.Add(resolvedTestResults);
                }
                else if (Directory.Exists(resolvedTestResults))
                {
                    testResultFiles.AddRange(Directory.GetFiles(resolvedTestResults, "*.xml", SearchOption.AllDirectories));
                    testResultFiles.AddRange(Directory.GetFiles(resolvedTestResults, "*.trx", SearchOption.AllDirectories));
                    testResultFiles.AddRange(Directory.GetFiles(resolvedTestResults, "*.json", SearchOption.AllDirectories));
                }
            }

            if (testResultFiles.Any())
            {
                Console.WriteLine($"📊 Found {testResultFiles.Count} test result files");
            }
            else
            {
                Console.WriteLine("⚠️  No test results provided - generating documentation without test execution data");
            }

            // Generate HTML
            var options = new HtmlGenerationOptions
            {
                Theme = resolvedTheme
            };

            // Determine output mode (default: chunked since v2.1.0)
            var configOutputMode = config?.Advanced?.OutputMode;
            var effectiveOutputMode = outputMode ?? configOutputMode;

            var resolvedOutputMode = OutputMode.Chunked; // default is now chunked
            if (!string.IsNullOrWhiteSpace(effectiveOutputMode))
            {
                if (string.Equals(effectiveOutputMode, "legacy", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOutputMode = OutputMode.Legacy;
                    Console.WriteLine("⚠️  Legacy output mode is deprecated and will be removed in a future major release.");
                    Console.WriteLine("   Consider migrating to 'chunked' mode for better performance with large reports.");
                }
                else if (!string.Equals(effectiveOutputMode, "chunked", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"⚠️  Unknown output mode '{effectiveOutputMode}', using default 'chunked'");
                }
            }
            options.OutputMode = resolvedOutputMode;

            Console.WriteLine($"🎨 Using theme: {resolvedTheme}");
            Console.WriteLine($"📦 Output mode: {resolvedOutputMode.ToString().ToLowerInvariant()}");

            // Enable telemetry in verbose mode
            GenerationTelemetry? telemetry = null;
            if (resolvedVerbose && generator is LivingDocumentationGenerator concreteGenerator)
            {
                telemetry = new GenerationTelemetry();
                concreteGenerator.Telemetry = telemetry;
                Console.WriteLine("📊 Performance telemetry enabled");
            }

            if (resolvedOutputMode == OutputMode.Chunked)
            {
                // Chunked mode: emit manifest + index + per-feature JSON chunks
                if (generator is not LivingDocumentationGenerator concreteGen)
                {
                    Console.WriteLine("❌ Chunked output mode requires the concrete LivingDocumentationGenerator");
                    return 1;
                }

                // Use the output path's directory for chunked artifacts, or create a subdirectory
                var chunkedOutputDir = Path.GetDirectoryName(Path.GetFullPath(resolvedOutput));
                if (string.IsNullOrEmpty(chunkedOutputDir))
                    chunkedOutputDir = Directory.GetCurrentDirectory();

                // Use a dedicated subdirectory named after the output file (without extension)
                var outputBaseName = Path.GetFileNameWithoutExtension(resolvedOutput);
                chunkedOutputDir = Path.Combine(chunkedOutputDir, $"{outputBaseName}-chunked");

                var result = await concreteGen.GenerateChunkedAsync(
                    featureFiles,
                    testResultFiles,
                    chunkedOutputDir,
                    resolvedTitle,
                    options);

                // Finalize and display telemetry
                if (telemetry != null)
                {
                    telemetry.MarkPhaseEnd("FileWrite");
                    // Estimate total output size
                    long totalSize = 0;
                    if (File.Exists(result.ManifestPath))
                        totalSize += new FileInfo(result.ManifestPath).Length;
                    if (File.Exists(result.IndexPath))
                        totalSize += new FileInfo(result.IndexPath).Length;
                    foreach (var chunkPath in result.ChunkPaths)
                    {
                        if (File.Exists(chunkPath))
                            totalSize += new FileInfo(chunkPath).Length;
                    }
                    telemetry.Complete(totalSize);
                    MetricsReportWriter.WriteConsoleReport(telemetry.Metrics);

                    var metricsPath = Path.Combine(chunkedOutputDir, "generation.metrics.json");
                    MetricsReportWriter.WriteJsonReport(telemetry.Metrics, metricsPath);
                    Console.WriteLine($"📊 Metrics saved to: {metricsPath}");
                }

                Console.WriteLine($"\n🎉 Chunked output generated successfully!");
                Console.WriteLine($"   📁 Output directory: {chunkedOutputDir}");
                Console.WriteLine($"   📄 Manifest: {result.ManifestPath}");
                Console.WriteLine($"   📄 Index: {result.IndexPath}");
                Console.WriteLine($"   📄 Chunks: {result.ChunkPaths.Count} feature files");
                Console.WriteLine($"   📊 Features: {result.TotalFeatures}, Scenarios: {result.TotalScenarios}");
            }
            else
            {
                // Legacy mode: single self-contained HTML file
                var html = await generator.GenerateAsync(
                    featureFiles,
                    testResultFiles,
                    resolvedTitle,
                    options);

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(resolvedOutput);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                await File.WriteAllTextAsync(resolvedOutput, html);

                // Finalize and display telemetry
                if (telemetry != null)
                {
                    var outputFileSize = new FileInfo(resolvedOutput).Length;
                    telemetry.MarkPhaseEnd("FileWrite");
                    telemetry.Complete(outputFileSize);
                    MetricsReportWriter.WriteConsoleReport(telemetry.Metrics);

                    // Write metrics JSON alongside output
                    var metricsPath = Path.ChangeExtension(resolvedOutput, ".metrics.json");
                    MetricsReportWriter.WriteJsonReport(telemetry.Metrics, metricsPath);
                    Console.WriteLine($"📊 Metrics saved to: {metricsPath}");
                }

                Console.WriteLine($"\n🎉 Success! Open in browser:");
                Console.WriteLine($"   file://{Path.GetFullPath(resolvedOutput)}");
            }
            return 0; // Success
        }
        catch (Core.Exceptions.ConfigurationException ex)
        {
            Console.WriteLine($"❌ Configuration Error: {ex.Message}");
            return 1;
        }
        catch (Core.Exceptions.ValidationException ex)
        {
            Console.WriteLine($"❌ Validation Error: {ex.Message}");
            return 1;
        }
        catch (Core.Exceptions.ParseException ex)
        {
            Console.WriteLine($"❌ Parse Error: {ex.Message}");
            if (verbose && ex.InnerException != null)
            {
                Console.WriteLine($"   Details: {ex.InnerException.Message}");
            }
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"❌ Access Denied: {ex.Message}");
            Console.WriteLine("   Check file permissions and try again.");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ I/O Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }
}

// Extension method to add Truncate functionality
static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength) + "...";
    }
}
