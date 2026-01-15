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
        generateCommand.AddOption(verboseOption);

        generateCommand.SetHandler(async (features, testResults, configPath, output, title, color, theme, verbose) =>
        {
            var generator = serviceProvider.GetRequiredService<ILivingDocumentationGenerator>();
            var exitCode = await GenerateHandler(generator, features, testResults, configPath, output, title, color, theme, verbose);
            Environment.ExitCode = exitCode;
        }, featuresArgument, testResultsArgument, configOption, generateOutputOption, titleOption, colorOption, themeOption, verboseOption);

        return generateCommand;
    }

    static async Task<int> ParseHandler(IUniversalParserService service, string path, string? output, string? frameworkStr, bool verbose)
    {
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1; // Failure
        }
    }

    static async Task<int> TestResultsHandler(ITestReportService service, string path, string? output, bool verbose)
    {
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1; // Failure
        }
    }

    static async Task<int> GenerateHandler(ILivingDocumentationGenerator generator, string? featuresPath, string? testResultsPath, string? configPath, string? output, string? title, string? color, string? theme, bool verbose)
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

            Console.WriteLine($"🎨 Using theme: {resolvedTheme}");

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

            Console.WriteLine($"\n🎉 Success! Open in browser:");
            Console.WriteLine($"   file://{Path.GetFullPath(resolvedOutput)}");
            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }
            return 1; // Failure
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
