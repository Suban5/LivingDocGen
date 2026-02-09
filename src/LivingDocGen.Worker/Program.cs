using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Diagnostics;
using LivingDocGen.Parser.Parsers;
using LivingDocGen.Parser.Models;
using LivingDocGen.Generator.Models;
using LivingDocGen.Generator.Services;
using LivingDocGen.TestReporter.Models;
using LivingDocGen.TestReporter.Services;

namespace LivingDocGen.Worker
{
    /// <summary>
    /// Detached worker process that generates living documentation independently of the test host.
    /// This allows generation to complete even after VSTest terminates the test process.
    /// </summary>
    internal class Program
    {
        private const int MaxRetries = 60;  // Increased retries
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);  // 1 second between retries
        private static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(2);  // Wait up to 2 minutes
        private static readonly TimeSpan FileStabilityDelay = TimeSpan.FromSeconds(2);  // Wait for file to stabilize

        static int Main(string[] args)
        {
            return MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task<int> MainAsync(string[] args)
        {
            var jobFilePath = ParseArguments(args);
            if (string.IsNullOrEmpty(jobFilePath))
            {
                Console.Error.WriteLine("Error: --job argument required");
                Console.Error.WriteLine("Usage: LivingDocGen.Worker --job <path-to-job-file>");
                return 1;
            }

            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Worker started (PID: {Process.GetCurrentProcess().Id})");
                
                // Initial delay to let test process complete and write results
                // This is critical - VSTest writes TRX AFTER [AfterTestRun] hook completes
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting 3 seconds for test process to complete...");
                await Task.Delay(3000);
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Reading job file: {jobFilePath}");

                var job = ReadJobFile(jobFilePath);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Job loaded - ProjectRoot: {job.ProjectRoot}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] TestResultsPath: {job.TestResultsPath}");

                // Load livingdocgen.json configuration
                var configPath = Path.Combine(job.ProjectRoot, "livingdocgen.json");
                if (!File.Exists(configPath))
                {
                    Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error: Configuration file not found: {configPath}");
                    return 1;
                }

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Loading configuration: {configPath}");
                var configJson = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ConfigData>(configJson);
                
                if (config == null)
                {
                    Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error: Failed to parse configuration");
                    return 1;
                }

                // Extract paths and settings using helper methods (supports both formats)
                var featuresPath = config.GetFeaturesPath();
                var outputPath = config.GetOutputPath();
                var title = config.GetTitle();
                var theme = config.GetTheme();
                var testResultPatterns = config.GetTestResultPatterns();

                // Resolve paths relative to project root
                featuresPath = Path.IsPathRooted(featuresPath)
                    ? featuresPath
                    : Path.Combine(job.ProjectRoot, featuresPath);

                outputPath = Path.IsPathRooted(outputPath)
                    ? outputPath
                    : Path.Combine(job.ProjectRoot, outputPath);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Features path: {featuresPath}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Output path: {outputPath}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Test result formats: {string.Join(", ", testResultPatterns)}");

                // Wait for test result files to appear (with timeout)
                var testResultFiles = WaitForTestResults(job.TestResultsPath, testResultPatterns);
                if (testResultFiles.Length == 0)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Warning: No test result files found after waiting {MaxWaitTime.TotalSeconds}s");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Proceeding with documentation generation anyway");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Found {testResultFiles.Length} test result file(s):");
                    foreach (var file in testResultFiles)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}]   - {Path.GetFileName(file)}");
                    }
                }

                // Parse features
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Parsing feature files from: {featuresPath}");
                var featureParser = new GherkinParser();
                var featureFiles = Directory.GetFiles(featuresPath, "*.feature", SearchOption.AllDirectories);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Found {featureFiles.Length} feature file(s)");

                // Parse test results
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Parsing test results...");

                // Generate documentation using the main generator
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Generating HTML documentation...");
                var generator = new LivingDocumentationGenerator();
                
                var html = await generator.GenerateAsync(
                    featureFiles,
                    testResultFiles,
                    title,
                    options: new HtmlGenerationOptions { Theme = theme },
                    cancellationToken: CancellationToken.None
                );

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Generated {html.Length / 1024} KB of HTML");

                // Write output
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Writing to: {outputPath}");
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllText(outputPath, html);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Documentation generated successfully!");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] File size: {new FileInfo(outputPath).Length / 1024} KB");

                // Clean up job file
                if (File.Exists(jobFilePath))
                {
                    File.Delete(jobFilePath);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Cleaned up job file");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ❌ Error: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private static string? ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--job" || args[i] == "-j")
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static JobData ReadJobFile(string jobFilePath)
        {
            if (!File.Exists(jobFilePath))
            {
                throw new FileNotFoundException($"Job file not found: {jobFilePath}");
            }

            var json = File.ReadAllText(jobFilePath);
            var job = JsonSerializer.Deserialize<JobData>(json);
            
            if (job == null)
            {
                throw new InvalidOperationException("Failed to deserialize job file");
            }

            return job;
        }

        private static string[] WaitForTestResults(string testResultsPath, string[] patterns)
        {
            var stopwatch = Stopwatch.StartNew();
            int attemptCount = 0;
            long lastFileSize = 0;
            int stableCount = 0;
            const int requiredStableChecks = 3;  // File size must be stable for 3 consecutive checks

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting for test results in: {testResultsPath}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Looking for patterns: {string.Join(", ", patterns)}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Max wait time: {MaxWaitTime.TotalSeconds}s");

            while (stopwatch.Elapsed < MaxWaitTime && attemptCount < MaxRetries)
            {
                attemptCount++;

                if (Directory.Exists(testResultsPath))
                {
                    // Search for files matching all configured patterns
                    var allFiles = patterns
                        .SelectMany(pattern => Directory.GetFiles(testResultsPath, pattern, SearchOption.AllDirectories))
                        .Distinct()
                        .ToArray();

                    if (allFiles.Length > 0)
                    {
                        // Check if files are still being written by monitoring size stability
                        long currentTotalSize = allFiles.Sum(f => new FileInfo(f).Length);
                        
                        if (currentTotalSize > 0 && currentTotalSize == lastFileSize)
                        {
                            stableCount++;
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] File size stable: {currentTotalSize} bytes (check {stableCount}/{requiredStableChecks})");
                            
                            if (stableCount >= requiredStableChecks)
                            {
                                // Additional delay to ensure file handles are released
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting {FileStabilityDelay.TotalSeconds}s for file handles to be released...");
                                Thread.Sleep(FileStabilityDelay);
                                
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✅ Test results ready after {stopwatch.ElapsedMilliseconds}ms");
                                return allFiles;
                            }
                        }
                        else
                        {
                            stableCount = 0;  // Reset if size changed
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] File size changed: {lastFileSize} → {currentTotalSize} bytes");
                        }
                        
                        lastFileSize = currentTotalSize;
                    }
                }

                if (attemptCount % 5 == 0)  // Log every 5 attempts
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Still waiting for test results... ({stopwatch.Elapsed.TotalSeconds:F0}s elapsed)");
                }
                
                Thread.Sleep(RetryDelay);
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠️ No stable test results found after {stopwatch.ElapsedMilliseconds}ms");
            return Array.Empty<string>();
        }

        private class JobData
        {
            public string ProjectRoot { get; set; } = string.Empty;
            public string TestResultsPath { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        // Simplified config classes for JSON deserialization - supports both flat and nested formats
        private class ConfigData
        {
            // Nested format (new)
            [JsonPropertyName("paths")]
            public PathsConfig? Paths { get; set; }

            [JsonPropertyName("documentation")]
            public DocumentationConfig? Documentation { get; set; }

            [JsonPropertyName("testResults")]
            public TestResultsConfig? TestResults { get; set; }
            
            // Flat format (legacy - for backward compatibility)
            [JsonPropertyName("featurePath")]
            public string? FeaturePath { get; set; }
            
            [JsonPropertyName("testResultsPath")]
            public string? TestResultsPath { get; set; }
            
            [JsonPropertyName("outputPath")]
            public string? OutputPath { get; set; }
            
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            
            [JsonPropertyName("theme")]
            public string? Theme { get; set; }

            [JsonPropertyName("testResultFormat")]
            public string? TestResultFormat { get; set; }
            
            // Helper methods to get values from either format
            public string GetFeaturesPath() => Paths?.Features ?? FeaturePath ?? "./Features";
            public string GetOutputPath() => Paths?.Output ?? OutputPath ?? "./living-documentation.html";
            public string GetTitle() => Documentation?.Title ?? Title ?? "Living Documentation";
            public string GetTheme() => Documentation?.Theme ?? Theme ?? "purple";
            
            /// <summary>
            /// Gets the file patterns to search for test results.
            /// Supports: trx (VSTest), xml (NUnit/xUnit/JUnit), json (SpecFlow/Reqnroll)
            /// Default: ["*.trx", "*.xml"] for backward compatibility
            /// </summary>
            public string[] GetTestResultPatterns()
            {
                // Check nested config patterns first (explicit patterns take priority)
                if (TestResults?.Patterns != null && TestResults.Patterns.Length > 0)
                    return TestResults.Patterns;
                
                // Check nested config format shorthand
                if (!string.IsNullOrEmpty(TestResults?.Format))
                {
                    return GetPatternsForFormat(TestResults.Format);
                }
                
                // Check legacy single format (backward compatibility)
                if (!string.IsNullOrEmpty(TestResultFormat))
                {
                    return GetPatternsForFormat(TestResultFormat);
                }
                
                // Default: TRX and XML (covers most frameworks)
                return new[] { "*.trx", "*.xml" };
            }
            
            private static string[] GetPatternsForFormat(string format)
            {
                return format.ToLowerInvariant() switch
                {
                    "trx" => new[] { "*.trx" },
                    "nunit" or "nunit3" or "nunit2" => new[] { "*.xml" },
                    "xunit" => new[] { "*.xml" },
                    "junit" => new[] { "*.xml" },
                    "specflow" or "reqnroll" or "json" => new[] { "*.json" },
                    "all" => new[] { "*.trx", "*.xml", "*.json" },
                    _ => new[] { "*.trx", "*.xml" }
                };
            }
        }

        private class PathsConfig
        {
            [JsonPropertyName("features")]
            public string? Features { get; set; }

            [JsonPropertyName("output")]
            public string? Output { get; set; }
        }

        private class DocumentationConfig
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("theme")]
            public string? Theme { get; set; }
        }

        private class TestResultsConfig
        {
            /// <summary>
            /// File patterns to search for test results.
            /// Examples: ["*.trx"], ["*.xml"], ["*.trx", "*.xml"], ["test-results*.json"]
            /// </summary>
            [JsonPropertyName("patterns")]
            public string[]? Patterns { get; set; }

            /// <summary>
            /// Shorthand format name that sets appropriate patterns automatically.
            /// Supported: trx, nunit, xunit, junit, specflow, all
            /// </summary>
            [JsonPropertyName("format")]
            public string? Format { get; set; }

            /// <summary>
            /// Subdirectory to search for test results (relative to project root).
            /// Default: "TestResults"
            /// </summary>
            [JsonPropertyName("directory")]
            public string? Directory { get; set; }
        }
    }
}
