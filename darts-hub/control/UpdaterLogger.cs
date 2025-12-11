using System;
using System.IO;
using System.Runtime.InteropServices;

namespace darts_hub.control
{
    /// <summary>
    /// Logger specifically for updater operations with monthly rotation
    /// </summary>
    public static class UpdaterLogger
    {
        private static readonly object lockObject = new object();
        private static string logFileName = string.Empty;
        private static string logDirectory = string.Empty;

        static UpdaterLogger()
        {
            InitializeLogger();
        }

        private static void InitializeLogger()
        {
            var basePath = Helper.GetAppBasePath();
            logDirectory = Path.Combine(basePath, "logs");
            
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Generate log filename with day only (will be overwritten monthly)
            var now = DateTime.Now;
            logFileName = Path.Combine(logDirectory, $"{now.Day:D2}_darts-hub.log");
            
            // Check if log file needs to be rotated
            CheckAndRotateLogFile();
        }

        private static void EnsureCurrentLogFile()
        {
            var now = DateTime.Now;
            var expectedFileName = Path.Combine(logDirectory, $"{now.Day:D2}_darts-hub.log");
            
            if (logFileName != expectedFileName)
            {
                logFileName = expectedFileName;
                CheckAndRotateLogFile();
            }
            else
            {
                // Even if filename is the same, check if it's from previous month
                CheckAndRotateLogFile();
            }
        }

        private static void CheckAndRotateLogFile()
        {
            try
            {
                if (File.Exists(logFileName))
                {
                    var lastWriteTime = File.GetLastWriteTime(logFileName);
                    var now = DateTime.Now;
                    
                    // Check if the file was last modified in a different month or year
                    if (lastWriteTime.Year != now.Year || lastWriteTime.Month != now.Month)
                    {
                        // Delete the old log file from previous month
                        File.Delete(logFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                // If rotation fails, log to console but don't prevent logging
                Console.WriteLine($"Log file rotation failed: {ex.Message}");
            }
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("WARN", message);
        }

        public static void LogError(string message, Exception? exception = null)
        {
            var errorMessage = exception != null ? $"{message} - Exception: {exception.Message}" : message;
            if (exception != null && !string.IsNullOrEmpty(exception.StackTrace))
            {
                errorMessage += $"\nStackTrace: {exception.StackTrace}";
            }
            Log("ERROR", errorMessage);
        }

        public static void LogDebug(string message)
        {
            Log("DEBUG", message);
        }

        private static void Log(string level, string message)
        {
            lock (lockObject)
            {
                try
                {
                    EnsureCurrentLogFile();
                    
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{level}] [UPDATER] {message}";
                    
                    File.AppendAllText(logFileName, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // If logging fails, we can't log the error, so we write to console as fallback
                    Console.WriteLine($"Logging failed: {ex.Message}");
                    Console.WriteLine($"Original message: [{level}] {message}");
                }
            }
        }

        public static void LogSystemInfo()
        {
            LogInfo("=== UPDATER SESSION STARTED ===");
            LogInfo($"Operating System: {Environment.OSVersion}");
            LogInfo($"Architecture: {RuntimeInformation.ProcessArchitecture}");
            
            string platform;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                platform = "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                platform = "Linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                platform = "macOS";
            else
                platform = "Unknown";
                
            LogInfo($"Platform: {platform}");
            LogInfo($"Current Version: {Updater.version}");
            LogInfo($"Base Path: {Helper.GetAppBasePath()}");
        }

        public static void LogSessionEnd()
        {
            LogInfo("=== UPDATER SESSION ENDED ===");
            LogInfo("");
        }
    }
}