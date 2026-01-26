using Reqnroll;
using System;
using System.Diagnostics;
using System.IO;
using LivingDocGen.Reqnroll.Integration.Bootstrap;

namespace IntegrationTest.Net8.Hooks
{
    [Binding]
    public class LivingDocGenBridge
    {
        [BeforeTestRun(Order = int.MinValue)]
        public static void BeforeAllTests()
        {
            try
            {
                // Add console trace listener to see Trace.WriteLine output
                Trace.Listeners.Add(new ConsoleTraceListener());
                
                Console.WriteLine("========================================");
                Console.WriteLine("BEFORE TEST RUN - LivingDocGen Starting");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine($"Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine("========================================");
                
                // Create marker file to verify hook execution
                var markerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HOOK_BEFORE_EXECUTED.txt");
                File.WriteAllText(markerPath, $"Before hook executed at {DateTime.Now}\nCurrent Dir: {Directory.GetCurrentDirectory()}\nBase Dir: {AppDomain.CurrentDomain.BaseDirectory}");
                
                LivingDocBootstrap.BeforeTestRun();
                
                Console.WriteLine("BeforeTestRun completed successfully");
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
                var debugLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DEBUG_AFTER.txt");
                File.WriteAllText(debugLog, $"After hook started at {DateTime.Now}\n");
                
                Console.WriteLine("========================================");
                Console.WriteLine("AFTER TEST RUN - LivingDocGen Generating");
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
                Console.WriteLine("========================================");
                
                // Create marker file to verify hook execution
                var markerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HOOK_AFTER_EXECUTED.txt");
                File.WriteAllText(markerPath, $"After hook executed at {DateTime.Now}");
                
                File.AppendAllText(debugLog, "About to call LivingDocBootstrap.AfterTestRun()\n");
                
                try
                {
                    LivingDocBootstrap.AfterTestRun();
                    File.AppendAllText(debugLog, "LivingDocBootstrap.AfterTestRun() completed successfully\n");
                }
                catch (Exception ex2)
                {
                    File.AppendAllText(debugLog, $"ERROR in LivingDocBootstrap.AfterTestRun(): {ex2.Message}\n{ex2.StackTrace}\n");
                    throw;
                }
                
                Console.WriteLine("========================================");
                Console.WriteLine("AFTER TEST RUN - LivingDocGen Complete");
                Console.WriteLine("========================================");
                
                // Flush trace output
                Trace.Flush();
            }
            catch (Exception ex)
            {
                var debugLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DEBUG_AFTER_ERROR.txt");
                File.WriteAllText(debugLog, $"ERROR in AfterAllTests: {ex.Message}\n{ex.StackTrace}\n");
                
                Console.WriteLine($"ERROR in AfterAllTests: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
   