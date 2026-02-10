using System;
using System.Diagnostics;
using System.IO;
using LivingDocGen.Reqnroll.Integration.Runtime;

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
    ///     [BeforeTestRun(Order = int.MinValue)]
    ///     public static void BeforeAllTests()
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
    /// 
    /// Performance Notes:
    /// - BeforeTestRun is called ONCE before all tests (not per scenario)
    /// - Optimized for large test suites (1000+ scenarios)
    /// - Uses Trace.WriteLine for proper test output visibility
    /// </summary>
    public static class LivingDocBootstrap
    {
        private static string _projectRoot;
        private static string _testResultsPath;

        /// <summary>
        /// Initialize living documentation generation.
        /// Call this from [BeforeTestRun] in your test project for optimal performance.
        /// Note: Using [BeforeScenario] will work but causes unnecessary overhead with large test suites.
        /// </summary>
        public static void BeforeTestRun()
        {
            // Initialize paths at the start of test run
            _projectRoot = FindProjectRoot();
            _testResultsPath = Path.Combine(_projectRoot, "TestResults");
            
            Trace.WriteLine("========================================");
            Trace.WriteLine("🚀 LivingDocGen - Test run starting");
            Trace.WriteLine($"   Project Root: {_projectRoot}");
            Trace.WriteLine($"   Test Results Path: {_testResultsPath}");
            Trace.WriteLine($"   Test Runner: {GetTestRunnerName()}");
            Trace.WriteLine("========================================");
        }

        /// <summary>
        /// Generate living documentation after tests complete.
        /// Call this from [AfterTestRun] in your test project.
        /// 
        /// Uses clean runtime architecture:
        /// - Hook returns immediately (doesn't block test runner)
        /// - Process stays alive until documentation completes
        /// - Test result files are found reliably
        /// </summary>
        public static void AfterTestRun()
        {
            Trace.WriteLine("========================================");
            Trace.WriteLine("📊 LivingDocGen - Scheduling post-test generation");
            Trace.WriteLine("========================================");
            
            LivingDocJob.Schedule(_projectRoot, _testResultsPath);
            
            Trace.WriteLine("✅ Hook returned - generation scheduled");
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
            // First, try to find project root from the test assembly location
            // This is more reliable than current directory during test execution
            var assemblyLocation = typeof(LivingDocBootstrap).Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                // Assembly is typically in: ProjectRoot/bin/Debug/net8.0/
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    // Walk up from assembly directory looking for .csproj
                    var checkDir = assemblyDir;
                    while (!string.IsNullOrEmpty(checkDir))
                    {
                        if (Directory.GetFiles(checkDir, "*.csproj").Length > 0)
                        {
                            // Verify livingdocgen.json exists here (confirms it's the right project)
                            if (File.Exists(Path.Combine(checkDir, "livingdocgen.json")))
                            {
                                return checkDir;
                            }
                        }
                        var parent = Directory.GetParent(checkDir);
                        if (parent == null) break;
                        checkDir = parent.FullName;
                    }
                }
            }
            
            // Fallback: try from current directory
            var currentDir = Directory.GetCurrentDirectory();
            
            // Walk up until we find a .csproj file
            var fallbackDir = currentDir;
            while (!string.IsNullOrEmpty(fallbackDir))
            {
                if (Directory.GetFiles(fallbackDir, "*.csproj").Length > 0)
                {
                    return fallbackDir;
                }
                var parent = Directory.GetParent(fallbackDir);
                if (parent == null) break;
                fallbackDir = parent.FullName;
            }
            
            // Last resort fallback to current directory
            return currentDir;
        }
    }
}
