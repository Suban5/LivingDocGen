using Reqnroll;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LivingDocGen.Reqnroll.Integration.Hooks
{
    /// <summary>
    /// Reqnroll hooks to automatically generate living documentation after test execution.
    /// Add this to your Reqnroll test project.
    /// Supports livingdocgen.json configuration file.
    /// </summary>
    [Binding]
    public class LivingDocumentationHooks
    {
        private static string _projectRoot;
        private static string _testResultsPath;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // Find project root (adjust path as needed)
            _projectRoot = FindProjectRoot();
            _testResultsPath = Path.Combine(_projectRoot, "TestResults");
            
            Console.WriteLine("🚀 BDD Living Documentation - Test run starting");
            Console.WriteLine($"   Project Root: {_projectRoot}");
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            try
            {
                Console.WriteLine("\n📊 Generating Living Documentation...");
                
                var configPath = Path.Combine(_projectRoot, "livingdocgen.json");
                var cliPath = GetBDDCliPath();

                // Wait for test results to be written (NUnit/xUnit may delay file writes)
                System.Threading.Thread.Sleep(2000);

                string arguments;

                // Check if config file exists
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"   Using config file: {configPath}");
                    arguments = $"generate --config \"{configPath}\"";
                }
                else
                {
                    Console.WriteLine("   No config file found, using default settings");
                    
                    // Configuration
                    var config = new
                    {
                        FeaturePath = Path.Combine(_projectRoot, "Features"),
                        TestResultsPath = _testResultsPath,
                        OutputPath = Path.Combine(_projectRoot, "living-documentation.html"),
                        Title = "Living Documentation",
                        Theme = "purple"
                    };

                    // Verify paths exist
                    if (!Directory.Exists(config.FeaturePath))
                    {
                        Console.WriteLine($"⚠️ Features directory not found: {config.FeaturePath}");
                        return;
                    }

                    // Find latest test result file
                    var latestTestResult = FindLatestTestResult(config.TestResultsPath);
                    
                    if (string.IsNullOrEmpty(latestTestResult))
                    {
                        Console.WriteLine($"⚠️ No test results found in: {config.TestResultsPath}");
                        Console.WriteLine("   Generating documentation without test results...");
                    }

                    // Build command
                    arguments = string.IsNullOrEmpty(latestTestResult)
                        ? $"generate \"{config.FeaturePath}\" --output \"{config.OutputPath}\" --title \"{config.Title}\" --theme {config.Theme}"
                        : $"generate \"{config.FeaturePath}\" \"{latestTestResult}\" --output \"{config.OutputPath}\" --title \"{config.Title}\" --theme {config.Theme}";
                }

                // Execute BDD CLI tool
                var processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{cliPath}\" {arguments}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _projectRoot
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        var errors = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine($"✅ Living documentation generated successfully");
                            if (!string.IsNullOrWhiteSpace(output))
                                Console.WriteLine(output);
                        }
                        else
                        {
                            Console.WriteLine($"❌ Failed to generate documentation:");
                            Console.WriteLine(errors);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating living documentation: {ex.Message}");
            }
        }

        private static string FindProjectRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            
            // Walk up until we find a .csproj file
            while (!string.IsNullOrEmpty(currentDir))
            {
                if (Directory.GetFiles(currentDir, "*.csproj").Any())
                {
                    return currentDir;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            
            return Directory.GetCurrentDirectory();
        }

        private static string FindLatestTestResult(string testResultsPath)
        {
            if (!Directory.Exists(testResultsPath))
                return null;

            // Look for NUnit, xUnit, or JUnit XML files
            var testFiles = Directory.GetFiles(testResultsPath, "*.xml", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(testResultsPath, "*.trx", SearchOption.AllDirectories))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .FirstOrDefault();

            return testFiles;
        }

        private static string GetBDDCliPath()
        {
            // Option 1: Use environment variable
            var cliPath = Environment.GetEnvironmentVariable("BDD_CLI_PATH");
            if (!string.IsNullOrEmpty(cliPath) && File.Exists(cliPath))
                return cliPath;

            // Option 2: Use NuGet package tools folder (if distributed as package)
            // The path would be in the .nuget packages cache
            
            // Option 3: Use relative path (for development)
            var devPath = Path.Combine(_projectRoot, "..", "..", "src", "BDD.CLI", "bin", "Debug", "net8.0", "BDD.CLI.dll");
            if (File.Exists(devPath))
                return devPath;

            // Option 4: Global tool installation
            // User installs: dotnet tool install --global BDD.LivingDocGenerator
            // Then use: "bdd-livingdoc" instead of "dotnet <path>"
            
            throw new FileNotFoundException(
                "BDD CLI tool not found. Please set BDD_CLI_PATH environment variable or install as global tool.");
        }
    }
}
