using Reqnroll;
using System.Diagnostics;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace IntegrationTest.Net8.Hooks
{
    /// <summary>
    /// Reqnroll hooks that integrate LivingDocGen into the test execution lifecycle.
    /// Documentation is generated automatically after all tests complete.
    /// 
    /// Logs are written to:
    /// - LIVINGDOC_RUNTIME.log (detailed generation lifecycle)
    /// - Test output (via Trace.WriteLine)
    /// </summary>
    [Binding]
    public class LivingDocGenBridge
    {
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests()
        {
            // Add console trace listener to see Trace.WriteLine output in test results
            Trace.Listeners.Add(new ConsoleTraceListener());
            
            LivingDocBootstrap.BeforeTestRun();
        }
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            LivingDocBootstrap.AfterTestRun();
        }
    }
}
   