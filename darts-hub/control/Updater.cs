using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using darts_hub.model;

namespace darts_hub.control
{

    /// <summary>
    /// Manage new releases and initialize update-process
    /// </summary>
    public static class Updater
    {
        // ATTRIBUTES

        // Increase for new build ..
        public static readonly string version = "b1.5.0.7";
        //public static readonly string version = "b1.4.1.20";


        public static event EventHandler<ReleaseEventArgs>? NoNewReleaseFound;
        public static event EventHandler<ReleaseEventArgs>? NewReleaseFound;
        public static event EventHandler<ReleaseEventArgs>? ReleaseInstallInitialized;
        public static event EventHandler<ReleaseEventArgs>? ReleaseDownloadStarted;
        public static event EventHandler<ReleaseEventArgs>? ReleaseDownloadFailed;
        public static event EventHandler<DownloadProgressChangedEventArgs>? ReleaseDownloadProgressed;

        private static string latestRepoVersion = string.Empty;
        private const string appSourceUrl = "https://github.com/lbormann/darts-hub/releases/download";
        private const string appSourceUrlLatest = "https://api.github.com/repos/lbormann/darts-hub/releases/latest";
        public static readonly string appSourceUrlChangelog = "https://raw.githubusercontent.com/lbormann/darts-hub/main/CHANGELOG.md";
        private const string appDestination = "updates";
        private const string requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
        private const int requestTimeout = 10; // Increased from 4 to 10 seconds

        private static string destinationPath = String.Empty;
        private static string downloadPath = String.Empty;
        private static string downloadDirectory = String.Empty;




        // METHODS
        public static bool IsBetaTester { get; set; } = false;
        public static string SkippedVersion { get; set; } = string.Empty;
        public static string LatestFoundVersion { get; private set; } = string.Empty;

        public static async void CheckNewVersion()
        {
            UpdaterLogger.LogSystemInfo();
            UpdaterLogger.LogInfo("Starting version check process");
            
            try
            {
                if (IsBetaTester)
                {
                    UpdaterLogger.LogInfo("Beta tester mode enabled - checking for beta releases");
                    await CheckNewBetaVersion();
                }
                else
                {
                    UpdaterLogger.LogInfo("Checking for stable releases");
                    await CheckNewStableVersion();
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Unexpected error during version check", ex);
                OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", $"Version check failed: {ex.Message}"));
            }
            finally
            {
                UpdaterLogger.LogSessionEnd();
            }
        }

        private static async Task CheckNewStableVersion()
        {
            try
            {
                UpdaterLogger.LogInfo($"Current version: {version}");
                UpdaterLogger.LogInfo($"Checking latest stable release from: {appSourceUrlLatest}");
                
                var latestGithubVersion = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
                    client.Timeout = TimeSpan.FromSeconds(requestTimeout);
                    
                    UpdaterLogger.LogDebug("Sending HTTP request to GitHub API");
                    var result = await client.GetStringAsync(appSourceUrlLatest);
                    
                    UpdaterLogger.LogDebug("Parsing GitHub API response");
                    int tagNameIndex = result.IndexOf("tag_name");
                    if (tagNameIndex == -1) 
                    {
                        UpdaterLogger.LogError("GitHub API response parsing failed: tag_name not found");
                        throw new ArgumentException("github-tagName-Index not found");
                    }
                    
                    result = result.Substring(tagNameIndex);
                    int tagNameCommaIndex = result.IndexOf(',');
                    if (tagNameCommaIndex == -1) 
                    {
                        UpdaterLogger.LogError("GitHub API response parsing failed: tag_name comma not found");
                        throw new ArgumentException("github-tagNameComma-Index not found");
                    }
                    
                    result = result.Substring("tag_name: \"".Length, tagNameCommaIndex - "tag_name: \"".Length);
                    return result.Replace("\"", "");
                }, 3, 2000, "GitHub API Version Check");

                UpdaterLogger.LogInfo($"Latest GitHub version: {latestGithubVersion}");
                LatestFoundVersion = latestGithubVersion;

                if (CompareVersions(version, latestGithubVersion) < 0)
                {
                    // Skip when the found version is not newer than the skipped version
                    if (!string.IsNullOrEmpty(SkippedVersion) 
                        && CompareVersions(latestGithubVersion, SkippedVersion) <= 0)
                    {
                        UpdaterLogger.LogInfo($"Version {latestGithubVersion} is <= skipped version {SkippedVersion} - treating as no update");
                        OnNoNewReleaseFound(new ReleaseEventArgs(latestGithubVersion, string.Empty));
                    }
                    else
                    {
                        UpdaterLogger.LogInfo("New version found - fetching changelog");
                        latestRepoVersion = latestGithubVersion;

                    var changelog = await RetryHelper.ExecuteWithRetryAsync(async () =>
                    {
                        return await Helper.AsyncHttpGet(appSourceUrlChangelog, requestTimeout);
                    }, 3, 1000, "Changelog Download");

                    UpdaterLogger.LogInfo($"Successfully retrieved changelog ({changelog.Length} characters)");
                        OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, changelog));
                    }
                }
                else
                {
                    UpdaterLogger.LogInfo("Current version is up to date");
                    OnNoNewReleaseFound(new ReleaseEventArgs(latestGithubVersion, string.Empty));
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Failed to check for stable version updates", ex);
                OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", ex.Message));
            }
        }

        private static async Task CheckNewBetaVersion()
        {
            try
            {
                UpdaterLogger.LogInfo($"Current version: {version}");
                UpdaterLogger.LogInfo("Checking for newest release (beta or stable) from GitHub API");

                var newestVersion = await RetryHelper.ExecuteWithRetryAsync(async () =>
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
                    client.Timeout = TimeSpan.FromSeconds(requestTimeout);

                    UpdaterLogger.LogDebug("Sending HTTP request to GitHub API for all releases");
                    var result = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases");

                    UpdaterLogger.LogDebug("Parsing releases to find newest version (beta or stable)");
                    var releases = JsonDocument.Parse(result).RootElement.EnumerateArray();
                    string? bestTag = null;

                    foreach (var release in releases)
                    {
                        if (release.GetProperty("draft").GetBoolean())
                            continue;

                        var tag = release.GetProperty("tag_name").GetString();
                        if (string.IsNullOrEmpty(tag))
                            continue;

                        if (bestTag == null || CompareVersions(tag, bestTag) > 0)
                        {
                            bestTag = tag;
                        }
                    }

                    if (bestTag == null)
                    {
                        UpdaterLogger.LogWarning("No releases found");
                    }

                    return bestTag;
                }, 3, 2000, "GitHub API Beta Version Check");

                if (newestVersion != null)
                {
                    UpdaterLogger.LogInfo($"Newest available version: {newestVersion}");
                    LatestFoundVersion = newestVersion;

                    if (CompareVersions(version, newestVersion) < 0)
                    {
                        // Skip when the found version is not newer than the skipped version
                        if (!string.IsNullOrEmpty(SkippedVersion) 
                            && CompareVersions(newestVersion, SkippedVersion) <= 0)
                        {
                            UpdaterLogger.LogInfo($"Version {newestVersion} is <= skipped version {SkippedVersion} - treating as no update");
                            OnNoNewReleaseFound(new ReleaseEventArgs(newestVersion, string.Empty));
                        }
                        else
                        {
                            UpdaterLogger.LogInfo("Newer version found - fetching changelog");
                            latestRepoVersion = newestVersion;

                        var changelog = await RetryHelper.ExecuteWithRetryAsync(async () =>
                        {
                            return await Helper.AsyncHttpGet(appSourceUrlChangelog, requestTimeout);
                        }, 3, 1000, "Changelog Download");

                        UpdaterLogger.LogInfo($"Successfully retrieved changelog ({changelog.Length} characters)");
                            OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, changelog));
                        }
                    }
                    else
                    {
                        UpdaterLogger.LogInfo("Current version is up to date");
                        OnNoNewReleaseFound(new ReleaseEventArgs(newestVersion, string.Empty));
                    }
                }
                else
                {
                    UpdaterLogger.LogWarning("No releases available");
                    OnNoNewReleaseFound(new ReleaseEventArgs("vx.x.x", "No releases found."));
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Failed to check for beta version updates", ex);
                OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", ex.Message));
            }
        }

        public static void UpdateToNewVersion()
        {
            if (!string.IsNullOrEmpty(latestRepoVersion))
            {
                UpdaterLogger.LogInfo($"Starting update process to version {latestRepoVersion}");
                
                try
                {
                    var appSourceFile = GetAppFileByOS();
                    if (String.IsNullOrEmpty(appSourceFile)) 
                    {
                        var error = "There are no releases for your specific OS.";
                        UpdaterLogger.LogError($"Update failed: {error}");
                        throw new Exception(error);
                    }

                    UpdaterLogger.LogInfo($"Target file for current OS: {appSourceFile}");

                    destinationPath = Helper.GetAppBasePath();
                    downloadPath = Path.Join(destinationPath, appDestination, appSourceFile);
                    downloadDirectory = Path.GetDirectoryName(downloadPath);

                    string downloadUrl = appSourceUrl + "/" + latestRepoVersion + "/" + appSourceFile;
                    
                    UpdaterLogger.LogInfo($"Download URL: {downloadUrl}");
                    UpdaterLogger.LogInfo($"Download path: {downloadPath}");
                    UpdaterLogger.LogInfo($"Download directory: {downloadDirectory}");

                    // Removes existing download-directory and creates a new one
                    UpdaterLogger.LogInfo("Cleaning up existing download directory");
                    Helper.RemoveDirectory(downloadDirectory, true);

                    // Inform subscribers about a pending download
                    UpdaterLogger.LogInfo("Starting download process");
                    OnReleaseDownloadStarted(new ReleaseEventArgs(latestRepoVersion, latestRepoVersion));

                    // Start the download
                    var webClient = new WebClient();
                    webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += WebClient_DownloadCompleted;
                    webClient.DownloadFileAsync(new Uri(downloadUrl), downloadPath);
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogError("Failed to initialize update process", ex);
                    Helper.RemoveDirectory(downloadDirectory);
                    OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, ex.Message));
                }
            }
            else
            {
                UpdaterLogger.LogWarning("Update requested but no latest version available");
            }
        }

        //public static async void CheckNewVersion()
        //{
        //    try
        //    {
        //        using var client = new HttpClient();
        //        client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
        //        client.Timeout = TimeSpan.FromSeconds(requestTimeout);
        //        var result = await client.GetStringAsync(appSourceUrlLatest);
        //        int tagNameIndex = result.IndexOf("tag_name");
        //        if (tagNameIndex == -1) throw new ArgumentException("github-tagName-Index not found");
        //        result = result.Substring(tagNameIndex);
        //        int tagNameCommaIndex = result.IndexOf(',');
        //        if (tagNameCommaIndex == -1) throw new ArgumentException("github-tagNameComma-Index not found");
        //        result = result.Substring("tag_name: \"".Length, tagNameCommaIndex - "tag_name: \"".Length);
        //        var latestGithubVersion = result.Replace("\"", "");

        //        if (version != latestGithubVersion)
        //        {
        //            latestRepoVersion = latestGithubVersion;
        //            var changelog = await Helper.AsyncHttpGet(appSourceUrlChangelog, requestTimeout);
        //            OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, changelog));
        //        }
        //        else
        //        {
        //            OnNoNewReleaseFound(new ReleaseEventArgs(latestGithubVersion, string.Empty));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", ex.Message));
        //    }
        //}

        //public static void UpdateToNewVersion()
        //{
        //    if (!string.IsNullOrEmpty(latestRepoVersion))
        //    {
        //        try
        //        {
        //            var appSourceFile = GetAppFileByOS();
        //            if (String.IsNullOrEmpty(appSourceFile)) throw new Exception("There are no releases for your specific OS.");

        //            destinationPath = Helper.GetAppBasePath();
        //            downloadPath = Path.Join(destinationPath, appDestination, appSourceFile);
        //            downloadDirectory = Path.GetDirectoryName(downloadPath);

        //            string downloadUrl = appSourceUrl + "/" + latestRepoVersion + "/" + appSourceFile;

        //            // Removes existing download-directory and creates a new one
        //            Helper.RemoveDirectory(downloadDirectory, true);

        //            // Inform subscribers about a pending download
        //            OnReleaseDownloadStarted(new ReleaseEventArgs(latestRepoVersion, latestRepoVersion));

        //            // Start the download
        //            var webClient = new WebClient();
        //            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
        //            webClient.DownloadFileCompleted += WebClient_DownloadCompleted;
        //            webClient.DownloadFileAsync(new Uri(downloadUrl), downloadPath);
        //        }
        //        catch (Exception ex)
        //        {
        //            Helper.RemoveDirectory(downloadDirectory);
        //            OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, ex.Message));
        //        }

        //    }
        //}



        private static string GetUpdateFileByOs()
        {
            UpdaterLogger.LogDebug("Determining update script file for current OS");
            
            string updateFile = String.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                updateFile = "update.sh";
                UpdaterLogger.LogDebug($"Linux OS detected - using {updateFile}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                updateFile = "update.bat";
                UpdaterLogger.LogDebug($"Windows OS detected - using {updateFile}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                updateFile = "update.sh";
                UpdaterLogger.LogDebug($"macOS detected - using {updateFile}");
            }
            else
            {
                UpdaterLogger.LogError("Unknown operating system detected");
            }
            
            return updateFile;
        }

        private static string GetAppFileByOS()
        {
            UpdaterLogger.LogDebug("Determining application file for current OS and architecture");
            
            string appFile = String.Empty;
            var platform = "Unknown";
            var architecture = RuntimeInformation.ProcessArchitecture.ToString();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "Linux";
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    appFile = "darts-hub-linux-X64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    appFile = "darts-hub-linux-ARM64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    appFile = "darts-hub-linux-ARM.zip";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platform = "Windows";
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    appFile = "darts-hub-windows-X64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    appFile = "darts-hub-windows-X86.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    appFile = "darts-hub-windows-ARM64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    appFile = "darts-hub-windows-ARM.zip";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "macOS";
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    appFile = "darts-hub-macOS-X64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    appFile = "darts-hub-macOS-ARM64.zip";
                }
            }
            
            if (string.IsNullOrEmpty(appFile))
            {
                UpdaterLogger.LogError($"No app file mapping found for {platform} {architecture}");
            }
            else
            {
                UpdaterLogger.LogDebug($"App file determined: {appFile} for {platform} {architecture}");
            }
            
            return appFile;
        }

        private static void EnsureExecutablePermissions(string updateFile)
        {
            var scriptPath = Path.Combine(destinationPath, updateFile);
            UpdaterLogger.LogInfo($"Setting executable permissions for: {scriptPath}");
            
            if (updateFile.EndsWith(".sh"))
            {
                try
                {
                    var chmodProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{scriptPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false
                        }
                    };

                    chmodProcess.Start();
                    chmodProcess.WaitForExit();

                    if (chmodProcess.ExitCode != 0)
                    {
                        var error = chmodProcess.StandardError.ReadToEnd();
                        UpdaterLogger.LogError($"Failed to set executable permissions. Exit code: {chmodProcess.ExitCode}, Error: {error}");
                        throw new Exception($"Failed to set executable permissions for {scriptPath}. Exit code: {chmodProcess.ExitCode}");
                    }
                    else
                    {
                        UpdaterLogger.LogInfo("Executable permissions set successfully");
                    }
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogError("Exception while setting executable permissions", ex);
                    throw;
                }
            }
            else
            {
                UpdaterLogger.LogDebug("Skipping executable permissions (not a shell script)");
            }
        }

        private static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Only log every 10% to avoid spam
            if (e.ProgressPercentage % 10 == 0)
            {
                UpdaterLogger.LogDebug($"Download progress: {e.ProgressPercentage}% ({e.BytesReceived}/{e.TotalBytesToReceive} bytes)");
            }
            OnReleaseDownloadProgressed(e);
        }

        private static void WebClient_DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                UpdaterLogger.LogInfo("Download completed - processing download result");
                
                if (e.Error != null)
                {
                    UpdaterLogger.LogError("Download failed", e.Error);
                    OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, e.Error.Message));
                    return;
                }

                if (e.Cancelled)
                {
                    UpdaterLogger.LogWarning("Download was cancelled");
                    OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, "Download was cancelled"));
                    return;
                }

                var wc = sender as WebClient;
                UpdaterLogger.LogInfo("Download successful - extracting files");

                // Extract the downloaded zip file
                UpdaterLogger.LogInfo($"Extracting {downloadPath} to {Path.GetDirectoryName(downloadPath)}");
                ZipFile.ExtractToDirectory(downloadPath, Path.GetDirectoryName(downloadPath), true);
                
                UpdaterLogger.LogInfo("Extraction completed - cleaning up zip file");
                File.Delete(downloadPath);

                // start update-batch, that waits until files are accessible; then copy new release to assembly location and delete temp files
                UpdaterLogger.LogInfo("Starting update script execution");

                // Clear skipped version after successful update download
                SkippedVersion = string.Empty;
                try
                {
                    var cfg = new Configurator("config.json");
                    cfg.Settings.SkippedVersion = string.Empty;
                    cfg.SaveSettings();
                }
                catch (Exception cfgEx)
                {
                    UpdaterLogger.LogWarning($"Could not persist SkippedVersion reset: {cfgEx.Message}");
                }

                using (var process = new Process())
                {
                    try
                    {
                        var updateFile = GetUpdateFileByOs();
                        if (String.IsNullOrEmpty(updateFile)) 
                        {
                            var error = "There is no update-script for your specific OS.";
                            UpdaterLogger.LogError($"Update script execution failed: {error}");
                            throw new Exception(error);
                        }

                        UpdaterLogger.LogInfo($"Using update script: {updateFile}");
                        EnsureExecutablePermissions(updateFile);

                        process.StartInfo.WorkingDirectory = destinationPath;
                        process.StartInfo.FileName = updateFile;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.RedirectStandardError = false;
                        process.StartInfo.UseShellExecute = false;
                        
                        UpdaterLogger.LogInfo($"Executing update script from directory: {destinationPath}");
                        process.Start();
                        
                        UpdaterLogger.LogInfo("Update script started successfully");
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogError("Failed to start update script", ex);
                        Console.WriteLine($"An error occurred trying to start \"update-script\":\n{ex.Message}");
                        throw;
                    }
                }
                
                UpdaterLogger.LogInfo("Update installation initialized successfully");
                OnReleaseInstallInitialized(new ReleaseEventArgs(latestRepoVersion, downloadPath));
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Update installation failed", ex);
                OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, ex.Message));
            }
        }

        /// <summary>
        /// Fetches the last available release versions from GitHub (up to 4), excluding the current version.
        /// If beta mode is active, beta versions are included; otherwise only stable (v-prefixed) versions.
        /// Does not use RetryHelper to avoid triggering the global loading overlay.
        /// </summary>
        public static async Task<List<string>> FetchAvailableVersionsAsync(int maxCount = 4)
        {
            UpdaterLogger.LogInfo($"Fetching available versions (beta={IsBetaTester}, max={maxCount})");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
            client.Timeout = TimeSpan.FromSeconds(requestTimeout);

            var result = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases");
            var releases = JsonDocument.Parse(result).RootElement.EnumerateArray();
            var tags = new List<string>();

            foreach (var release in releases)
            {
                if (release.GetProperty("draft").GetBoolean())
                    continue;

                var tag = release.GetProperty("tag_name").GetString();
                if (string.IsNullOrEmpty(tag))
                    continue;

                // Skip current version
                if (string.Equals(tag, version, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool isBeta = tag.StartsWith("b", StringComparison.OrdinalIgnoreCase);

                if (!IsBetaTester && isBeta)
                    continue;

                tags.Add(tag);
            }

            // Sort descending (newest first)
            tags.Sort((a, b) => CompareVersions(b, a));

            var versions = tags.Take(maxCount).ToList();
            UpdaterLogger.LogInfo($"Found {versions.Count} available version(s)");
            return versions;
        }

        /// <summary>
        /// Downloads and installs a specific version (used for version rollback).
        /// </summary>
        public static void RollbackToVersion(string targetVersion)
        {
            if (string.IsNullOrWhiteSpace(targetVersion))
            {
                UpdaterLogger.LogWarning("Rollback requested but no target version specified");
                return;
            }

            UpdaterLogger.LogInfo($"Starting rollback to version {targetVersion}");

            try
            {
                var appSourceFile = GetAppFileByOS();
                if (string.IsNullOrEmpty(appSourceFile))
                {
                    var error = "There are no releases for your specific OS.";
                    UpdaterLogger.LogError($"Rollback failed: {error}");
                    throw new InvalidOperationException(error);
                }

                UpdaterLogger.LogInfo($"Target file for current OS: {appSourceFile}");

                destinationPath = Helper.GetAppBasePath();
                downloadPath = Path.Join(destinationPath, appDestination, appSourceFile);
                downloadDirectory = Path.GetDirectoryName(downloadPath);

                string downloadUrl = appSourceUrl + "/" + targetVersion + "/" + appSourceFile;

                UpdaterLogger.LogInfo($"Download URL: {downloadUrl}");
                UpdaterLogger.LogInfo($"Download path: {downloadPath}");

                UpdaterLogger.LogInfo("Cleaning up existing download directory");
                Helper.RemoveDirectory(downloadDirectory, true);

                // Store target version so download-completed handler can reference it
                latestRepoVersion = targetVersion;

                UpdaterLogger.LogInfo("Starting rollback download");
                OnReleaseDownloadStarted(new ReleaseEventArgs(targetVersion, targetVersion));

                var webClient = new WebClient();
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadCompleted;
                webClient.DownloadFileAsync(new Uri(downloadUrl), downloadPath);
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Failed to initialize rollback", ex);
                Helper.RemoveDirectory(downloadDirectory);
                OnReleaseDownloadFailed(new ReleaseEventArgs(targetVersion, ex.Message));
            }
        }

        /// <summary>
        /// Compares two version strings (supports formats like "v1.2.3", "b1.0.8", "v1.2.3.0", "b1.2.3.1")
        /// Strips the v/b prefix and compares numerically.
        /// </summary>
        /// <returns>Negative if version1 &lt; version2, 0 if equal, positive if version1 &gt; version2</returns>
        internal static int CompareVersions(string version1, string version2)
        {
            var cleanVersion1 = version1.TrimStart('v', 'b', 'V', 'B');
            var cleanVersion2 = version2.TrimStart('v', 'b', 'V', 'B');

            if (Version.TryParse(cleanVersion1, out var v1) && Version.TryParse(cleanVersion2, out var v2))
            {
                return v1.CompareTo(v2);
            }

            UpdaterLogger.LogWarning($"Version parsing failed, using string comparison: {version1} vs {version2}");
            return string.Compare(cleanVersion1, cleanVersion2, StringComparison.OrdinalIgnoreCase);
        }

        private static void OnNoNewReleaseFound(ReleaseEventArgs e)
        {
            NoNewReleaseFound?.Invoke(typeof(Updater), e);
        }

        private static void OnNewReleaseFound(ReleaseEventArgs e)
        {
            NewReleaseFound?.Invoke(typeof(Updater), e);
        }

        private static void OnReleaseInstallInitialized(ReleaseEventArgs e)
        {
            ReleaseInstallInitialized?.Invoke(typeof(Updater), e);
        }

        private static void OnReleaseDownloadStarted(ReleaseEventArgs e)
        {
            ReleaseDownloadStarted?.Invoke(typeof(Updater), e);
        }

        private static void OnReleaseDownloadFailed(ReleaseEventArgs e)
        {
            ReleaseDownloadFailed?.Invoke(typeof(Updater), e);
        }

        private static void OnReleaseDownloadProgressed(DownloadProgressChangedEventArgs e)
        {
            ReleaseDownloadProgressed?.Invoke(typeof(Updater), e);
        }

    }
}
