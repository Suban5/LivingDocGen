using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace LivingDocGen.Reqnroll.Integration.Runtime
{
    /// <summary>
    /// Launches a detached process to generate documentation independently of test host.
    /// Solves VSTest termination issue - the worker process runs after test process exits.
    /// </summary>
    internal static class DetachedGeneratorLauncher
    {
        /// <summary>
        /// Starts a detached worker process to generate documentation.
        /// The process is completely independent and will not be killed by VSTest.
        /// </summary>
        public static void Launch(string projectRoot, string testResultsPath)
        {
            try
            {
                // Write job file with all context needed for generation
                var jobFile = Path.Combine(projectRoot, $"livingdoc-job-{Guid.NewGuid()}.json");
                
                var jobData = new
                {
                    ProjectRoot = projectRoot,
                    TestResultsPath = testResultsPath,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(jobData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(jobFile, json);

                // Find dotnet executable
                var dotnetPath = FindDotNetExecutable();
                
                // Build path to worker assembly (same directory as this integration package)
                var assemblyDir = Path.GetDirectoryName(typeof(DetachedGeneratorLauncher).Assembly.Location);
                
                // Try .dll first (Windows/Linux), then native executable (macOS)
                var workerDllPath = Path.Combine(assemblyDir, "LivingDocGen.Worker.dll");
                var workerExePath = Path.Combine(assemblyDir, "LivingDocGen.Worker");
                
                string workerPath = null;
                if (File.Exists(workerDllPath))
                {
                    workerPath = workerDllPath;
                }
                else if (File.Exists(workerExePath))
                {
                    workerPath = workerExePath;
                }

                // If worker doesn't exist, fall back to inline generation
                if (workerPath == null)
                {
                    LivingDocLogger.Warn($"Worker not found at: {workerDllPath}");
                    LivingDocLogger.Warn($"Also checked: {workerExePath}");
                    LivingDocLogger.Warn("Falling back to inline generation (may be terminated by VSTest)");
                    return;
                }

                LivingDocLogger.Info($"Found worker: {Path.GetFileName(workerPath)}");

                // Start detached process
                var psi = new ProcessStartInfo
                {
                    FileName = dotnetPath,
                    Arguments = $"\"{workerPath}\" --job \"{jobFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                var process = Process.Start(psi);
                
                LivingDocLogger.Info($"✅ Launched detached worker process (PID: {process?.Id})");
                LivingDocLogger.Info($"Worker will generate documentation independently of test process");
                LivingDocLogger.Info($"Job file: {jobFile}");

                // Do NOT call WaitForExit - that's the whole point!
                // The worker process will outlive the test process
            }
            catch (Exception ex)
            {
                LivingDocLogger.Error("Failed to launch detached worker", ex);
            }
        }

        private static string FindDotNetExecutable()
        {
            // On Windows: dotnet.exe, on Unix: dotnet
            var fileName = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "dotnet.exe"
                : "dotnet";

            // Check if dotnet is in PATH
            var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
            
            foreach (var dir in pathDirs)
            {
                var fullPath = Path.Combine(dir, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Fallback to just "dotnet" and let OS resolve it
            return "dotnet";
        }
    }
}
