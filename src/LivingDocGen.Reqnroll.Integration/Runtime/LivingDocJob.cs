namespace LivingDocGen.Reqnroll.Integration.Runtime
{
    /// <summary>
    /// Orchestrates the living documentation generation job.
    /// Launches a detached Worker process to generate documentation after tests complete.
    /// </summary>
    /// <remarks>
    /// The Worker process runs independently of the test host, solving the VSTest
    /// termination issue where test results are written AFTER hooks complete.
    /// </remarks>
    internal static class LivingDocJob
    {
        /// <summary>
        /// Schedules the living documentation generation by launching the Worker process.
        /// </summary>
        /// <param name="projectRoot">Root directory of the test project</param>
        /// <param name="testResultsPath">Directory where test results will be written</param>
        public static void Schedule(string projectRoot, string testResultsPath)
        {
            LivingDocLogger.Info("Scheduling living documentation generation via Worker");
            LivingDocLogger.Info($"  Project Root: {projectRoot}");
            LivingDocLogger.Info($"  Test Results: {testResultsPath}");

            // Always use detached Worker process for reliable generation
            DetachedGeneratorLauncher.Launch(projectRoot, testResultsPath);
        }
    }
}
