// This file bridges the test project to the LivingDocGen.Reqnroll.Integration package hooks
// Reqnroll doesn't auto-discover hooks from referenced assemblies, so we call them explicitly

using System;
using Reqnroll;
 using NUnit.Framework;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace IntegrationTest.Net7.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        private static bool _testRunStarted = false;
        private static bool _testRunEnded = false;
        private static readonly object _lock = new object();
        private static int _scenarioCount = 0;
        private static int _totalScenarios = 0;

        [BeforeScenario(Order = int.MinValue)]
        public static void BeforeFirstScenario()
        {
            lock (_lock)
            {
                _scenarioCount++;
                if (!_testRunStarted)
                {
                    _testRunStarted = true;
                    Console.WriteLine("🚀 LivingDocGen - Test run starting");
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
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            lock (_lock)
            {
                if (!_testRunEnded)
                {
                    _testRunEnded = true;
                    Console.WriteLine($"📊 LivingDocGen - Test run completed ({_totalScenarios} scenarios)");
                    LivingDocBootstrap.AfterTestRun();
                }
            }
        }
    }
}
