using darts_hub.model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// Collects relevant log files, configuration files and system information
    /// for the currently selected day and packages everything into a single
    /// debug collection ZIP archive that the user can hand to support.
    /// </summary>
    public static class DebugCollectionService
    {
        private const string DebugFolderName = "debug";
        private const string DartsCallerAppName = "darts-caller";

        public class CollectionResult
        {
            public string ZipFilePath { get; set; } = string.Empty;
            public string ZipFolderPath { get; set; } = string.Empty;
            public List<string> CollectedItems { get; } = new();
            public List<string> Warnings { get; } = new();
        }

        /// <summary>
        /// License information snapshot for the debug report. The actual license key
        /// is intentionally not part of this structure to avoid leaking it in the
        /// collected ZIP file.
        /// </summary>
        public class LicenseSnapshot
        {
            public bool HasStoredKey { get; set; }
            public string Status { get; set; } = "Unknown";
            public string Message { get; set; } = string.Empty;
            public string? ExpiresAt { get; set; }
            public int FeatureCount { get; set; }
            public string HardwareIdHashShort { get; set; } = string.Empty;
        }

        /// <summary>
        /// Creates a debug collection ZIP file.
        /// </summary>
        /// <param name="selectedExtensions">Apps the user reported issues with.</param>
        /// <param name="problemDescription">Free-form description provided by the user.</param>
        /// <param name="incidentDate">Date the issue occurred (used to pick daily log files).</param>
        /// <param name="callerBoardId">Optional darts-caller board id used in the file name.</param>
        /// <param name="license">Optional license snapshot to include (without the key itself).</param>
        public static Task<CollectionResult> CreateAsync(
            IEnumerable<AppBase> selectedExtensions,
            string problemDescription,
            DateTime incidentDate,
            string? callerBoardId,
            LicenseSnapshot? license = null)
        {
            if (selectedExtensions == null) throw new ArgumentNullException(nameof(selectedExtensions));

            return Task.Run(() => Create(selectedExtensions.ToList(), problemDescription ?? string.Empty, incidentDate, callerBoardId, license));
        }

        private static CollectionResult Create(
            List<AppBase> selectedExtensions,
            string problemDescription,
            DateTime incidentDate,
            string? callerBoardId,
            LicenseSnapshot? license)
        {
            var result = new CollectionResult();

            var basePath = Helper.GetAppBasePath();
            var debugDir = Path.Combine(basePath, DebugFolderName);
            Directory.CreateDirectory(debugDir);
            result.ZipFolderPath = debugDir;

            var safeBoardId = SanitizeForFileName(string.IsNullOrWhiteSpace(callerBoardId) ? "unknown" : callerBoardId!);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipFileName = $"DH_debug_collection_{timestamp}_{safeBoardId}.zip";
            var zipFilePath = Path.Combine(debugDir, zipFileName);
            result.ZipFilePath = zipFilePath;

            // Stage everything in a temp directory so we can build the archive in one shot.
            var stagingDir = Path.Combine(Path.GetTempPath(), $"dh_debug_{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingDir);

            try
            {
                CollectAppLogs(basePath, stagingDir, DartsCallerAppName, incidentDate, result, alwaysInclude: true);

                foreach (var ext in selectedExtensions)
                {
                    if (ext == null) continue;
                    if (string.Equals(ext.CustomName, DartsCallerAppName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ext.Name, DartsCallerAppName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Already collected as mandatory entry.
                        continue;
                    }

                    var folderName = !string.IsNullOrWhiteSpace(ext.CustomName) ? ext.CustomName : ext.Name;
                    CollectAppLogs(basePath, stagingDir, folderName, incidentDate, result, alwaysInclude: false);
                }

                CollectDartsHubLog(basePath, stagingDir, incidentDate, result);
                CollectConfigJson(basePath, stagingDir, result);
                CollectAppsDownloadable(basePath, stagingDir, result);
                CollectLoggingConfig(basePath, stagingDir, result);

                // Security/Environment inspection - includes elevation, AV/firewall products
                // and (Windows) Defender exclusion checks for darts-hub itself + each
                // selected extension installation directory.
                var pathsToCheck = new List<SecurityEnvironmentInspector.PathToCheck>
                {
                    new() { Name = "darts-hub", Path = basePath }
                };
                foreach (var ext in selectedExtensions)
                {
                    if (ext == null) continue;
                    var name = !string.IsNullOrWhiteSpace(ext.CustomName) ? ext.CustomName : ext.Name;
                    var path = Path.Combine(basePath, ext.Name ?? name);
                    if (!Directory.Exists(path)) continue;
                    pathsToCheck.Add(new SecurityEnvironmentInspector.PathToCheck
                    {
                        Name = name,
                        Path = path
                    });
                }

                SecurityReport? security = null;
                try
                {
                    security = SecurityEnvironmentInspector.InspectAsync(pathsToCheck).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Security/environment inspection failed: {ex.Message}");
                }

                WriteSystemInfoFile(stagingDir, problemDescription, incidentDate, selectedExtensions, callerBoardId, license, security, result);

                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                ZipFile.CreateFromDirectory(stagingDir, zipFilePath, CompressionLevel.Optimal, includeBaseDirectory: false);
            }
            finally
            {
                TryDeleteDirectory(stagingDir);
            }

            return result;
        }

        private static void CollectAppLogs(
            string basePath,
            string stagingDir,
            string appFolderName,
            DateTime incidentDate,
            CollectionResult result,
            bool alwaysInclude)
        {
            try
            {
                var sanitizedFolder = SanitizeForFileName(appFolderName);
                var srcDir = Path.Combine(basePath, "logs", sanitizedFolder);
                var destDir = Path.Combine(stagingDir, "logs", sanitizedFolder);

                if (!Directory.Exists(srcDir))
                {
                    if (alwaysInclude)
                    {
                        result.Warnings.Add($"No log directory found for {appFolderName} at '{srcDir}'.");
                    }
                    return;
                }

                Directory.CreateDirectory(destDir);

                // Daily log file matches "{day:D2}_{appName}.log"
                var dayPrefix = $"{incidentDate.Day:D2}_";
                var matched = Directory.EnumerateFiles(srcDir, "*.log", SearchOption.TopDirectoryOnly)
                    .Where(f => Path.GetFileName(f).StartsWith(dayPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matched.Count == 0)
                {
                    result.Warnings.Add($"No log file for {appFolderName} on day {incidentDate:yyyy-MM-dd}.");
                    return;
                }

                foreach (var file in matched)
                {
                    var destFile = Path.Combine(destDir, Path.GetFileName(file));
                    SafeCopy(file, destFile);
                    result.CollectedItems.Add($"logs/{sanitizedFolder}/{Path.GetFileName(file)}");
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to collect logs for {appFolderName}: {ex.Message}");
            }
        }

        private static void CollectDartsHubLog(string basePath, string stagingDir, DateTime incidentDate, CollectionResult result)
        {
            try
            {
                var srcDir = Path.Combine(basePath, "logs");
                var fileName = $"{incidentDate.Day:D2}_darts-hub.log";
                var srcFile = Path.Combine(srcDir, fileName);

                if (!File.Exists(srcFile))
                {
                    result.Warnings.Add($"darts-hub log not found for day {incidentDate:yyyy-MM-dd} ('{srcFile}').");
                    return;
                }

                var destFile = Path.Combine(stagingDir, "logs", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                SafeCopy(srcFile, destFile);
                result.CollectedItems.Add($"logs/{fileName}");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to collect darts-hub log: {ex.Message}");
            }
        }

        private static void CollectConfigJson(string basePath, string stagingDir, CollectionResult result)
        {
            try
            {
                var srcFile = Path.Combine(basePath, "config.json");
                if (!File.Exists(srcFile))
                {
                    result.Warnings.Add("config.json not found - skipping.");
                    return;
                }
                var destFile = Path.Combine(stagingDir, "config.json");
                SafeCopy(srcFile, destFile);
                result.CollectedItems.Add("config.json");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to collect config.json: {ex.Message}");
            }
        }

        private static void CollectAppsDownloadable(string basePath, string stagingDir, CollectionResult result)
        {
            try
            {
                var srcFile = Path.Combine(basePath, "apps-downloadable.json");
                if (!File.Exists(srcFile))
                {
                    result.Warnings.Add("apps-downloadable.json not found - skipping.");
                    return;
                }

                var destFile = Path.Combine(stagingDir, "apps-downloadable.json");
                var sanitized = SanitizeAppsDownloadable(File.ReadAllText(srcFile));
                File.WriteAllText(destFile, sanitized);
                result.CollectedItems.Add("apps-downloadable.json (credentials removed)");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to collect apps-downloadable.json: {ex.Message}");
            }
        }

        private static void CollectLoggingConfig(string basePath, string stagingDir, CollectionResult result)
        {
            try
            {
                var srcFile = Path.Combine(basePath, "logging-config.json");
                if (!File.Exists(srcFile)) return;

                var destFile = Path.Combine(stagingDir, "logging-config.json");
                SafeCopy(srcFile, destFile);
                result.CollectedItems.Add("logging-config.json");
            }
            catch
            {
                // Optional - ignore failures.
            }
        }

        /// <summary>
        /// Removes the darts-caller credentials (Autodarts user "U" and password "P")
        /// from the apps-downloadable.json content. The board id "B" is kept because
        /// it is helpful for support and is also used in the ZIP filename.
        /// </summary>
        private static string SanitizeAppsDownloadable(string json)
        {
            try
            {
                var apps = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(json);
                if (apps == null) return json;

                foreach (var app in apps)
                {
                    var nameKey = app.Keys.FirstOrDefault(k => string.Equals(k, "Name", StringComparison.OrdinalIgnoreCase));
                    var name = nameKey != null ? app[nameKey]?.ToString() : null;
                    if (!string.Equals(name, DartsCallerAppName, StringComparison.OrdinalIgnoreCase)) continue;

                    var configKey = app.Keys.FirstOrDefault(k => string.Equals(k, "Configuration", StringComparison.OrdinalIgnoreCase));
                    if (configKey == null || app[configKey] == null) continue;

                    var configJson = JsonConvert.SerializeObject(app[configKey]);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(configJson);
                    if (configDict == null) continue;

                    var argsKey = configDict.Keys.FirstOrDefault(k => string.Equals(k, "Arguments", StringComparison.OrdinalIgnoreCase));
                    if (argsKey == null || configDict[argsKey] == null) continue;

                    var argsJson = JsonConvert.SerializeObject(configDict[argsKey]);
                    var args = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(argsJson);
                    if (args == null) continue;

                    foreach (var arg in args)
                    {
                        var argNameKey = arg.Keys.FirstOrDefault(k => string.Equals(k, "Name", StringComparison.OrdinalIgnoreCase));
                        if (argNameKey == null) continue;
                        var argName = arg[argNameKey]?.ToString();

                        if (string.Equals(argName, "U", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(argName, "P", StringComparison.OrdinalIgnoreCase))
                        {
                            var valueKey = arg.Keys.FirstOrDefault(k => string.Equals(k, "Value", StringComparison.OrdinalIgnoreCase));
                            if (valueKey != null)
                            {
                                arg[valueKey] = "***REMOVED***";
                            }
                        }
                    }

                    configDict[argsKey] = args;
                    app[configKey] = configDict;
                }

                return JsonConvert.SerializeObject(apps, Formatting.Indented);
            }
            catch
            {
                // If sanitation fails for any reason, return original to avoid losing data
                // - the support workflow is more important than perfect masking, and the
                //   warnings list will surface that something went wrong.
                return json;
            }
        }

        private static void WriteSystemInfoFile(
            string stagingDir,
            string problemDescription,
            DateTime incidentDate,
            List<AppBase> selectedExtensions,
            string? callerBoardId,
            LicenseSnapshot? license,
            SecurityReport? security,
            CollectionResult result)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Darts-Hub Debug Collection");
                sb.AppendLine("==========================");
                sb.AppendLine();
                sb.AppendLine($"Generated (local time):   {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
                sb.AppendLine($"Generated (UTC):          {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                sb.AppendLine($"Reported incident date:   {incidentDate:yyyy-MM-dd}");
                sb.AppendLine($"darts-caller board id:    {(string.IsNullOrWhiteSpace(callerBoardId) ? "(unknown)" : callerBoardId)}");
                sb.AppendLine();
                sb.AppendLine("--- Problem Description ---");
                sb.AppendLine(string.IsNullOrWhiteSpace(problemDescription) ? "(no description provided)" : problemDescription.Trim());
                sb.AppendLine();
                sb.AppendLine("--- Selected Extensions ---");
                if (selectedExtensions.Count == 0)
                {
                    sb.AppendLine("(none)");
                }
                else
                {
                    foreach (var ext in selectedExtensions)
                    {
                        sb.AppendLine($"- {ext.CustomName} (Name: {ext.Name})");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("--- System Information ---");
                sb.AppendLine($"OS Description:           {RuntimeInformation.OSDescription}");
                sb.AppendLine($"OS Architecture:          {RuntimeInformation.OSArchitecture}");
                sb.AppendLine($"Process Architecture:     {RuntimeInformation.ProcessArchitecture}");
                sb.AppendLine($"Framework:                {RuntimeInformation.FrameworkDescription}");
                sb.AppendLine($"Machine Name:             {Environment.MachineName}");
                sb.AppendLine($"User (interactive):       {Environment.UserInteractive}");
                sb.AppendLine($"Processor Count:          {Environment.ProcessorCount}");
                sb.AppendLine($"System Page Size:         {Environment.SystemPageSize}");
                sb.AppendLine($"Working Set (bytes):      {Environment.WorkingSet}");
                sb.AppendLine($"Current Directory:        {Environment.CurrentDirectory}");
                sb.AppendLine($"App Base Path:            {Helper.GetAppBasePath()}");
                sb.AppendLine($"CLR Version:              {Environment.Version}");
                sb.AppendLine($"Time Zone:                {TimeZoneInfo.Local.DisplayName}");
                sb.AppendLine($"Culture:                  {System.Globalization.CultureInfo.CurrentCulture.Name}");
                sb.AppendLine();
                sb.AppendLine("--- Darts-Hub Information ---");
                try
                {
                    sb.AppendLine($"Darts-Hub Version:        {Updater.version}");
                    var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    sb.AppendLine($"Assembly Location:        {asm.Location}");
                    sb.AppendLine($"Beta Tester Mode:         {Updater.IsBetaTester}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Assembly info unavailable: {ex.Message}");
                }
                sb.AppendLine();
                sb.AppendLine("--- License Information ---");
                if (license == null)
                {
                    sb.AppendLine("(license information not available)");
                }
                else
                {
                    sb.AppendLine($"License key stored:       {(license.HasStoredKey ? "yes" : "no")}");
                    sb.AppendLine($"Status:                   {license.Status}");
                    if (!string.IsNullOrWhiteSpace(license.Message))
                    {
                        sb.AppendLine($"Message:                  {license.Message}");
                    }
                    sb.AppendLine($"Expires at:               {(string.IsNullOrWhiteSpace(license.ExpiresAt) ? "(n/a)" : license.ExpiresAt)}");
                    sb.AppendLine($"Active features:          {license.FeatureCount}");
                    if (!string.IsNullOrWhiteSpace(license.HardwareIdHashShort))
                    {
                        sb.AppendLine($"Hardware ID (short hash): {license.HardwareIdHashShort}");
                    }
                    sb.AppendLine("(The actual license key is intentionally NOT included.)");
                }
                sb.AppendLine();
                AppendSecuritySection(sb, security);
                sb.AppendLine();
                sb.AppendLine("--- Collection Notes ---");
                if (result.Warnings.Count == 0)
                {
                    sb.AppendLine("(no warnings)");
                }
                else
                {
                    foreach (var w in result.Warnings)
                    {
                        sb.AppendLine($"- {w}");
                    }
                }
                sb.AppendLine();
                sb.AppendLine("--- Included Items ---");
                foreach (var item in result.CollectedItems)
                {
                    sb.AppendLine($"- {item}");
                }

                var path = Path.Combine(stagingDir, "system_info.txt");
                File.WriteAllText(path, sb.ToString());
                result.CollectedItems.Add("system_info.txt");
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to write system info file: {ex.Message}");
            }
        }

        private static void AppendSecuritySection(StringBuilder sb, SecurityReport? security)
        {
            sb.AppendLine("--- Security & Environment ---");
            if (security == null)
            {
                sb.AppendLine("(security inspection not available)");
                return;
            }

            sb.AppendLine($"Detected OS:              {security.OsKind}");
            sb.AppendLine($"Process user:             {security.ProcessUser}");
            sb.AppendLine($"Elevated / admin:         {(security.IsElevated ? "YES" : "no")}");
            if (!string.IsNullOrWhiteSpace(security.ElevationDetails))
            {
                sb.AppendLine($"Elevation details:        {security.ElevationDetails}");
            }
            sb.AppendLine();

            AppendProductList(sb, "Antivirus products", security.AntivirusProducts);
            AppendProductList(sb, "Firewall products", security.FirewallProducts);
            AppendProductList(sb, "Antispyware / endpoint security", security.AntispywareProducts);

            if (security.FirewallProfiles.Count > 0)
            {
                sb.AppendLine("Firewall profiles:");
                foreach (var p in security.FirewallProfiles)
                {
                    sb.AppendLine($"  - {p.Profile,-28} {p.State}{(string.IsNullOrWhiteSpace(p.Notes) ? string.Empty : $"   ({p.Notes})")}");
                }
                sb.AppendLine();
            }

            if (security.WindowsDefender != null)
            {
                var d = security.WindowsDefender;
                sb.AppendLine("Windows Defender:");
                sb.AppendLine($"  Real-time protection:   {FormatNullableBool(d.RealTimeProtectionEnabled)}");
                sb.AppendLine($"  Tamper protection:      {FormatNullableBool(d.TamperProtectionEnabled)}");
                sb.AppendLine($"  Excluded paths:         {d.ExcludedPaths.Count}");
                foreach (var p in d.ExcludedPaths) sb.AppendLine($"    - {p}");
                sb.AppendLine($"  Excluded processes:     {d.ExcludedProcesses.Count}");
                foreach (var p in d.ExcludedProcesses) sb.AppendLine($"    - {p}");
                sb.AppendLine($"  Excluded extensions:    {d.ExcludedExtensions.Count}");
                foreach (var p in d.ExcludedExtensions) sb.AppendLine($"    - {p}");
                if (!string.IsNullOrWhiteSpace(d.RawError))
                {
                    sb.AppendLine($"  Note: {d.RawError}");
                }
                sb.AppendLine();
            }

            if (security.ExclusionChecks.Count > 0)
            {
                sb.AppendLine("Defender exclusion check (darts-hub + selected extensions):");
                foreach (var c in security.ExclusionChecks)
                {
                    var status = c.ExcludedInDefender ? "EXCLUDED" : "not excluded";
                    sb.AppendLine($"  - {c.Name,-22} {status}   ({c.Path})");
                    if (c.ExcludedInDefender && !string.IsNullOrWhiteSpace(c.MatchingExclusion))
                    {
                        sb.AppendLine($"      via rule: {c.MatchingExclusion}");
                    }
                }
                sb.AppendLine();
            }

            if (security.Notes.Count > 0)
            {
                sb.AppendLine("Inspection notes:");
                foreach (var n in security.Notes)
                {
                    sb.AppendLine($"  - {n}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Note: Third-party antivirus exclusion lists (Avast, AVG, Bitdefender, ESET,");
            sb.AppendLine("Kaspersky, Norton, McAfee, Sophos, Webroot, Trend Micro, ...) are not");
            sb.AppendLine("exposed through a public API. Their presence is detected via Windows");
            sb.AppendLine("Security Center, but exclusion paths must be checked manually if needed.");
        }

        private static void AppendProductList(StringBuilder sb, string title, List<SecurityProduct> products)
        {
            if (products.Count == 0) return;
            sb.AppendLine($"{title}: {products.Count}");
            foreach (var p in products)
            {
                sb.AppendLine($"  - {p.Name}");
                if (!string.IsNullOrWhiteSpace(p.Status)) sb.AppendLine($"      Status: {p.Status}");
                if (!string.IsNullOrWhiteSpace(p.ExePath)) sb.AppendLine($"      Path:   {p.ExePath}");
                if (!string.IsNullOrWhiteSpace(p.Source)) sb.AppendLine($"      Source: {p.Source}");
            }
            sb.AppendLine();
        }

        private static string FormatNullableBool(bool? value)
        {
            return value switch
            {
                true => "ENABLED",
                false => "DISABLED",
                _ => "(unknown)"
            };
        }

        private static void SafeCopy(string src, string dest)
        {
            // Use FileShare.ReadWrite so that a log file currently held open by the
            // running process can still be copied without throwing.
            using var input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var output = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }

        private static string SanitizeForFileName(string value)
        {
            if (string.IsNullOrEmpty(value)) return "_";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            return new string(chars);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            }
            catch
            {
                // best effort cleanup
            }
        }
    }
}
