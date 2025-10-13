using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using darts_hub.control;

namespace darts_hub.UI
{
    /// <summary>
    /// Helper class for creating and managing backups of DartsHub configuration and data
    /// </summary>
    public static class BackupHelper
    {
        private const string BACKUP_DIRECTORY = "backups";
        private const string CONFIG_FILE = "config.json";
        private const string SETTINGS_FILE = "apps-downloadable.json";
        private const string APPSLOCAL_FILE = "apps-local.json";
        private const string APPSOPEN_FILE = "apps-open.json";
        private const string PROFILES_DIRECTORY = "profiles";
        private const string LOGS_DIRECTORY = "logs";
        private const string WIZARD_CONFIG = "control/wizard/WizardArgumentsConfig.json";

        
        // Use System.Console for better PowerShell compatibility
        private static void WriteLine(string text = "") => System.Console.WriteLine(text);
        private static void Write(string text) => System.Console.Write(text);
        
        /// <summary>
        /// Creates a full backup of all important DartsHub data
        /// </summary>
        public static async Task<string> CreateFullBackup(string customName = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var backupName = customName ?? $"dartshub-backup-{timestamp}";
                var backupDir = Path.Combine(Helper.GetAppBasePath(), BACKUP_DIRECTORY);
                var backupPath = Path.Combine(backupDir, $"{backupName}.zip");
                
                // Ensure backup directory exists
                Directory.CreateDirectory(backupDir);
                
                WriteLine($"Creating backup: {backupName}...");
                
                // Create temporary directory for staging files
                var tempDir = Path.Combine(Path.GetTempPath(), $"dartshub_backup_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    var filesToBackup = new List<(string source, string destination)>();
                    var appBasePath = Helper.GetAppBasePath();
                    
                    // 1. Configuration file
                    await AddFileToBackup(appBasePath, CONFIG_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, SETTINGS_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, APPSLOCAL_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, APPSOPEN_FILE, tempDir, filesToBackup);

                    // 2. Profiles directory (if exists)
                    await AddDirectoryToBackup(appBasePath, PROFILES_DIRECTORY, tempDir, filesToBackup);
                    
                    // 3. Recent logs (last 30 days)
                    await AddRecentLogsToBackup(appBasePath, tempDir, filesToBackup);
                    
                    // 4. Wizard configuration
                    await AddFileToBackup(appBasePath, WIZARD_CONFIG, tempDir, filesToBackup);
                    
                    // 5. Add backup manifest
                    await CreateBackupManifest(tempDir, backupName, filesToBackup);
                    
                    // Copy files to temp directory
                    foreach (var (source, destination) in filesToBackup)
                    {
                        if (File.Exists(source))
                        {
                            var destDir = Path.GetDirectoryName(destination);
                            if (!string.IsNullOrEmpty(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }
                            File.Copy(source, destination, true);
                        }
                    }
                    
                    // Create ZIP archive
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    
                    ZipFile.CreateFromDirectory(tempDir, backupPath, CompressionLevel.Optimal, false);
                    
                    var backupSize = new FileInfo(backupPath).Length;
                    WriteLine($"✅ Backup created successfully: {backupPath}");
                    WriteLine($"   Size: {FormatFileSize(backupSize)}");
                    WriteLine($"   Files: {filesToBackup.Count}");
                    
                    return backupPath;
                }
                finally
                {
                    // Clean up temp directory
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create backup: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Creates a configuration-only backup
        /// </summary>
        public static async Task<string> CreateConfigBackup(string customName = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var backupName = customName ?? $"dartshub-config-{timestamp}";
                var backupDir = Path.Combine(Helper.GetAppBasePath(), BACKUP_DIRECTORY);
                var backupPath = Path.Combine(backupDir, $"{backupName}.zip");
                
                Directory.CreateDirectory(backupDir);
                
                WriteLine($"Creating configuration backup: {backupName}...");
                
                var tempDir = Path.Combine(Path.GetTempPath(), $"dartshub_config_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);
                
                try
                {
                    var filesToBackup = new List<(string source, string destination)>();
                    var appBasePath = Helper.GetAppBasePath();
                    
                    // Configuration file
                    await AddFileToBackup(appBasePath, CONFIG_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, SETTINGS_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, APPSLOCAL_FILE, tempDir, filesToBackup);
                    await AddFileToBackup(appBasePath, APPSOPEN_FILE, tempDir, filesToBackup);

                    // Wizard configuration
                    await AddFileToBackup(appBasePath, WIZARD_CONFIG, tempDir, filesToBackup);
                    
                    // Create manifest
                    await CreateBackupManifest(tempDir, backupName, filesToBackup, "Configuration Only");
                    
                    // Copy files
                    foreach (var (source, destination) in filesToBackup)
                    {
                        if (File.Exists(source))
                        {
                            var destDir = Path.GetDirectoryName(destination);
                            if (!string.IsNullOrEmpty(destDir))
                            {
                                Directory.CreateDirectory(destDir);
                            }
                            File.Copy(source, destination, true);
                        }
                    }
                    
                    // Create ZIP
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    
                    ZipFile.CreateFromDirectory(tempDir, backupPath, CompressionLevel.Optimal, false);
                    
                    var backupSize = new FileInfo(backupPath).Length;
                    WriteLine($"✅ Configuration backup created: {backupPath}");
                    WriteLine($"   Size: {FormatFileSize(backupSize)}");
                    
                    return backupPath;
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create configuration backup: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Lists all available backups
        /// </summary>
        public static void ListBackups()
        {
            try
            {
                var backupDir = Path.Combine(Helper.GetAppBasePath(), BACKUP_DIRECTORY);
                
                if (!Directory.Exists(backupDir))
                {
                    WriteLine("No backup directory found.");
                    WriteLine($"Backups will be created in: {backupDir}");
                    return;
                }
                
                var backupFiles = Directory.GetFiles(backupDir, "*.zip", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToArray();
                
                if (backupFiles.Length == 0)
                {
                    WriteLine("No backups found.");
                    return;
                }
                
                WriteLine("=== AVAILABLE BACKUPS ===");
                WriteLine();
                
                for (int i = 0; i < backupFiles.Length; i++)
                {
                    var fileInfo = new FileInfo(backupFiles[i]);
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    
                    WriteLine($"{i + 1}. {fileName}");
                    WriteLine($"   Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    WriteLine($"   Size: {FormatFileSize(fileInfo.Length)}");
                    WriteLine($"   Path: {fileInfo.FullName}");
                    
                    // Try to read manifest for additional info
                    try
                    {
                        using var archive = ZipFile.OpenRead(fileInfo.FullName);
                        var manifestEntry = archive.GetEntry("backup-manifest.txt");
                        if (manifestEntry != null)
                        {
                            using var stream = manifestEntry.Open();
                            using var reader = new StreamReader(stream);
                            var manifest = reader.ReadToEnd();
                            
                            // Extract backup type from manifest
                            var lines = manifest.Split('\n');
                            var typeLine = lines.FirstOrDefault(l => l.StartsWith("Type:"));
                            if (!string.IsNullOrEmpty(typeLine))
                            {
                                var type = typeLine.Substring(5).Trim();
                                WriteLine($"   Type: {type}");
                            }
                        }
                    }
                    catch
                    {
                        // Ignore manifest read errors
                    }
                    
                    if (i < backupFiles.Length - 1)
                        WriteLine();
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Error listing backups: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Restores a backup from file
        /// </summary>
        public static async Task<bool> RestoreBackup(string backupPath, bool interactive = true)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    WriteLine($"❌ Backup file not found: {backupPath}");
                    return false;
                }
                
                WriteLine($"Preparing to restore backup: {Path.GetFileName(backupPath)}");
                
                // Show backup info
                var fileInfo = new FileInfo(backupPath);
                WriteLine($"   Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                WriteLine($"   Size: {FormatFileSize(fileInfo.Length)}");
                WriteLine();
                
                // Interactive confirmation
                if (interactive)
                {
                    WriteLine("⚠️  WARNING: This will overwrite existing configuration files!");
                    WriteLine("   Make sure to backup your current configuration first.");
                    WriteLine();
                    Write("Do you want to continue? (y/N): ");
                    
                    var response = System.Console.ReadLine();
                    if (!string.Equals(response?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                    {
                        WriteLine("Restore cancelled.");
                        return false;
                    }
                }
                
                var appBasePath = Helper.GetAppBasePath();
                var tempDir = Path.Combine(Path.GetTempPath(), $"dartshub_restore_{Guid.NewGuid():N}");
                
                try
                {
                    // Extract backup to temp directory
                    ZipFile.ExtractToDirectory(backupPath, tempDir);
                    
                    var restoredFiles = 0;
                    
                    // Restore configuration files
                    var configSource = Path.Combine(tempDir, CONFIG_FILE);
                    var configDest = Path.Combine(appBasePath, CONFIG_FILE);
                    if (File.Exists(configSource))
                    {
                        File.Copy(configSource, configDest, true);
                        WriteLine($"✅ Restored: {CONFIG_FILE}");
                        restoredFiles++;
                    }
                    
                    // Restore settings file
                    var settingsSource = Path.Combine(tempDir, SETTINGS_FILE);
                    var settingsDest = Path.Combine(appBasePath, SETTINGS_FILE);
                    if (File.Exists(settingsSource))
                    {
                        File.Copy(settingsSource, settingsDest, true);
                        WriteLine($"✅ Restored: {SETTINGS_FILE}");
                        restoredFiles++;
                    }
                    
                    // Restore apps local file
                    var appsLocalSource = Path.Combine(tempDir, APPSLOCAL_FILE);
                    var appsLocalDest = Path.Combine(appBasePath, APPSLOCAL_FILE);
                    if (File.Exists(appsLocalSource))
                    {
                        File.Copy(appsLocalSource, appsLocalDest, true);
                        WriteLine($"✅ Restored: {APPSLOCAL_FILE}");
                        restoredFiles++;
                    }
                    
                    // Restore apps open file
                    var appsOpenSource = Path.Combine(tempDir, APPSOPEN_FILE);
                    var appsOpenDest = Path.Combine(appBasePath, APPSOPEN_FILE);
                    if (File.Exists(appsOpenSource))
                    {
                        File.Copy(appsOpenSource, appsOpenDest, true);
                        WriteLine($"✅ Restored: {APPSOPEN_FILE}");
                        restoredFiles++;
                    }
                    
                    // Restore wizard configuration
                    var wizardSource = Path.Combine(tempDir, WIZARD_CONFIG);
                    var wizardDest = Path.Combine(appBasePath, WIZARD_CONFIG);
                    if (File.Exists(wizardSource))
                    {
                        var destDir = Path.GetDirectoryName(wizardDest);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }
                        File.Copy(wizardSource, wizardDest, true);
                        WriteLine($"✅ Restored: {WIZARD_CONFIG}");
                        restoredFiles++;
                    }
                    
                    WriteLine();
                    WriteLine($"✅ Backup restored successfully!");
                    WriteLine($"   Files restored: {restoredFiles}");
                    WriteLine("   Please restart DartsHub to apply the restored configuration.");
                    
                    return true;
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine($"❌ Failed to restore backup: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Cleans up old backups (keeps last N backups)
        /// </summary>
        public static void CleanupOldBackups(int keepCount = 10)
        {
            try
            {
                var backupDir = Path.Combine(Helper.GetAppBasePath(), BACKUP_DIRECTORY);
                
                if (!Directory.Exists(backupDir))
                {
                    WriteLine("No backup directory found.");
                    return;
                }
                
                var backupFiles = Directory.GetFiles(backupDir, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToArray();
                
                if (backupFiles.Length <= keepCount)
                {
                    WriteLine($"Found {backupFiles.Length} backup(s). No cleanup needed (keeping {keepCount}).");
                    return;
                }
                
                var filesToDelete = backupFiles.Skip(keepCount).ToArray();
                
                WriteLine($"Found {backupFiles.Length} backup(s). Removing {filesToDelete.Length} old backup(s)...");
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file.FullName);
                        WriteLine($"   Deleted: {file.Name}");
                    }
                    catch (Exception ex)
                    {
                        WriteLine($"   Failed to delete {file.Name}: {ex.Message}");
                    }
                }
                
                WriteLine($"✅ Cleanup completed. Kept {Math.Min(backupFiles.Length, keepCount)} most recent backup(s).");
            }
            catch (Exception ex)
            {
                WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
        
        #region Helper Methods
        
        private static async Task AddFileToBackup(string basePath, string relativePath, string tempDir, List<(string source, string destination)> filesToBackup)
        {
            var sourcePath = Path.Combine(basePath, relativePath);
            var destPath = Path.Combine(tempDir, relativePath);
            
            if (File.Exists(sourcePath))
            {
                filesToBackup.Add((sourcePath, destPath));
            }
        }
        
        private static async Task AddDirectoryToBackup(string basePath, string relativePath, string tempDir, List<(string source, string destination)> filesToBackup)
        {
            var sourceDir = Path.Combine(basePath, relativePath);
            
            if (!Directory.Exists(sourceDir))
                return;
            
            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativeFile = Path.GetRelativePath(basePath, file);
                var destPath = Path.Combine(tempDir, relativeFile);
                filesToBackup.Add((file, destPath));
            }
        }
        
        private static async Task AddRecentLogsToBackup(string basePath, string tempDir, List<(string source, string destination)> filesToBackup)
        {
            var logsDir = Path.Combine(basePath, LOGS_DIRECTORY);
            
            if (!Directory.Exists(logsDir))
                return;
            
            var cutoffDate = DateTime.Now.AddDays(-30);
            var recentLogs = Directory.GetFiles(logsDir, "*.log", SearchOption.AllDirectories)
                .Where(f => new FileInfo(f).CreationTime > cutoffDate)
                .ToArray();
            
            foreach (var logFile in recentLogs)
            {
                var relativeFile = Path.GetRelativePath(basePath, logFile);
                var destPath = Path.Combine(tempDir, relativeFile);
                filesToBackup.Add((logFile, destPath));
            }
        }
        
        private static async Task CreateBackupManifest(string tempDir, string backupName, List<(string source, string destination)> filesToBackup, string backupType = "Full Backup")
        {
            var manifestPath = Path.Combine(tempDir, "backup-manifest.txt");
            var manifest = "DartsHub Backup Manifest\n" +
                          "========================\n\n" +
                          $"Backup Name: {backupName}\n" +
                          $"Type: {backupType}\n" +
                          $"Created: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                          $"DartsHub Version: {Updater.version}\n" +
                          $"Platform: {Environment.OSVersion}\n\n" +
                          "Files included:\n" +
                          string.Join("\n", filesToBackup.Select(f => $"  - {Path.GetRelativePath(tempDir, f.destination)}")) + "\n\n" +
                          $"Total Files: {filesToBackup.Count}\n";
            
            await File.WriteAllTextAsync(manifestPath, manifest);
        }
        
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
        
        #endregion
    }
}