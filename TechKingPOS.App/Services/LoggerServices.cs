using System;
using System.IO;

namespace TechKingPOS.App.Services
{
    public static class LoggerService
    {
        // ✅ FIX: NEVER write to Program Files
        // Use LocalAppData instead (installer-safe)
        private static readonly string LogDirectory =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TechKingPOS",
                "logs"
            );

        private static readonly string LogFile =
            Path.Combine(LogDirectory, "app.log");

        static LoggerService()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
            }
            catch
            {
                // ❗ Logger must NEVER crash the app
            }
        }

        // ✅ MAIN INFO METHOD (unchanged)
        public static void Info(
            string emoji,
            string category,
            string message,
            string? details = null)
        {
            WriteLog("INFO", emoji, category, message, details);
        }

        // ✅ ERROR METHOD (unchanged behavior)
        public static void Error(
            string emoji,
            string category,
            string message,
            Exception? ex = null)
        {
            var details = ex?.ToString();
            WriteLog("ERROR", emoji, category, message, details);
        }

        private static void WriteLog(
            string level,
            string emoji,
            string category,
            string message,
            string? details)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var logLine =
                    $"{timestamp} [{level}] {emoji} [{category}] {message}";

                if (!string.IsNullOrWhiteSpace(details))
                    logLine += $" | {details}";

                File.AppendAllText(LogFile, logLine + Environment.NewLine);

#if DEBUG
                Console.WriteLine(logLine);
#endif
            }
            catch
            {
                // ❗ Logging failure must NEVER break the app
            }
        }
    }
}
