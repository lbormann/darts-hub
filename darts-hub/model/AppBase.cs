using darts_hub.control;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace darts_hub.model
{
    /// <summary>
    /// Configuration class for log level patterns
    /// </summary>
    public class LoggingConfig
    {
        public Dictionary<string, LogLevelConfig> LogLevelPatterns { get; set; } = new();
        public FallbackRules FallbackRules { get; set; } = new();
        public LoggingSettings Settings { get; set; } = new();
    }

    public class LogLevelConfig
    {
        public List<string> Patterns { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
    }

    public class FallbackRules
    {
        public string ErrorStreamDefault { get; set; } = "WARN";
        public string StandardStreamDefault { get; set; } = "INFO";
        public string EmptyMessageDefault { get; set; } = "INFO";
    }

    public class LoggingSettings
    {
        public bool CaseSensitive { get; set; } = false;
        public bool UseRegexPatterns { get; set; } = true;
        public bool UseKeywordMatching { get; set; } = true;
        public int PatternTimeout { get; set; } = 100;
    }

    /// <summary>
    /// Main functions for using an app
    /// </summary>
    public abstract class AppBase : IApp, INotifyPropertyChanged
    {

        // ATTRIBUTES
        public const int MaxAppMonitorEntries = 600;

        public string Name { get; set; }
        public string CustomName { get; set; }
        public string? HelpUrl { get; set; }
        public string? ChangelogUrl { get; set; }
        public string? DescriptionShort { get; set; }
        public string? DescriptionLong { get; private set; }
        public bool RunAsAdmin { get; private set; }
        public bool Chmod { get; set; }
        public ProcessWindowStyle StartWindowState { get; private set; }
        public Configuration? Configuration { get; protected set; }

        public event EventHandler<AppEventArgs>? AppConfigurationRequired;

        [JsonIgnore]
        public Argument? ArgumentRequired { get; private set; }

        [JsonIgnore]
        public string AppConsoleStdOutput { get; private set; }

        [JsonIgnore]
        public string AppConsoleStdError { get; private set; }

        
        private bool _appRunningState;
        [JsonIgnore]
        public bool AppRunningState
        {
            get => _appRunningState;
            private set
            {
                if (_appRunningState != value)
                {
                    _appRunningState = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public int AppMonitorEntries { get; private set; }


        private string _appMonitor;
        [JsonIgnore]
        public string AppMonitor
        {
            get => _appMonitor;
            private set
            {
                if (_appMonitor != value)
                {
                    _appMonitor = value;
                    AppMonitorAvailable = _appMonitor != String.Empty ? true : false;
                    OnPropertyChanged();
                }
            }
        }



        
        private bool _appMonitorAvailable;
        [JsonIgnore]
        public bool AppMonitorAvailable
        {
            get => _appMonitorAvailable;
            private set
            {
                if (_appMonitorAvailable != value)
                {
                    _appMonitorAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? executable;
        private readonly ProcessWindowStyle DefaultStartWindowState = ProcessWindowStyle.Minimized;
        protected Dictionary<string, string>? runtimeArguments;
        private TaskCompletionSource<bool> eventHandled;
        private Process process;
        private const int defaultProcessId = 0;
        private int processId;
        
        // Daily logging attributes
        private string? currentLogFilePath;
        private int currentLogDay = -1;
        private readonly object logFileLock = new object();
        
        // Configurable logging system
        private static LoggingConfig? loggingConfig;
        private static readonly object configLock = new object();
        private static DateTime lastConfigLoad = DateTime.MinValue;
        
        public event PropertyChangedEventHandler PropertyChanged;

        // METHODS

        public AppBase(string name,
                        string? customName,
                        string? helpUrl,
                        string? changelogUrl,
                        string? descriptionShort,
                        string? descriptionLong,
                        bool runAsAdmin,
                        bool chmod,
                        ProcessWindowStyle? startWindowState,
                        Configuration? configuration = null
            )
        {
            Name = name;
            CustomName = customName;
            HelpUrl = helpUrl;
            ChangelogUrl = changelogUrl;
            DescriptionShort = descriptionShort;
            DescriptionLong = descriptionLong;
            RunAsAdmin = runAsAdmin;
            Chmod = chmod;
            StartWindowState = (ProcessWindowStyle)(startWindowState == null ? DefaultStartWindowState : startWindowState);
            Configuration = configuration;

            processId = defaultProcessId;
            AppRunningState = false;
        }

        /// <summary>
        /// Loads logging configuration from JSON file
        /// </summary>
        private static LoggingConfig LoadLoggingConfig()
        {
            lock (configLock)
            {
                // Check if we need to reload config (every 60 seconds or first time)
                if (loggingConfig == null || DateTime.Now.Subtract(lastConfigLoad).TotalSeconds > 60)
                {
                    try
                    {
                        var configPath = Path.Combine(Environment.CurrentDirectory, "logging-config.json");
                        
                        System.Diagnostics.Debug.WriteLine($"[AppBase] Looking for logging config at: {configPath}");
                        System.Diagnostics.Debug.WriteLine($"[AppBase] Current working directory: {Environment.CurrentDirectory}");
                        System.Diagnostics.Debug.WriteLine($"[AppBase] File exists check: {File.Exists(configPath)}");
                        
                        if (File.Exists(configPath))
                        {
                            var jsonContent = File.ReadAllText(configPath);
                            System.Diagnostics.Debug.WriteLine($"[AppBase] Config file content length: {jsonContent.Length} characters");
                            
                            loggingConfig = JsonConvert.DeserializeObject<LoggingConfig>(jsonContent);
                            lastConfigLoad = DateTime.Now;
                            System.Diagnostics.Debug.WriteLine("[AppBase] Logging configuration loaded successfully from file");
                            
                            // Debug output for loaded config
                            var totalKeywords = loggingConfig.LogLevelPatterns.Sum(p => p.Value.Keywords?.Count ?? 0);
                            System.Diagnostics.Debug.WriteLine($"[AppBase] Loaded {loggingConfig.LogLevelPatterns.Count} log levels with {totalKeywords} total keywords");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[AppBase] Logging config file not found, using default configuration");
                            System.Diagnostics.Debug.WriteLine($"[AppBase] Searched in: {configPath}");
                            
                            // List all files in current directory for debugging
                            try
                            {
                                var filesInCurrentDir = Directory.GetFiles(Environment.CurrentDirectory, "*.json");
                                System.Diagnostics.Debug.WriteLine($"[AppBase] JSON files in current directory ({Environment.CurrentDirectory}):");
                                foreach (var file in filesInCurrentDir)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[AppBase]   - {Path.GetFileName(file)}");
                                }
                                
                                if (filesInCurrentDir.Length == 0)
                                {
                                    System.Diagnostics.Debug.WriteLine("[AppBase]   No JSON files found in current directory");
                                }
                            }
                            catch (Exception dirEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[AppBase] Error listing directory contents: {dirEx.Message}");
                            }
                            
                            loggingConfig = CreateDefaultLoggingConfig();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AppBase] Failed to load logging config: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[AppBase] Exception type: {ex.GetType().Name}");
                        System.Diagnostics.Debug.WriteLine($"[AppBase] Stack trace: {ex.StackTrace}");
                        System.Diagnostics.Debug.WriteLine("[AppBase] Using default configuration as fallback");
                        loggingConfig = CreateDefaultLoggingConfig();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AppBase] Using cached logging config (last loaded: {lastConfigLoad})");
                }
                
                return loggingConfig;
            }
        }

        /// <summary>
        /// Creates default logging configuration as fallback
        /// </summary>
        private static LoggingConfig CreateDefaultLoggingConfig()
        {
            return new LoggingConfig
            {
                LogLevelPatterns = new Dictionary<string, LogLevelConfig>
                {
                    ["ERROR"] = new LogLevelConfig
                    {
                        Patterns = new List<string>
                        {
                            @"\b(error|exception|fail|crash|abort|fatal|critical)\b",
                            @"\[ERROR\]|\[ERR\]|\[FATAL\]"
                        },
                        Keywords = new List<string> { "error", "exception", "fail", "crash" }
                    },
                    ["WARN"] = new LogLevelConfig
                    {
                        Patterns = new List<string> { @"\b(warn|warning|caution)\b" },
                        Keywords = new List<string> { "warn", "warning", "caution" }
                    },
                    ["DEBUG"] = new LogLevelConfig
                    {
                        Patterns = new List<string> { @"\[DEBUG\]|\[DBG\]" },
                        Keywords = new List<string> { "debug", "trace", "verbose" }
                    },
                    ["WEBSOCKET"] = new LogLevelConfig
                    {
                        Patterns = new List<string> { @"\b(websocket|ws:|wss:)\b" },
                        Keywords = new List<string> { "websocket", "ws:", "wss:" }
                    },
                    ["INFO"] = new LogLevelConfig
                    {
                        Patterns = new List<string> { @"\[INFO\]|\[INFORMATION\]" },
                        Keywords = new List<string> { "started", "starting", "ready" }
                    }
                }
            };
        }

        /// <summary>
        /// Ensures the correct log file path for the current day and creates directories if needed
        /// </summary>
        private void EnsureLogFile()
        {
            lock (logFileLock)
            {
                var today = DateTime.Now.Day;
                
                // Check if we need to create a new log file (new day or first time)
                if (currentLogDay != today || string.IsNullOrEmpty(currentLogFilePath))
                {
                    var logsDir = Path.Combine(Environment.CurrentDirectory, "logs", SanitizeFileName(CustomName));
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(logsDir))
                    {
                        Directory.CreateDirectory(logsDir);
                    }
                    
                    // Create daily log file name: DD_AppName.log
                    var fileName = $"{today:D2}_{SanitizeFileName(CustomName)}.log";
                    currentLogFilePath = Path.Combine(logsDir, fileName);
                    currentLogDay = today;
                    
                    // Check if log file exists and is from previous month
                    if (File.Exists(currentLogFilePath))
                    {
                        try
                        {
                            var lastWriteTime = File.GetLastWriteTime(currentLogFilePath);
                            var now = DateTime.Now;
                            
                            // If file was last modified in a different month or year, delete it
                            if (lastWriteTime.Year != now.Year || lastWriteTime.Month != now.Month)
                            {
                                File.Delete(currentLogFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{CustomName}] Failed to rotate log file: {ex.Message}");
                        }
                    }
                    
                    // Log application start to file (if restarted on same day, append)
                    try
                    {
                        var startMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [SYSTEM] === {CustomName} started/restarted ==={Environment.NewLine}";
                        File.AppendAllText(currentLogFilePath, startMessage);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{CustomName}] Failed to write to log file: {ex.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Analyzes message content to determine the appropriate log level using configurable patterns
        /// </summary>
        private string DetermineLogLevel(string message, bool isFromErrorStream)
        {
            var config = LoadLoggingConfig();
            
            if (string.IsNullOrWhiteSpace(message))
            {
                return config.FallbackRules.EmptyMessageDefault;
            }
            
            var messageToCheck = config.Settings.CaseSensitive ? message : message.ToLowerInvariant();
            
            // **IMPORTANT: Check DEBUG first to catch JSON and detailed output**
            // Then check ERROR, WEBSOCKET, INFO, and finally WARN as catch-all
            foreach (var level in new[] { "DEBUG", "ERROR", "WEBSOCKET", "INFO", "WARN" })
            {
                if (!config.LogLevelPatterns.TryGetValue(level, out var levelConfig))
                    continue;

                // Check regex patterns if enabled
                if (config.Settings.UseRegexPatterns && levelConfig.Patterns?.Any() == true)
                {
                    foreach (var pattern in levelConfig.Patterns)
                    {
                        try
                        {
                            var regexOptions = config.Settings.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                            var timeout = TimeSpan.FromMilliseconds(config.Settings.PatternTimeout);
                            
                            if (Regex.IsMatch(messageToCheck, pattern, regexOptions, timeout))
                            {
                                return level;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{CustomName}] Regex error in pattern '{pattern}': {ex.Message}");
                        }
                    }
                }

                // Check simple keyword matching if enabled
                if (config.Settings.UseKeywordMatching && levelConfig.Keywords?.Any() == true)
                {
                    foreach (var keyword in levelConfig.Keywords)
                    {
                        var keywordToCheck = config.Settings.CaseSensitive ? keyword : keyword.ToLowerInvariant();
                        if (messageToCheck.Contains(keywordToCheck))
                        {
                            return level;
                        }
                    }
                }
            }
            
            // Fallback logic based on stream source
            if (isFromErrorStream)
            {
                return config.FallbackRules.ErrorStreamDefault;
            }
            
            // Default for stdout
            return config.FallbackRules.StandardStreamDefault;
        }
        
        /// <summary>
        /// Writes a console output line to the daily log file with timestamp and smart log level detection
        /// </summary>
        private void WriteToLogFile(string message, bool isFromErrorStream = false, string? forceLogLevel = null)
        {
            if (string.IsNullOrEmpty(message)) return;
            
            try
            {
                EnsureLogFile();
                
                if (!string.IsNullOrEmpty(currentLogFilePath))
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLevel = forceLogLevel ?? DetermineLogLevel(message, isFromErrorStream);
                    
                    // Mask sensitive information before writing to log
                    var sanitizedMessage = MaskSensitiveInformation(message);
                    
                    var logLine = $"[{timestamp}] [{logLevel}] {sanitizedMessage}{Environment.NewLine}";
                    
                    lock (logFileLock)
                    {
                        File.AppendAllText(currentLogFilePath, logLine);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{CustomName}] Failed to write to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Masks sensitive information like passwords in log messages
        /// </summary>
        private string MaskSensitiveInformation(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            try
            {
                // If this app has a configuration with password arguments, mask them
                if (Configuration?.Arguments != null)
                {
                    var passwordArguments = Configuration.Arguments
                        .Where(arg => IsPasswordArgument(arg))
                        .ToList();

                    var maskedMessage = message;

                    foreach (var passwordArg in passwordArguments)
                    {
                        if (!string.IsNullOrEmpty(passwordArg.Value))
                        {
                            var actualValue = passwordArg.MappedValue() ?? passwordArg.Value;
                            
                            // Create patterns to find the password value in the message
                            var patterns = new[]
                            {
                                // Command line argument patterns
                                $"{Configuration.Prefix}{passwordArg.Name}{Configuration.Delimitter}\"{actualValue}\"",
                                $"{Configuration.Prefix}{passwordArg.Name}{Configuration.Delimitter}{actualValue}",
                                $"{Configuration.Prefix}{passwordArg.Name}=\"{actualValue}\"", 
                                $"{Configuration.Prefix}{passwordArg.Name}={actualValue}",
                                // Direct value patterns
                                $"\"{actualValue}\"",
                                actualValue
                            };

                            foreach (var pattern in patterns)
                            {
                                if (maskedMessage.Contains(pattern))
                                {
                                    var maskedValue = GetMaskedPassword(actualValue);
                                    var replacement = pattern.Replace(actualValue, maskedValue);
                                    maskedMessage = maskedMessage.Replace(pattern, replacement);
                                }
                            }
                        }
                    }

                    return maskedMessage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{CustomName}] Error masking sensitive information: {ex.Message}");
            }

            return message;
        }

        /// <summary>
        /// Determines if an argument contains password information
        /// </summary>
        private bool IsPasswordArgument(Argument argument)
        {
            if (argument.Type.ToLower().Contains("password"))
                return true;
                
            var argName = argument.Name?.ToLower() ?? "";
            var argHuman = argument.NameHuman?.ToLower() ?? "";
            
            // Check for common password argument patterns
            var passwordIndicators = new[]
            {
                "password", "pass", "pwd", "secret", "key", "token", "auth",
                "autodarts_password", "lidarts_password", "dartboards_password"
            };
            
            return passwordIndicators.Any(indicator => 
                argName.Contains(indicator) || argHuman.Contains(indicator));
        }

        /// <summary>
        /// Creates a masked version of a password
        /// </summary>
        private string GetMaskedPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "";
                
            // For very short passwords, mask completely
            if (password.Length <= 3)
                return "***";
                
            // For longer passwords, show first character and mask the rest
            return password[0] + new string('*', Math.Min(password.Length - 1, 8));
        }
        
        /// <summary>
        /// Sanitizes filename to remove invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }
            return fileName;
        }

        public bool Run(Dictionary<string, string>? runtimeArguments = null)
        {
            this.runtimeArguments = runtimeArguments;
            executable = SetRunExecutable();
            if (IsRunning()) return true;
            if (Install()) return false;
            RunProcess(runtimeArguments);
            return true;
        }

        public bool ReRun(Dictionary<string, string>? runtimeArguments = null)
        {
            if (!IsRunning()) return false;
            Close();
            return Run(runtimeArguments);
        }

        public bool IsInstalled()
        {
            executable = SetRunExecutable();
            return String.IsNullOrEmpty(executable) ? false : File.Exists(executable);
        }

        public bool IsRunning()
        {
            return AppRunningState;
            //return process != null && !process.HasExited;
        }

        public bool IsConfigurationChanged()
        {
            return Configuration != null ? Configuration.IsChanged() : false;
        }

        public void Close()
        {
            executable = SetRunExecutable();

            //if(!IsRunning()) return;
            if (IsRunnable())
            {
                try
                {
                    //Console.WriteLine(Name + " tries to exit");
                    
                    // Log application stop
                    WriteToLogFile($"=== {CustomName} stopping ===", false, "SYSTEM");
                    
                    if(process != null)
                    {
                        process.CloseMainWindow();
                        process.Close();
                    }
                    if (processId != defaultProcessId)
                    {
                        Helper.KillProcess(processId);
                    }
                    if (executable != null)
                    {
                        Helper.KillProcess(executable);
                    }
                    AppRunningState = false;
                    
                    // Final log entry
                    WriteToLogFile($"=== {CustomName} stopped ===", false, "SYSTEM");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Can't close {executable}: {ex.Message}");
                    WriteToLogFile($"Error closing application: {ex.Message}", true, "ERROR");
                }
            }
        }

        public abstract bool Install();

        public abstract bool IsConfigurable();

        public abstract bool IsInstallable();


        private async Task RunProcess(Dictionary<string, string>? runtimeArguments = null)
        {
            if (!IsRunnable()) return;

            if (String.IsNullOrEmpty(executable)) return;
            var arguments = ComposeArguments(this, runtimeArguments);
            //if (arguments == null) return;

            eventHandled = new TaskCompletionSource<bool>();

            try
            {
                // Only reset console output when starting a new process (restart)
                // Do NOT reset during continuous execution
                AppConsoleStdOutput = String.Empty;
                AppConsoleStdError = String.Empty;
                AppMonitor = String.Empty;
                AppMonitorEntries = 0; // Reset counter only on application start/restart

                // Initialize logging for this session
                EnsureLogFile();
                WriteToLogFile($"Starting process: {executable}", false, "SYSTEM");
                if (!string.IsNullOrEmpty(arguments))
                {
                    WriteToLogFile($"Arguments: {arguments}", false, "SYSTEM");
                }

                // For testing purposes
                //AppConsoleStdOutput = arguments;

                bool isUri = Uri.TryCreate(executable, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                process = new Process();
                process.StartInfo.WindowStyle = StartWindowState;
                process.EnableRaisingEvents = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Exited += (sender, e) =>
                {
                    //Console.WriteLine(
                    //    $"Exit time    : {process.ExitTime}\n" +
                    //    $"Exit code    : {process.ExitCode}\n" +
                    //    $"Elapsed time : {Math.Round((process.ExitTime - process.StartTime).TotalMilliseconds)}");
                    //Console.WriteLine("Process " + Name + " exited");
                    
                    // Log process exit with appropriate level based on exit code
                    var exitLogLevel = process.ExitCode == 0 ? "SYSTEM" : "ERROR";
                    WriteToLogFile($"Process exited with code: {process.ExitCode}", false, exitLogLevel);
                    
                    processId = defaultProcessId;
                    if (!isUri) {
                        AppRunningState = false;
                    }
                    eventHandled.TrySetResult(true);
                };
                process.OutputDataReceived += (sender, e) =>
                {   
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        // Write to daily log file with smart log level detection
                        WriteToLogFile(e.Data, false);
                        
                        // Instead of hard reset, trim old lines to prevent memory issues
                        // but keep output continuous during application execution
                        if (AppMonitorEntries >= MaxAppMonitorEntries * 2) // Allow double the limit before trimming
                        {
                            // Trim to keep most recent MaxAppMonitorEntries lines
                            var outputLines = AppConsoleStdOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            var errorLines = AppConsoleStdError.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            
                            if (outputLines.Length > MaxAppMonitorEntries)
                            {
                                AppConsoleStdOutput = string.Join(Environment.NewLine, outputLines.Skip(outputLines.Length - MaxAppMonitorEntries));
                            }
                            if (errorLines.Length > MaxAppMonitorEntries)
                            {
                                AppConsoleStdError = string.Join(Environment.NewLine, errorLines.Skip(errorLines.Length - MaxAppMonitorEntries));
                            }
                            
                            AppMonitorEntries = MaxAppMonitorEntries; // Reset to manageable number
                        }
                        
                        AppConsoleStdOutput += e.Data + Environment.NewLine;
                        AppMonitor = AppConsoleStdOutput + Environment.NewLine + Environment.NewLine + AppConsoleStdError;
                        AppMonitorEntries++;
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        // Write to daily log file with smart log level detection (from error stream)
                        WriteToLogFile(e.Data, true);
                        
                        // Instead of hard reset, trim old lines to prevent memory issues
                        // but keep output continuous during application.execution
                        if (AppMonitorEntries >= MaxAppMonitorEntries * 2) // Allow double the limit before trimming
                        {
                            // Trim to keep most recent MaxAppMonitorEntries lines
                            var outputLines = AppConsoleStdOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            var errorLines = AppConsoleStdError.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                            
                            if (outputLines.Length > MaxAppMonitorEntries)
                            {
                                AppConsoleStdOutput = string.Join(Environment.NewLine, outputLines.Skip(outputLines.Length - MaxAppMonitorEntries));
                            }
                            if (errorLines.Length > MaxAppMonitorEntries)
                            {
                                AppConsoleStdError = string.Join(Environment.NewLine, errorLines.Skip(errorLines.Length - MaxAppMonitorEntries));
                            }
                            
                            AppMonitorEntries = MaxAppMonitorEntries; // Reset to manageable number
                        }
                        
                        AppConsoleStdError += e.Data + Environment.NewLine;
                        AppMonitor = AppConsoleStdOutput + Environment.NewLine + Environment.NewLine + AppConsoleStdError;
                        AppMonitorEntries++;
                    }
                };
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments == null ? String.Empty : arguments;

                

                if (isUri)
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                }
                else 
                {
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executable);
                }
                

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (RunAsAdmin) process.StartInfo.Verb = "runas";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (!isUri && Chmod) EnsureExecutablePermissions(executable);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (!isUri && Chmod) EnsureExecutablePermissions(executable);
                }

                process.Start();
                WriteToLogFile($"Process started with PID: {process.Id}", false, "SYSTEM");
                
                if(process.StartInfo.RedirectStandardOutput == true)
                {
                    process.BeginOutputReadLine();
                }
                if (process.StartInfo.RedirectStandardError == true)
                {
                    process.BeginErrorReadLine();
                }
                processId = process.Id;
                AppRunningState = true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"An error occurred trying to start \"{executable}\":\n{ex.Message}";
                Console.WriteLine(errorMessage);
                WriteToLogFile(errorMessage, true, "ERROR");
                throw;
            }

            // Wait for Exited event
            await Task.WhenAny(eventHandled.Task);
            
        }

        private void EnsureExecutablePermissions(string scriptPath)
        {
            var chmodProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{scriptPath}\"",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false
                }
            };
            chmodProcess.Start();
            chmodProcess.WaitForExit();

            if (chmodProcess.ExitCode != 0)
            {
                throw new Exception($"Failed to set executable permissions for {scriptPath}. Exit code: {chmodProcess.ExitCode}");
            }
        
        }

        protected virtual bool IsRunnable()
        {
            return true;
        }

        protected abstract string? SetRunExecutable();

        protected string? ComposeArguments(AppBase app, Dictionary<string, string>? runtimeArguments = null)
        {
            try
            {
                var ret = IsConfigurable() ? Configuration.GenerateArgumentString(app, runtimeArguments) : "";
                ArgumentRequired = null;
                return ret;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith(Configuration.ArgumentErrorKey))
                {
                    string invalidArgumentErrorMessage = ex.Message.Substring(Configuration.ArgumentErrorKey.Length, ex.Message.Length - Configuration.ArgumentErrorKey.Length);
                    ArgumentRequired = (Argument)ex.Data["argument"];
                    invalidArgumentErrorMessage += " - Please correct it.";
                    OnAppConfigurationRequired(new AppEventArgs(this, invalidArgumentErrorMessage));
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }
   
        protected virtual void OnAppConfigurationRequired(AppEventArgs e)
        {
            AppConfigurationRequired?.Invoke(this, e);
        }



        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
