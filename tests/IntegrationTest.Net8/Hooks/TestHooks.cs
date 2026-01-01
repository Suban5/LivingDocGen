using Reqnroll;
using System;
using System.IO;

namespace IntegrationTest.Net6.Hooks
{
    [Binding]
    public class TestHooks
    {
        private static readonly object _lockObject = new object();
        private static readonly string _markerFile = Path.Combine(Path.GetTempPath(), $"livingdoc-{DateTime.Now:yyyyMMdd-HHmmss}.marker");
        private static int _scenarioCount = 0;

        [BeforeScenario(Order = int.MinValue)]
        public void BeforeScenario()
        {
            lock (_lockObject)
            {
                _scenarioCount++;
                
                // Log only once at the start
                if (_scenarioCount == 1)
                {
                    Console.WriteLine($"🚀 BDD Living Documentation - Test run starting (marker: {Path.GetFileName(_markerFile)})");
                }
            }
        }

        [AfterScenario(Order = int.MaxValue)]
        public void AfterScenario()
        {
            lock (_lockObject)
            {
                Console.WriteLine($"DEBUG: Scenario {_scenarioCount} completed. Marker exists: {File.Exists(_markerFile)}");
                
                // Generate documentation only once
                if (!File.Exists(_markerFile))
                {
                    File.WriteAllText(_markerFile, DateTime.Now.ToString());
                    
                    // Wait for other scenarios to complete
                    System.Threading.Thread.Sleep(3000);
                    
                    Console.WriteLine($"📊 Generating Living Documentation... (Scenario #{_scenarioCount})");
                }
            }
        }
    }
}
