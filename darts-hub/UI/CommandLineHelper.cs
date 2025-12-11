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
        private const string APP_NAME = "Darts-Hub";
        
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
        /// Reads a line from console with proper handling for redirected streams
        /// </summary>
        private static string ReadLine()
        {
            try
            {
                System.Console.Out.Flush();
                
                // Try to read from standard input
                var input = System.Console.In.ReadLine();
                
                // If we got null or empty, might be redirected
                if (input == null)
                {
                    return string.Empty;
                }
                
                return input;
            }
            catch (Exception)
            {
                // Fallback if input is redirected or unavailable
                return string.Empty;
            }
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

                // Config Export/Import commands
                case "--export":
                case "--export-full":
                    await RunConfigExportFull(args.Skip(1).ToArray());
                    return true;

                case "--export-extension":
                case "--export-ext":
                    await RunConfigExportExtension(args.Skip(1).ToArray());
                    return true;

                case "--export-params":
                case "--export-parameters":
                    await RunConfigExportParameters(args.Skip(1).ToArray());
                    return true;

                case "--import":
                    await RunConfigImport(args.Skip(1).ToArray());
                    return true;

                case "--list-extensions":
                case "--extensions":
                    await ListExtensions();
                    return true;

                case "--list-params":
                case "--params":
                    await ListExtensionParams(args.Skip(1).ToArray());
                    return true;

                case "--list-exports":
                case "--exports":
                    ListExports();
                    return true;

                case "--export-info":
                case "--info-export":
                    await ShowExportInfo(args.Skip(1).ToArray());
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
            WriteLine("  Configuration Export/Import:");
            WriteLine("    --export [name]         Export complete configuration (all extensions)");
            WriteLine("    --export-ext <names...> Export specific extension(s)");
            WriteLine("    --export-params         Export specific parameters (interactive)");
            WriteLine("    --import <file> [mode]  Import configuration (mode: merge|replace, default: merge)");
            WriteLine("    --list-extensions       List all available extensions");
            WriteLine("    --list-params <ext>     List parameters for an extension");
            WriteLine("    --list-exports          List all export files");
            WriteLine("    --export-info <file>    Show information about an export file");
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
            WriteLine("  Configuration Export:");
            WriteLine($"    darts-hub --export my-config");
            WriteLine($"    darts-hub --export-ext darts-caller darts-wled");
            WriteLine($"    darts-hub --export-params");
            WriteLine($"    darts-hub --list-extensions");
            WriteLine($"    darts-hub --list-params darts-caller");
            WriteLine();
            WriteLine("  Configuration Import:");
            WriteLine($"    darts-hub --import exports/my-config.json");
            WriteLine($"    darts-hub --import exports/my-config.json replace");
            WriteLine($"    darts-hub --export-info exports/my-config.json");
            WriteLine($"    darts-hub --list-exports");
            WriteLine();
            WriteLine("  Other:");
            WriteLine($"    darts-hub --help");
            WriteLine($"    darts-hub --test-full");
            WriteLine($"    darts-hub --backup my-backup");
            WriteLine($"    darts-hub --backup-config");
            WriteLine($"    darts-hub --list-profiles");
            WriteLine($"    darts-hub --verbose");
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
                WriteLine("Creating full backup...");
                WriteLine();
                
                string customName = null;
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    customName = args[0];
                }
                
                var backupPath = await BackupHelper.CreateFullBackup(customName);
                WriteLine();
                WriteLine($"Full backup completed successfully!");
                WriteLine($"   Backup location: {backupPath}");
            }
            catch (Exception ex)
            {
                WriteLine($"Backup failed: {ex.Message}");
            }
        }

        private static async Task RunConfigBackup(string[] args)
        {
            try
            {
                WriteLine("Creating configuration backup...");
                WriteLine();
                
                string customName = null;
                if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
                {
                    customName = args[0];
                }
                
                var backupPath = await BackupHelper.CreateConfigBackup(customName);
                WriteLine();
                WriteLine($"Configuration backup completed successfully!");
                WriteLine($"   Backup location: {backupPath}");
            }
            catch (Exception ex)
            {
                WriteLine($"Configuration backup failed: {ex.Message}");
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
                WriteLine($"Failed to list backups: {ex.Message}");
            }
        }

        private static async Task RunRestoreBackup(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    WriteLine("Please specify a backup file to restore.");
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
                
                WriteLine("Restoring backup...");
                WriteLine();
                
                var success = await BackupHelper.RestoreBackup(backupFile, interactive: true);
                
                if (success)
                {
                    WriteLine();
                    WriteLine("Backup restored successfully!");
                    WriteLine("   Please restart DartsHub to apply the restored configuration.");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Restore failed: {ex.Message}");
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
                
                WriteLine($"Cleaning up old backups (keeping {keepCount} most recent)...");
                WriteLine();
                
                BackupHelper.CleanupOldBackups(keepCount);
            }
            catch (Exception ex)
            {
                WriteLine($"Cleanup failed: {ex.Message}");
            }
        }

        #endregion

        #region Config Export/Import Commands

        private static async Task RunConfigExportFull(string[] args)
        {
            try
            {
                WriteLine("=== CONFIG EXPORT - FULL ===");
                WriteLine();
                WriteLine("Exporting complete configuration (all extensions)...");
                WriteLine();

                string customName = null;
                string description = null;

                if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                {
                    customName = args[0];
                }

                if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
                {
                    description = args[1];
                }

                // Check if darts-caller is included and ask about credentials
                var extensions = await ConfigExportManager.ListAvailableExtensions();
                bool excludeCredentials = false;
                
                if (extensions.Any(e => string.Equals(e, "darts-caller", StringComparison.OrdinalIgnoreCase)))
                {
                    WriteLine("? WARNING: Your configuration includes 'darts-caller'");
                    WriteLine("   This extension contains sensitive Autodarts credentials:");
                    WriteLine("   - Email (U)");
                    WriteLine("   - Password (P)");
                    WriteLine("   - Board ID (B)");
                    WriteLine();
                    Write("Do you want to EXCLUDE these credentials from export? (y/N): ");
                    var response = ReadLine();
                    excludeCredentials = response?.ToLower() == "y";
                    WriteLine();
                }

                var exportPath = await ConfigExportManager.ExportFull(customName, description, excludeCredentials);

                WriteLine("? Export successful!");
                WriteLine($"   Export file: {exportPath}");
                WriteLine();
                
                if (excludeCredentials)
				{
					WriteLine("   ? Autodarts credentials (U, P, B) were excluded from export.");
				}
                
                var info = await ConfigExportManager.GetExportInfo(exportPath);
                WriteLine($"   Exported {info.ExtensionNames.Count} extensions:");
                foreach (var extName in info.ExtensionNames)
                {
                    WriteLine($"      • {extName}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"? Export failed: {ex.Message}");
            }
        }

        private static async Task RunConfigExportExtension(string[] args)
        {
            try
            {
                WriteLine("=== CONFIG EXPORT - EXTENSIONS ===");
                WriteLine();

                if (args.Length == 0)
                {
                    WriteLine("Please specify extension name(s) to export.");
                    WriteLine();
                    WriteLine("Usage: --export-ext <extension1> [extension2] [extension3]...");
                    WriteLine();
                    WriteLine("Available extensions:");
                    await ListExtensions();
                    return;
                }

                var extensionNames = args.Where(a => !a.StartsWith("--")).ToList();
                
                if (!extensionNames.Any())
                {
                    WriteLine("No valid extension names provided.");
                    return;
                }

                // Check if darts-caller is included and ask about credentials
                bool excludeCredentials = false;
                bool hasCallerExtension = extensionNames.Any(e => 
                    string.Equals(e, "darts-caller", StringComparison.OrdinalIgnoreCase));
                
                if (hasCallerExtension)
                {
                    WriteLine("? WARNING: You are exporting 'darts-caller'");
                    WriteLine("   This extension contains sensitive Autodarts credentials:");
                    WriteLine("   - Email (U)");
                    WriteLine("   - Password (P)");
                    WriteLine("   - Board ID (B)");
                    WriteLine();
                    Write("Do you want to EXCLUDE these credentials from export? (y/N): ");
                    var response = ReadLine();
                    excludeCredentials = response?.ToLower() == "y";
                    WriteLine();
                }

                WriteLine($"Exporting {extensionNames.Count} extension(s)...");
                WriteLine();

                var exportPath = await ConfigExportManager.ExportExtensions(extensionNames, null, null, excludeCredentials);

                WriteLine("? Export successful!");
                WriteLine($"   Export file: {exportPath}");
                WriteLine();
                
                if (excludeCredentials)
                {
                    WriteLine("   ? Autodarts credentials (U, P, B) were excluded from export.");
                }
                
                WriteLine($"   Exported extensions:");
                foreach (var extName in extensionNames)
                {
                    WriteLine($"      • {extName}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"? Export failed: {ex.Message}");
            }
        }

        private static async Task RunConfigExportParameters(string[] args)
        {
            try
            {
                WriteLine("=== CONFIG EXPORT - PARAMETERS ===");
                WriteLine();
                WriteLine("This command exports specific parameters from specific extensions.");
                WriteLine();

                // Get available extensions
                var extensions = await ConfigExportManager.ListAvailableExtensions();
                
                if (!extensions.Any())
                {
                    WriteLine("No extensions found in configuration.");
                    return;
                }

                var extensionParameters = new Dictionary<string, List<string>>();

                // Interactive mode
                WriteLine("Available extensions:");
                for (int i = 0; i < extensions.Count; i++)
                {
                    WriteLine($"  {i + 1}. {extensions[i]}");
                }
                WriteLine();

                Write("Select extensions (comma-separated numbers, or 'all'): ");
                var input = ReadLine();

                List<string> selectedExtensions;
                if (input?.ToLower() == "all")
                {
                    selectedExtensions = extensions;
                }
                else
                {
                    var indices = input?.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => int.TryParse(s, out _))
                        .Select(s => int.Parse(s) - 1)
                        .Where(i => i >= 0 && i < extensions.Count)
                        .ToList() ?? new List<int>();

                    selectedExtensions = indices.Select(i => extensions[i]).ToList();
                }

                if (!selectedExtensions.Any())
                {
                    WriteLine("No valid extensions selected.");
                    return;
                }

                WriteLine();
                WriteLine($"Selected {selectedExtensions.Count} extension(s).");
                WriteLine();

                // Check for darts-caller and ask about credentials BEFORE parameter selection
                bool excludeCredentials = false;
                bool hasCallerExtension = selectedExtensions.Any(e => 
                    string.Equals(e, "darts-caller", StringComparison.OrdinalIgnoreCase));
                
                if (hasCallerExtension)
                {
                    WriteLine("? WARNING: You selected 'darts-caller'");
                    WriteLine("   This extension contains sensitive Autodarts credentials:");
                    WriteLine("   - Email (U)");
                    WriteLine("   - Password (P)");
                    WriteLine("   - Board ID (B)");
                    WriteLine();
                    Write("Do you want to EXCLUDE these credentials from export? (y/N): ");
                    var response = ReadLine();
                    excludeCredentials = response?.ToLower() == "y";
                    WriteLine();
                    
                    if (excludeCredentials)
                    {
                        WriteLine("? Credentials (U, P, B) will be excluded from parameter selection.");
                        WriteLine();
                    }
                }

                // For each selected extension, ask for parameters
                foreach (var extName in selectedExtensions)
                {
                    WriteLine($"--- {extName} ---");
                    
                    var parameters = await ConfigExportManager.ListExtensionParameters(extName);
                    
                    // If this is darts-caller and user wants to exclude credentials, filter them out
                    if (excludeCredentials && string.Equals(extName, "darts-caller", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters = parameters.Where(p => 
                            !string.Equals(p, "U", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p, "P", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p, "B", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        WriteLine("  (Credentials U, P, B excluded from selection)");
                    }
                    
                    if (!parameters.Any())
                    {
                        WriteLine("  No parameters available for this extension.");
                        WriteLine();
                        continue;
                    }

                    WriteLine("  Available parameters:");
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        WriteLine($"    {i + 1}. {parameters[i]}");
                    }
                    WriteLine();

                    Write("  Select parameters (comma-separated numbers, 'all', or 'skip'): ");
                    var paramInput = ReadLine();

                    if (paramInput?.ToLower() == "skip")
                    {
                        continue;
                    }

                    List<string> selectedParams;
                    if (paramInput?.ToLower() == "all")
                    {
                        selectedParams = parameters;
                    }
                    else
                    {
                        var paramIndices = paramInput?.Split(',')
                            .Select(s => s.Trim())
                            .Where(s => int.TryParse(s, out _))
                            .Select(s => int.Parse(s) - 1)
                            .Where(i => i >= 0 && i < parameters.Count)
                            .ToList() ?? new List<int>();

                        selectedParams = paramIndices.Select(i => parameters[i]).ToList();
                    }

                    if (selectedParams.Any())
                    {
                        extensionParameters[extName] = selectedParams;
                        WriteLine($"  ? Selected {selectedParams.Count} parameter(s)");
                    }
                    else
                    {
                        WriteLine("  ? No parameters selected for this extension");
                    }
                    WriteLine();
                }

                if (!extensionParameters.Any())
                {
                    WriteLine("No parameters selected for export.");
                    return;
                }

                WriteLine("Exporting parameters...");
                WriteLine();

                var exportPath = await ConfigExportManager.ExportParameters(extensionParameters);

                WriteLine("? Export successful!");
                WriteLine($"   Export file: {exportPath}");
                WriteLine();
                
                if (excludeCredentials)
                {
                    WriteLine("   ? Autodarts credentials (U, P, B) were excluded from export.");
                }
                
                WriteLine("   Exported:");
                foreach (var kvp in extensionParameters)
                {
                    WriteLine($"      • {kvp.Key}: {kvp.Value.Count} parameter(s)");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"? Export failed: {ex.Message}");
            }
        }

        private static async Task RunConfigImport(string[] args)
        {
            try
            {
                WriteLine("=== CONFIG IMPORT ===");
                WriteLine();

                if (args.Length == 0)
                {
                    WriteLine("Please specify an export file to import.");
                    WriteLine();
                    WriteLine("Usage: --import <export-file> [mode]");
                    WriteLine("       mode: merge (default) or replace");
                    WriteLine();
                    WriteLine("Available export files:");
                    ListExports();
                    return;
                }

                var exportFile = args[0];
                var mode = ImportMode.Merge;

                if (args.Length > 1)
                {
                    if (args[1].ToLower() == "replace")
                    {
                        mode = ImportMode.Replace;
                    }
                }

                // If it's just a filename, try to find it in the exports directory
                if (!Path.IsPathRooted(exportFile) && !exportFile.Contains(Path.DirectorySeparatorChar))
                {
                    var exportsDir = Path.Combine(Helper.GetAppBasePath(), "exports");
                    var possiblePath = Path.Combine(exportsDir, exportFile);

                    if (File.Exists(possiblePath))
                    {
                        exportFile = possiblePath;
                    }
                    else if (!exportFile.EndsWith(".json"))
                    {
                        possiblePath = Path.Combine(exportsDir, exportFile + ".json");
                        if (File.Exists(possiblePath))
                        {
                            exportFile = possiblePath;
                        }
                    }
                }

                // Show export info
                var info = await ConfigExportManager.GetExportInfo(exportFile);
                WriteLine($"Export file: {Path.GetFileName(exportFile)}");
                WriteLine($"Export type: {info.Type}");
                WriteLine($"Created: {info.Timestamp:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"Extensions: {string.Join(", ", info.ExtensionNames)}");
                if (!string.IsNullOrWhiteSpace(info.Description))
                {
                    WriteLine($"Description: {info.Description}");
                }
                WriteLine();
                WriteLine($"Import mode: {mode}");
                WriteLine();

                Write("Proceed with import? (y/N): ");
                var confirm = ReadLine();

                if (confirm?.ToLower() != "y")
                {
                    WriteLine("Import cancelled.");
                    return;
                }

                WriteLine();
                WriteLine("Importing configuration...");
                WriteLine("(A backup will be created automatically)");
                WriteLine();

                var result = await ConfigExportManager.Import(exportFile, mode, createBackup: true);

                if (result.Success)
                {
                    WriteLine("? Import successful!");
                    WriteLine();
                    WriteLine($"   {result.Message}");
                    
                    if (!string.IsNullOrWhiteSpace(result.BackupPath))
                    {
                        WriteLine($"   Backup created: {result.BackupPath}");
                    }
                    
                    if (result.Warnings.Any())
                    {
                        WriteLine();
                        WriteLine("   Warnings:");
                        foreach (var warning in result.Warnings)
                        {
                            WriteLine($"      ? {warning}");
                        }
                    }
                }
                else
                {
                    WriteLine("? Import failed!");
                    WriteLine();
                    foreach (var error in result.Errors)
                    {
                        WriteLine($"   ? {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"? Import failed: {ex.Message}");
            }
        }

        private static async Task ListExtensions()
        {
            try
            {
                var extensions = await ConfigExportManager.ListAvailableExtensions();

                if (!extensions.Any())
                {
                    WriteLine("  No extensions found.");
                    return;
                }

                WriteLine($"  Found {extensions.Count} extension(s):");
                foreach (var ext in extensions)
                {
                    WriteLine($"     • {ext}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to list extensions: {ex.Message}");
            }
        }

        private static async Task ListExtensionParams(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    WriteLine("Please specify an extension name.");
                    WriteLine();
                    WriteLine("Usage: --list-params <extension-name>");
                    WriteLine();
                    WriteLine("Available extensions:");
                    await ListExtensions();
                    return;
                }

                var extensionName = args[0];
                WriteLine($"=== PARAMETERS FOR: {extensionName} ===");
                WriteLine();

                var parameters = await ConfigExportManager.ListExtensionParameters(extensionName);

                if (!parameters.Any())
                {
                    WriteLine("No parameters found for this extension.");
                    return;
                }

                WriteLine($"Found {parameters.Count} parameter(s):");
                foreach (var param in parameters)
                {
                    WriteLine($"   • {param}");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to list parameters: {ex.Message}");
            }
        }

        private static void ListExports()
        {
            try
            {
                var exports = ConfigExportManager.ListExports();

                if (!exports.Any())
                {
                    WriteLine("  No export files found.");
                    return;
                }

                WriteLine($"  Found {exports.Count} export file(s):");
                WriteLine();
                foreach (var export in exports)
                {
                    WriteLine($"     • {export.Name}");
                    WriteLine($"       Created: {export.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    WriteLine($"       Size: {export.Length / 1024.0:F2} KB");
                    WriteLine();
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to list exports: {ex.Message}");
            }
        }

        private static async Task ShowExportInfo(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    WriteLine("Please specify an export file.");
                    WriteLine();
                    WriteLine("Usage: --export-info <export-file>");
                    WriteLine();
                    WriteLine("Available export files:");
                    ListExports();
                    return;
                }

                var exportFile = args[0];

                // If it's just a filename, try to find it in the exports directory
                if (!Path.IsPathRooted(exportFile) && !exportFile.Contains(Path.DirectorySeparatorChar))
                {
                    var exportsDir = Path.Combine(Helper.GetAppBasePath(), "exports");
                    var possiblePath = Path.Combine(exportsDir, exportFile);

                    if (File.Exists(possiblePath))
                    {
                        exportFile = possiblePath;
                    }
                    else if (!exportFile.EndsWith(".json"))
                    {
                        possiblePath = Path.Combine(exportsDir, exportFile + ".json");
                        if (File.Exists(possiblePath))
                        {
                            exportFile = possiblePath;
                        }
                    }
                }

                WriteLine("=== EXPORT FILE INFORMATION ===");
                WriteLine();

                var info = await ConfigExportManager.GetExportInfo(exportFile);

                WriteLine($"File: {Path.GetFileName(exportFile)}");
                WriteLine($"Type: {info.Type}");
                WriteLine($"Version: {info.Version}");
                WriteLine($"Created: {info.Timestamp:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"App Version: {info.AppVersion}");
                WriteLine();

                if (!string.IsNullOrWhiteSpace(info.Description))
                {
                    WriteLine($"Description: {info.Description}");
                    WriteLine();
                }

                WriteLine($"Extensions ({info.ExtensionNames.Count}):");
                foreach (var extName in info.ExtensionNames)
                {
                    WriteLine($"   • {extName}");

                    // Show parameter data if available (new format for parameter exports)
                    if (info.ParameterData != null && info.ParameterData.ContainsKey(extName))
                    {
                        var paramData = info.ParameterData[extName];
                        WriteLine($"     Parameters with values ({paramData.Count}):");
                        foreach (var param in paramData)
                        {
                            WriteLine($"        - {param.NameHuman}");
                            WriteLine($"          Value: {(string.IsNullOrWhiteSpace(param.Value) ? "(empty)" : param.Value)}");
                        }
                    }
                    // Fallback to old format
                    else if (info.ParameterNames != null && info.ParameterNames.ContainsKey(extName))
                    {
                        var paramNames = info.ParameterNames[extName];
                        WriteLine($"     Parameters ({paramNames.Count}):");
                        foreach (var paramName in paramNames)
                        {
                            WriteLine($"        - {paramName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Failed to read export info: {ex.Message}");
            }
        }

        #endregion

        #region Test Commands

        private static async Task RunUpdaterTest(string[] additionalArgs)
        {
            await UpdaterTestCLI.RunCLI(additionalArgs);
        }

        private static async Task RunFullUpdaterTest()
        {
            WriteLine();
            WriteLine("Starting comprehensive updater test...");
            WriteLine("This may take several minutes...");
            
            try
            {
                await UpdaterTester.RunFullUpdateTest();
            }
            catch (Exception ex)
            {
                WriteLine($"Test failed: {ex.Message}");
            }
        }

        private static async Task RunVersionTest()
        {
            WriteLine();
            WriteLine("Starting version check test...");
            
            try
            {
                await UpdaterTester.TestVersionCheckOnly();
            }
            catch (Exception ex)
            {
                WriteLine($"Test failed: {ex.Message}");
            }
        }

        private static async Task RunRetryTest()
        {
            WriteLine();
            WriteLine("Starting retry mechanism test...");
            
            try
            {
                await UpdaterTester.TestRetryMechanismOnly();
            }
            catch (Exception ex)
            {
                WriteLine($"Test failed: {ex.Message}");
            }
        }

        private static async Task RunLoggingTest()
        {
            WriteLine();
            WriteLine("Starting logging system test...");
            
            try
            {
                UpdaterLogger.LogInfo("CLI Test - INFO Level");
                UpdaterLogger.LogWarning("CLI Test - WARNING Level");
                UpdaterLogger.LogError("CLI Test - ERROR Level", new Exception("Test exception"));
                UpdaterLogger.LogDebug("CLI Test - DEBUG Level");
                
                WriteLine("Logging test completed. Check log files in logs/ directory.");
            }
            catch (Exception ex)
            {
                WriteLine($"Logging test failed: {ex.Message}");
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
            WriteLine("Verbose mode enabled.");
            WriteLine("The application will start with detailed logging enabled.");
            WriteLine();
            
            // Set environment variable or flag for verbose mode
            Environment.SetEnvironmentVariable("DARTSHUB_VERBOSE", "true");
            
            // Enable debug logging
            UpdaterLogger.LogInfo("Verbose mode enabled via command line");
        }

        private static void EnableBetaMode()
        {
            WriteLine("Beta tester mode enabled.");
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