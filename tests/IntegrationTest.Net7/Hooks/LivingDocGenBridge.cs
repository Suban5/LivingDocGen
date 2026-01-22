// This file bridges the test project to the LivingDocGen.Reqnroll.Integration package hooks
// Reqnroll doesn't auto-discover hooks from referenced assemblies, so we call them explicitly

using System;
using System.Diagnostics;
using Reqnroll;
 using NUnit.Framework;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace IntegrationTest.Net7.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        // Performance: Use volatile for thread-safe double-checked locking
        private static volatile bool _testRunStarted = false;
        private static volatile bool _testRunEnded = false;
        private static readonly object _lock = new object();
        private static int _totalScenarios = 0;

        // Primary: Use BeforeTestRun hook (called once before all tests)
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests()
        {
            Trace.WriteLine("🚀 LivingDocGen - Test run starting (BeforeTestRun hook)");
            LivingDocBootstrap.BeforeTestRun();
            _testRunStarted = true;
        }

        // Fallback: If BeforeTestRun doesn't fire, use double-checked locking pattern
        // Performance: Fast path check without lock for 1799/1800 scenarios
        [BeforeScenario(Order = int.MinValue)]
        public static void BeforeFirstScenario()
        {
            // Fast path: if already started, skip entirely (no lock acquisition)
            if (_testRunStarted) return;
            
            // Slow path: only executed once for the first scenario
            lock (_lock)
            {
                // Double-check after acquiring lock
                if (!_testRunStarted)
                {
                    _testRunStarted = true;
                    Trace.WriteLine("🚀 LivingDocGen - Test run starting (BeforeScenario fallback)");
                    LivingDocBootstrap.BeforeTestRun();
                }
            }
        }

        [AfterScenario(Order = int.MaxValue)]
        public static void AfterEachScenario()
        {
            lock (_lock)
            {
                _totalScenarios++;
            }
        }
        
        // Performance: Use volatile for lock-free check in most cases
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            // Fast path: if already ended, skip
            if (_testRunEnded) return;
            
            lock (_lock)
            {
                if (!_testRunEnded)
                {
                    _testRunEnded = true;
                    Trace.WriteLine($"📊 LivingDocGen - Test run completed ({_totalScenarios} scenarios)");
                    LivingDocBootstrap.AfterTestRun();
                }
            }
        }
    }
}
