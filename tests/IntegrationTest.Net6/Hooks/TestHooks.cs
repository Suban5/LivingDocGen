using Reqnroll;
using System;
using System.Diagnostics;
using System.IO;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace IntegrationTest.Net6.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests()
        {
            try
            {
                Trace.Listeners.Add(new ConsoleTraceListener());
                
                Console.WriteLine("========================================");
                Console.WriteLine("BEFORE TEST RUN - LivingDocGen Starting");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine("========================================");
                
                LivingDocBootstrap.BeforeTestRun();
                
                Console.WriteLine("BeforeTestRun completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in BeforeAllTests: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        [AfterTestRun(Order = int.MaxValue)]
        public static void AfterAllTests()
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("AFTER TEST RUN - LivingDocGen Generating");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine("========================================");
                
                LivingDocBootstrap.AfterTestRun();
                
                Console.WriteLine("========================================");
                Console.WriteLine("AFTER TEST RUN - Complete");
                Console.WriteLine("========================================");
                
                Trace.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in AfterAllTests: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
