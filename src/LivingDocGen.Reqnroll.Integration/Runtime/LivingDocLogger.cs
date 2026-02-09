using System;
using System.IO;

namespace LivingDocGen.Reqnroll.Integration.Runtime
{
    /// <summary>
    /// Shutdown-safe logging for LivingDocGen runtime operations.
    /// Writes to LIVINGDOC_RUNTIME.log in test output directory.
    /// </summary>
    internal static class LivingDocLogger
    {
        private static readonly string LogFile =
            Path.Combine(
                AppContext.BaseDirectory,
                "LIVINGDOC_RUNTIME.log"
            );

        public static void Info(string msg) => Write("INFO", msg);
        
        public static void Warn(string msg) => Write("WARN", msg);
        
        public static void Error(string msg, Exception ex)
            => Write("ERROR", $"{msg}\n{ex}");

        private static void Write(string level, string msg)
        {
            try
            {
                File.AppendAllText(
                    LogFile,
                    $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}\n"
                );
            }
            catch
            {
                // Never throw during shutdown
            }
        }
    }
}
