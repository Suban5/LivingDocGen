using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.Json;
using LivingDocGen.Generator.Services;
using LivingDocGen.Generator.Models;

namespace LivingDocGen.Reqnroll.Integration.Bootstrap
{
    /// <summary>
    /// Public bootstrap API for living documentation generation.
    /// Call these methods from your test project's Reqnroll hooks.
    /// 
    /// Self-contained: No global tool installation required!
    /// 
    /// Configuration:
    /// - Uses livingdocgen.json if present in project root
    /// - Falls back to default configuration (Features folder, TestResults folder)
    /// - Automatically detects test results after execution
    /// 
    /// Usage in your test project:
    /// <code>
    /// [Binding]
    /// public class LivingDocHooks
    /// {
    ///     [BeforeScenario(Order = int.MinValue)]
    ///     public static void BeforeFirstScenario()
    ///     {
    ///         LivingDocBootstrap.BeforeTestRun();
    ///     }
    ///     
    ///     [AfterTestRun(Order = int.MaxValue)]
    ///     public static void AfterAllTests()
    ///     {
    ///         LivingDocBootstrap.AfterTestRun();
    ///     }
    /// }
    /// </code>
    /// </summary>
    public static class LivingDocBootstrap
    {
        private static string _projectRoot;
        private static string _testResultsPath;

        /// <summary>
        /// Initialize living documentation generation.
        /// Call this from [BeforeScenario] or [BeforeTestRun] in your test project.
        /// </summary>
        public static void BeforeTestRun()
        {
            // Initialize paths at the start of test run
            _projectRoot = FindProjectRoot();
            _testResultsPath = Path.Combine(_projectRoot, "TestResults");
            
            Console.WriteLine("🚀 LivingDocGen - Test run starting");
            Console.WriteLine($"   Project Root: {_projectRoot}");
            Console.WriteLine($"   Test Runner: {GetTestRunnerName()}");
        }

        /// <summary>
        /// Generate living documentation after tests complete.
        /// Call this from [AfterTestRun] in your test project.
        /// </summary>
        public static void AfterTestRun()
        {
            // Wait for test results to be fully written to disk
            // This is important for all test runners as they write results asynchronously
            Thread.Sleep(3000);
            
            GenerateDocumentation();
        }

        private static void GenerateDocumentation()
        {
            try
            {
                Console.WriteLine("\n📊 Generating Living Documentation...");
                
                var configPath = Path.Combine(_projectRoot, "livingdocgen.json");
                
                // Load configuration
                GenerationConfig config;
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"   ✓ Using config file: {configPath}");
                    config = LoadConfiguration(configPath);
                }
                else
                {
                    Console.WriteLine("   ℹ Using default configuration");
                    config = GetDefaultConfiguration();
                }

                // Validate feature path
                if (!Directory.Exists(config.FeaturePath))
                {
                    Console.WriteLine($"   ⚠️ Features directory not found: {config.FeaturePath}");
                    Console.WriteLine("   Create a 'Features' folder or use livingdocgen.json to specify custom path");
                    return;
                }

                // Get feature files
                var featureFiles = Directory.GetFiles(config.FeaturePath, "*.feature", SearchOption.AllDirectories);
                
                if (featureFiles.Length == 0)
                {
                    Console.WriteLine($"   ⚠️ No .feature files found in: {config.FeaturePath}");
                    return;
                }

                Console.WriteLine($"   ✓ Found {featureFiles.Length} feature file(s)");

                // Find test results if available
                var testResultFiles = new string[0];
                var latestTestResult = FindLatestTestResult(config.TestResultsPath ?? _testResultsPath);
                
                if (!string.IsNullOrEmpty(latestTestResult) && config.IncludeTestResults)
                {
                    Console.WriteLine($"   ✓ Using test results: {Path.GetFileName(latestTestResult)}");
                    testResultFiles = new[] { latestTestResult };
                }
                else if (config.IncludeTestResults)
                {
                    Console.WriteLine($"   ⚠️ No test results found in: {config.TestResultsPath ?? _testResultsPath}");
                    Console.WriteLine("   Generating documentation without test execution data...");
                }

                // Generate HTML documentation
                var generator = new LivingDocumentationGenerator();
                var outputPath = Path.IsPathRooted(config.OutputPath) 
                    ? config.OutputPath 
                    : Path.Combine(_projectRoot, config.OutputPath);
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Generate documentation
                var options = new HtmlGenerationOptions
                {
                    Theme = config.Theme ?? "purple"
                };

                var task = generator.GenerateToFileAsync(
                    featureFiles,
                    testResultFiles,
                    outputPath,
                    config.Title,  // Title passed separately
                    options
                );
                
                task.Wait(); // Synchronously wait for completion

                Console.WriteLine("   ✅ Living documentation generated successfully!");
                Console.WriteLine($"   📄 file://{outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating living documentation: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }

        private static GenerationConfig LoadConfiguration(string configPath)
        {
            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<GenerationConfig>(jsonContent, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                
                // Make paths absolute if they're relative
                if (config != null)
                {
                    if (!Path.IsPathRooted(config.FeaturePath))
                        config.FeaturePath = Path.Combine(_projectRoot, config.FeaturePath);
                    
                    if (config.TestResultsPath != null && !Path.IsPathRooted(config.TestResultsPath))
                        config.TestResultsPath = Path.Combine(_projectRoot, config.TestResultsPath);
                }
                
                return config ?? GetDefaultConfiguration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Error loading config file: {ex.Message}");
                Console.WriteLine("   Using default configuration");
                return GetDefaultConfiguration();
            }
        }

        private static GenerationConfig GetDefaultConfiguration()
        {
            return new GenerationConfig
            {
                FeaturePath = Path.Combine(_projectRoot, "Features"),
                TestResultsPath = _testResultsPath,
                OutputPath = "living-documentation.html",
                Title = Path.GetFileName(_projectRoot) + " - Living Documentation",
                Theme = "purple",
                IncludeTestResults = true
            };
        }

        private static string GetTestRunnerName()
        {
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.ToLower();
            
            if (processName.Contains("testhost"))
                return "Visual Studio Test Explorer / dotnet test";
            if (processName.Contains("vstest"))
                return "Visual Studio Test Explorer";
            if (processName.Contains("resharper"))
                return "ReSharper Test Runner";
            if (processName.Contains("rider"))
                return "Rider Test Runner";
            
            return processName;
        }

        private static string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            
            // Walk up until we find a .csproj file
            var checkDir = currentDir;
            while (!string.IsNullOrEmpty(checkDir))
            {
                if (Directory.GetFiles(checkDir, "*.csproj").Any())
                {
                    return checkDir;
                }
                var parent = Directory.GetParent(checkDir);
                if (parent == null) break;
                checkDir = parent.FullName;
            }
            
            // Fallback to current directory
            return currentDir;
        }

        private static string FindLatestTestResult(string testResultsPath)
        {
            if (!Directory.Exists(testResultsPath))
                return null;

            try
            {
                // Look for test result files (NUnit XML, xUnit XML, TRX, SpecFlow JSON)
                var testFiles = Directory.GetFiles(testResultsPath, "*.xml", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(testResultsPath, "*.trx", SearchOption.AllDirectories))
                    .Concat(Directory.GetFiles(testResultsPath, "*.json", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith("livingdocgen.json") && !f.EndsWith("reqnroll.json")))
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .FirstOrDefault();

                return testFiles;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Configuration model for living documentation generation
    /// </summary>
    internal class GenerationConfig
    {
        public string FeaturePath { get; set; }
        public string TestResultsPath { get; set; }
        public string OutputPath { get; set; }
        public string Title { get; set; }
        public string Theme { get; set; }
        public bool IncludeTestResults { get; set; } = true;
    }
}
