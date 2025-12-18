using System;
using System.IO;

namespace TechKingPOS.App.Services
{
    public static class LoggerService
    {
        private static readonly string LogDirectory =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        private static readonly string LogFile =
            Path.Combine(LogDirectory, "app.log");

        static LoggerService()
        {
            if (!Directory.Exists(LogDirectory))
                Directory.CreateDirectory(LogDirectory);
        }

        // ✅ MAIN INFO METHOD (emoji + category + message + optional details)
        public static void Info(
            string emoji,
            string category,
            string message,
            string? details = null)
        {
            WriteLog("INFO", emoji, category, message, details);
        }

        // ✅ ERROR METHOD
        public static void Error(
            string emoji,
            string category,
            string message,
            //string details = "",
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
    }
}
