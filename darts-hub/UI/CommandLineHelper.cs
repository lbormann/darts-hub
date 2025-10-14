using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using darts_hub.control;
using darts_hub.testing;

namespace darts_hub.UI
{
    /// <summary>
    /// Helper class for processing command line arguments and providing console output
    /// </summary>
    public static class CommandLineHelper
    {
        private const string APP_NAME = "DartsHub";
        
        // Use System.Console for better PowerShell compatibility
        private static void WriteLine(string text = "") 
        {
            System.Console.WriteLine(text);
            System.Console.Out.Flush(); // Ensure immediate output in PowerShell
        }
        
        private static void Write(string text) 
        {
            System.Console.Write(text);
            System.Console.Out.Flush(); // Ensure immediate output in PowerShell
        }
        
        /// <summary>
        /// Processes command line arguments and executes corresponding actions
        /// Returns true if a command was processed, false if GUI should be started
        /// </summary>
        public static async Task<bool> ProcessCommandLineArgs(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return false; // No args, start GUI
            }

            // Show banner for any command line usage
            ShowBanner();

            var command = args[0].ToLower();
            
            switch (command)
            {
                // Help commands
                case "--help":
                case "-h":
                case "help":
                    ShowHelp();
                    return true;

                // Version commands
                case "--version":
                case "-v":
                case "version":
                    ShowVersion();
                    return true;

                // Updater test commands
                case "--test-updater":
                case "--updater-test":
                    await RunUpdaterTest(args.Skip(1).ToArray());
                    return true;

                case "--test-full":
                    await RunFullUpdaterTest();
                    return true;

                case "--test-version":
                    await RunVersionTest();
                    return true;

                case "--test-retry":
                    await RunRetryTest();
                    return true;

                case "--test-logging":
                    await RunLoggingTest();
                    return true;

                // Backup commands
                case "--backup":
                case "--backup-full":
                    await RunFullBackup(args.Skip(1).ToArray());
                    return true;

                case "--backup-config":
                    await RunConfigBackup(args.Skip(1).ToArray());
                    return true;

                case "--backup-list":
                case "--list-backups":
                    ListBackups();
                    return true;

                case "--backup-restore":
                case "--restore":
                    await RunRestoreBackup(args.Skip(1).ToArray());
                    return true;

                case "--backup-cleanup":
                    await RunBackupCleanup(args.Skip(1).ToArray());
                    return true;

                // Application information
                case "--info":
                case "info":
                    ShowApplicationInfo();
                    return true;

                // List profiles (if available)
                case "--list-profiles":
                case "--profiles":
                    await ListProfiles();
                    return true;

                // System information
                case "--system-info":
                case "--sysinfo":
                    ShowSystemInfo();
                    return true;

                // Verbose/Debug mode
                case "--verbose":
                case "-vv":
                    EnableVerboseMode();
                    return false; // Continue with GUI but with verbose logging

                // Beta tester mode
                case "--beta":
                    EnableBetaMode();
                    return false; // Continue with GUI but in beta mode

                // Unknown command
                default:
                    if (command.StartsWith("-"))
                    {
                        ShowUnknownCommand(command);
                        return true;
                    }
                    return false; // Not a command, start GUI
            }
        }

        #region Help and Information Commands

        private static void ShowHelp()
        {
            WriteLine($"{APP_NAME} - Dart Game Application Manager");
            WriteLine($"Version: {Updater.version}");
            WriteLine();
            WriteLine("USAGE:");
            WriteLine($"  {APP_NAME.ToLower()} [COMMAND] [OPTIONS]");
            WriteLine();
            WriteLine("COMMANDS:");
            WriteLine("  General:");
            WriteLine("    -h, --help              Show this help message");
            WriteLine("    -v, --version           Show version information");
            WriteLine("    --info                  Show detailed application information");
            WriteLine("    --system-info           Show system and environment information");
            WriteLine();
            WriteLine("  Profile Management:");
            WriteLine("    --list-profiles         List all available dart profiles");
            WriteLine("    --profiles              Alias for --list-profiles");
            WriteLine();
            WriteLine("  Backup & Restore:");
            WriteLine("    --backup [name]         Create full backup (configs, profiles, logs)");
            WriteLine("    --backup-config [name]  Create configuration-only backup");
            WriteLine("    --backup-list           List all available backups");
            WriteLine("    --backup-restore <file> Restore backup from file");
            WriteLine("    --backup-cleanup [keep] Clean up old backups (default: keep 10)");
            WriteLine();
            WriteLine("  Testing:");
            WriteLine("    --test-updater          Run interactive updater test menu");
            WriteLine("    --test-full             Run complete updater test suite");
            WriteLine("    --test-version          Test version checking functionality");
            WriteLine("    --test-retry            Test retry mechanism");
            WriteLine("    --test-logging          Test logging system");
            WriteLine();
            WriteLine("  Runtime Options:");
            WriteLine("    --verbose, -vv          Enable verbose logging (starts GUI)");
            WriteLine("    --beta                  Enable beta tester mode (starts GUI)");
            WriteLine();
            WriteLine("EXAMPLES:");
            WriteLine($"  {APP_NAME.ToLower()} --help");
            WriteLine($"  {APP_NAME.ToLower()} --test-full");
            WriteLine($"  {APP_NAME.ToLower()} --backup my-backup");
            WriteLine($"  {APP_NAME.ToLower()} --backup-config");
            WriteLine($"  {APP_NAME.ToLower()} --backup-list");
            WriteLine($"  {APP_NAME.ToLower()} --backup-restore backups/my-backup.zip");
            WriteLine($"  {APP_NAME.ToLower()} --list-profiles");
            WriteLine($"  {APP_NAME.ToLower()} --verbose");
            WriteLine();
            WriteLine("For more information, visit: https://github.com/lbormann/darts-hub");
        }

        private static void ShowVersion()
        {
            WriteLine($"{APP_NAME} {Updater.version}");
            WriteLine($"Build Date: {GetBuildDate()}");
            WriteLine($"Runtime: .NET {Environment.Version}");
            WriteLine($"Platform: {Environment.OSVersion}");
            WriteLine($"Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        }

        private static void ShowApplicationInfo()
        {
            WriteLine($"=== {APP_NAME} APPLICATION INFORMATION ===");
            WriteLine();
            WriteLine($"Application Name: {APP_NAME}");
            WriteLine($"Version: {Updater.version}");
            WriteLine($"Build Date: {GetBuildDate()}");
            WriteLine();
            WriteLine("Runtime Information:");
            WriteLine($"  .NET Version: {Environment.Version}");
            WriteLine($"  Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            WriteLine($"  OS: {Environment.OSVersion}");
            WriteLine($"  Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            WriteLine($"  Working Directory: {Environment.CurrentDirectory}");
            WriteLine($"  Application Path: {Helper.GetAppBasePath()}");
            WriteLine();
            WriteLine("Features:");
            WriteLine("  ? Multi-platform support (Windows, macOS, Linux)");
            WriteLine("  ? Automatic updates");
            WriteLine("  ? Profile management");
            WriteLine("  ? Wizard-guided setup");
            WriteLine("  ? Console logging and testing");
            WriteLine("  ? Backup and restore functionality");
            WriteLine();
            WriteLine("Supported Dart Applications:");
            WriteLine("  • darts-caller (Voice announcements)");
            WriteLine("  • darts-wled (LED effects)");
            WriteLine("  • darts-pixelit (Pixel displays)");
            WriteLine("  • darts-gif (GIF animations)");
            WriteLine("  • darts-voice (Voice recognition)");
        }

        private static void ShowSystemInfo()
        {
            WriteLine("=== SYSTEM INFORMATION ===");
            WriteLine();
            WriteLine("Environment:");
            WriteLine($"  OS: {Environment.OSVersion}");
            WriteLine($"  Machine Name: {Environment.MachineName}");
            WriteLine($"  User: {Environment.UserName}");
            WriteLine($"  Processor Count: {Environment.ProcessorCount}");
            WriteLine($"  System Directory: {Environment.SystemDirectory}");
            WriteLine($"  Current Directory: {Environment.CurrentDirectory}");
            WriteLine();
            WriteLine("Runtime:");
            WriteLine($"  .NET Version: {Environment.Version}");
            WriteLine($"  Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            WriteLine($"  Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            WriteLine($"  OS Architecture: {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}");
            WriteLine();
            WriteLine("Memory:");
            WriteLine($"  Working Set: {Environment.WorkingSet / 1024 / 1024} MB");
            WriteLine();
            WriteLine("Application:");
            WriteLine($"  App Base Path: {Helper.GetAppBasePath()}");
            WriteLine($"  User Directory: {Helper.GetUserDirectoryPath()}");
        }

        private static void ShowUnknownCommand(string command)
        {
            WriteLine($"Unknown command: {command}");
            WriteLine();
            WriteLine($"Use '{APP_NAME.ToLower()} --help' to see available commands.");
        }

        #endregion

        #region Backup Commands

        private static async Task RunFullBackup(string[] args)
        {
            try
            {
                WriteLine("?? Creating full backup...");
                WriteLine();
                
                string customName = null;
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    customName = args[0];
                }
                
                var backupPath = await BackupHelper.CreateFullBackup(customName);
                WriteLine();
                WriteLine($"?? Full backup completed successfully!");
                WriteLine($"   Backup location: {backupPath}");
            }
            catch (Exception ex)
            {
                WriteLine($"? Backup failed: {ex.Message}");
            }
        }

        private static async Task RunConfigBackup(string[] args)
        {
            try
            {
                WriteLine("?? Creating configuration backup...");
                WriteLine();
                
                string customName = null;
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    customName = args[0];
                }
                
                var backupPath = await BackupHelper.CreateConfigBackup(customName);
                WriteLine();
                WriteLine($"?? Configuration backup completed successfully!");
                WriteLine($"   Backup location: {backupPath}");
            }
            catch (Exception ex)
            {
                WriteLine($"? Configuration backup failed: {ex.Message}");
            }
        }

        private static void ListBackups()
        {
            try
            {
                BackupHelper.ListBackups();
            }
            catch (Exception ex)
            {
                WriteLine($"? Failed to list backups: {ex.Message}");
            }
        }

        private static async Task RunRestoreBackup(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    WriteLine("? Please specify a backup file to restore.");
                    WriteLine("Usage: --backup-restore <path-to-backup-file>");
                    WriteLine();
                    WriteLine("Available backups:");
                    BackupHelper.ListBackups();
                    return;
                }
                
                var backupFile = args[0];
                
                // If it's just a filename, try to find it in the backups directory
                if (!Path.IsPathRooted(backupFile) && !backupFile.Contains(Path.DirectorySeparatorChar))
                {
                    var backupDir = Path.Combine(Helper.GetAppBasePath(), "backups");
                    var possiblePath = Path.Combine(backupDir, backupFile);
                    
                    if (File.Exists(possiblePath))
                    {
                        backupFile = possiblePath;
                    }
                    else if (!backupFile.EndsWith(".zip"))
                    {
                        possiblePath = Path.Combine(backupDir, backupFile + ".zip");
                        if (File.Exists(possiblePath))
                        {
                            backupFile = possiblePath;
                        }
                    }
                }
                
                WriteLine("?? Restoring backup...");
                WriteLine();
                
                var success = await BackupHelper.RestoreBackup(backupFile, interactive: true);
                
                if (success)
                {
                    WriteLine();
                    WriteLine("?? Backup restored successfully!");
                    WriteLine("   Please restart DartsHub to apply the restored configuration.");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"? Restore failed: {ex.Message}");
            }
        }

        private static async Task RunBackupCleanup(string[] args)
        {
            try
            {
                var keepCount = 10; // Default
                
                if (args.Length > 0 && int.TryParse(args[0], out var customKeepCount))
                {
                    keepCount = Math.Max(1, customKeepCount); // At least 1
                }
                
                WriteLine($"?? Cleaning up old backups (keeping {keepCount} most recent)...");
                WriteLine();
                
                BackupHelper.CleanupOldBackups(keepCount);
            }
            catch (Exception ex)
            {
                WriteLine($"? Cleanup failed: {ex.Message}");
            }
        }

        #endregion

        #region Test Commands

        private static async Task RunUpdaterTest(string[] additionalArgs)
        {
            WriteLine("=== DARTS-HUB UPDATER TEST SUITE ===");
            WriteLine();
            
            // Subscribe to test events for real-time output
            UpdaterTester.TestStatusChanged += (sender, status) =>
            {
                WriteLine($"[{DateTime.Now:HH:mm:ss}] {status}");
            };
            
            UpdaterTester.TestCompleted += (sender, results) =>
            {
                WriteLine();
                WriteLine("=== TEST RESULTS ===");
                WriteLine(results);
                WriteLine("=== TEST COMPLETE ===");
            };

            if (additionalArgs.Length == 0)
            {
                await ShowTestMenu();
            }
            else
            {
                await ProcessTestArguments(additionalArgs);
            }
        }

        private static async Task ShowTestMenu()
        {
            while (true)
            {
                WriteLine();
                WriteLine("Available Tests:");
                WriteLine("1. Full Test Suite (all components)");
                WriteLine("2. Version Check Test");
                WriteLine("3. Retry Mechanism Test");
                WriteLine("4. Logging System Test");
                WriteLine("5. Exit");
                WriteLine();
                Write("Select option (1-5): ");

                var input = System.Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        await RunFullUpdaterTest();
                        break;
                    case "2":
                        await RunVersionTest();
                        break;
                    case "3":
                        await RunRetryTest();
                        break;
                    case "4":
                        await RunLoggingTest();
                        break;
                    case "5":
                        WriteLine("Goodbye!");
                        return;
                    default:
                        WriteLine("Invalid selection. Please try again.");
                        break;
                }
            }
        }

        private static async Task ProcessTestArguments(string[] args)
        {
            var testType = args[0].ToLower();
            
            switch (testType)
            {
                case "full":
                    await RunFullUpdaterTest();
                    break;
                case "version":
                    await RunVersionTest();
                    break;
                case "retry":
                    await RunRetryTest();
                    break;
                case "logging":
                    await RunLoggingTest();
                    break;
                default:
                    WriteLine($"Unknown test type: {testType}");
                    WriteLine("Available test types: full, version, retry, logging");
                    break;
            }
        }

        private static async Task RunFullUpdaterTest()
        {
            WriteLine();
            WriteLine("?? Starting comprehensive updater test...");
            WriteLine("This may take several minutes...");
            
            try
            {
                await UpdaterTester.RunFullUpdateTest();
            }
            catch (Exception ex)
            {
                WriteLine($"? Test failed: {ex.Message}");
            }
        }

        private static async Task RunVersionTest()
        {
            WriteLine();
            WriteLine("?? Starting version check test...");
            
            try
            {
                await UpdaterTester.TestVersionCheckOnly();
            }
            catch (Exception ex)
            {
                WriteLine($"? Test failed: {ex.Message}");
            }
        }

        private static async Task RunRetryTest()
        {
            WriteLine();
            WriteLine("?? Starting retry mechanism test...");
            
            try
            {
                await UpdaterTester.TestRetryMechanismOnly();
            }
            catch (Exception ex)
            {
                WriteLine($"? Test failed: {ex.Message}");
            }
        }

        private static async Task RunLoggingTest()
        {
            WriteLine();
            WriteLine("?? Starting logging system test...");
            
            try
            {
                UpdaterLogger.LogInfo("CLI Test - INFO Level");
                UpdaterLogger.LogWarning("CLI Test - WARNING Level");
                UpdaterLogger.LogError("CLI Test - ERROR Level", new Exception("Test exception"));
                UpdaterLogger.LogDebug("CLI Test - DEBUG Level");
                
                WriteLine("? Logging test completed. Check log files in logs/ directory.");
            }
            catch (Exception ex)
            {
                WriteLine($"? Logging test failed: {ex.Message}");
            }
        }

        #endregion

        #region Profile Management

        private static async Task ListProfiles()
        {
            WriteLine("=== DART PROFILES ===");
            WriteLine();
            
            try
            {
                var profileManager = new ProfileManager();
                var profiles = profileManager.GetProfiles();
                
                if (profiles.Count == 0)
                {
                    WriteLine("No profiles found.");
                    WriteLine("Run the application normally to create profiles through the setup wizard.");
                    return;
                }

                WriteLine($"Found {profiles.Count} profile(s):");
                WriteLine();

                for (int i = 0; i < profiles.Count; i++)
                {
                    var profile = profiles[i];
                    WriteLine($"{i + 1}. {profile.Name}");
                    WriteLine($"   Tagged for Start: {(profile.IsTaggedForStart ? "Yes" : "No")}");
                    WriteLine($"   Applications: {profile.Apps.Count}");
                    
                    if (profile.Apps.Count > 0)
                    {
                        foreach (var app in profile.Apps.Values)
                        {
                            var status = app.TaggedForStart ? "Auto-start" : "Manual";
                            WriteLine($"     • {app.App.CustomName} ({status})");
                        }
                    }
                    
                    if (i < profiles.Count - 1)
                        WriteLine();
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Error listing profiles: {ex.Message}");
                WriteLine("Make sure the application has been run at least once to initialize profiles.");
            }
        }

        #endregion

        #region Runtime Options

        private static void EnableVerboseMode()
        {
            WriteLine("?? Verbose mode enabled.");
            WriteLine("The application will start with detailed logging enabled.");
            WriteLine();
            
            // Set environment variable or flag for verbose mode
            Environment.SetEnvironmentVariable("DARTSHUB_VERBOSE", "true");
            
            // Enable debug logging
            UpdaterLogger.LogInfo("Verbose mode enabled via command line");
        }

        private static void EnableBetaMode()
        {
            WriteLine("?? Beta tester mode enabled.");
            WriteLine("The application will check for beta releases and enable experimental features.");
            WriteLine();
            
            // Set beta mode flag
            Updater.IsBetaTester = true;
            
            UpdaterLogger.LogInfo("Beta tester mode enabled via command line");
        }

        #endregion

        #region Utility Methods

        private static string GetBuildDate()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var creationTime = System.IO.File.GetCreationTime(assembly.Location);
                return creationTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Shows a formatted banner for command line operations
        /// </summary>
        public static void ShowBanner()
        {
            // Use ASCII characters that work reliably in PowerShell
            WriteLine("================================================");
            WriteLine($"                    {APP_NAME}");
            WriteLine($"                 {Updater.version} - CLI Mode");
            WriteLine("================================================");
            WriteLine();
        }

        /// <summary>
        /// Checks if verbose mode is enabled
        /// </summary>
        public static bool IsVerboseMode()
        {
            return Environment.GetEnvironmentVariable("DARTSHUB_VERBOSE") == "true";
        }

        /// <summary>
        /// Prints a message with timestamp if verbose mode is enabled
        /// </summary>
        public static void VerboseLog(string message)
        {
            if (IsVerboseMode())
            {
                WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }
        }

        #endregion
    }
}