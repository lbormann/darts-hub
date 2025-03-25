﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        public static readonly string version = "b0.12.16";
        

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
        private const int requestTimeout = 4;

        private static string destinationPath = String.Empty;
        private static string downloadPath = String.Empty;
        private static string downloadDirectory = String.Empty;




        // METHODS
        public static bool IsBetaTester { get; set; } = false;

        public static async void CheckNewVersion()
        {
            if (IsBetaTester)
            {
                await CheckNewBetaVersion();
            }
            else
            {
                await CheckNewStableVersion();
            }
        }

        private static async Task CheckNewStableVersion()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
                client.Timeout = TimeSpan.FromSeconds(requestTimeout);
                var result = await client.GetStringAsync(appSourceUrlLatest);
                int tagNameIndex = result.IndexOf("tag_name");
                if (tagNameIndex == -1) throw new ArgumentException("github-tagName-Index not found");
                result = result.Substring(tagNameIndex);
                int tagNameCommaIndex = result.IndexOf(',');
                if (tagNameCommaIndex == -1) throw new ArgumentException("github-tagNameComma-Index not found");
                result = result.Substring("tag_name: \"".Length, tagNameCommaIndex - "tag_name: \"".Length);
                var latestGithubVersion = result.Replace("\"", "");

                if (version != latestGithubVersion)
                {
                    latestRepoVersion = latestGithubVersion;
                    var changelog = await Helper.AsyncHttpGet(appSourceUrlChangelog, requestTimeout);
                    OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, changelog));
                }
                else
                {
                    OnNoNewReleaseFound(new ReleaseEventArgs(latestGithubVersion, string.Empty));
                }
            }
            catch (Exception ex)
            {
                OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", ex.Message));
            }
        }

        private static async Task CheckNewBetaVersion()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
                client.Timeout = TimeSpan.FromSeconds(requestTimeout);
                var result = await client.GetStringAsync("https://api.github.com/repos/lbormann/darts-hub/releases");
                var releases = JsonDocument.Parse(result).RootElement.EnumerateArray();
                JsonElement? latestBetaRelease = null;

                foreach (var release in releases)
                {
                    if (release.GetProperty("prerelease").GetBoolean())
                    {
                        latestBetaRelease = release;
                        break;
                    }
                }

                if (latestBetaRelease.HasValue)
                {
                    var latestBetaVersion = latestBetaRelease.Value.GetProperty("tag_name").GetString();
                    if (version != latestBetaVersion)
                    {
                        latestRepoVersion = latestBetaVersion;
                        var changelog = await Helper.AsyncHttpGet(appSourceUrlChangelog, requestTimeout);
                        OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, changelog));
                    }
                    else
                    {
                        OnNoNewReleaseFound(new ReleaseEventArgs(latestBetaVersion, string.Empty));
                    }
                }
                else
                {
                    OnNoNewReleaseFound(new ReleaseEventArgs("vx.x.x", "No beta releases found."));
                }
            }
            catch (Exception ex)
            {
                OnReleaseDownloadFailed(new ReleaseEventArgs("vx.x.x", ex.Message));
            }
        }

        public static void UpdateToNewVersion()
        {
            if (!string.IsNullOrEmpty(latestRepoVersion))
            {
                try
                {
                    var appSourceFile = GetAppFileByOS();
                    if (String.IsNullOrEmpty(appSourceFile)) throw new Exception("There are no releases for your specific OS.");

                    destinationPath = Helper.GetAppBasePath();
                    downloadPath = Path.Join(destinationPath, appDestination, appSourceFile);
                    downloadDirectory = Path.GetDirectoryName(downloadPath);

                    string downloadUrl = appSourceUrl + "/" + latestRepoVersion + "/" + appSourceFile;

                    // Removes existing download-directory and creates a new one
                    Helper.RemoveDirectory(downloadDirectory, true);

                    // Inform subscribers about a pending download
                    OnReleaseDownloadStarted(new ReleaseEventArgs(latestRepoVersion, latestRepoVersion));

                    // Start the download
                    var webClient = new WebClient();
                    webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += WebClient_DownloadCompleted;
                    webClient.DownloadFileAsync(new Uri(downloadUrl), downloadPath);
                }
                catch (Exception ex)
                {
                    Helper.RemoveDirectory(downloadDirectory);
                    OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, ex.Message));
                }
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
            string updateFile = String.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    updateFile = "update.sh";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    updateFile = "update.bat";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    updateFile = "update.bat";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    updateFile = "update.bat";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    updateFile = "update.bat";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    updateFile = "update.sh";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    updateFile = "update.sh";
                }
            }
            return updateFile;
        }

        private static string GetAppFileByOS()
        {
            string appFile = String.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    appFile = "darts-hub-linux-X64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {

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
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                {
                    appFile = "darts-hub-macOS-X64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                {

                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    appFile = "darts-hub-macOS-ARM64.zip";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {

                }
            }
            return appFile;
        }

        private static void EnsureExecutablePermissions(string updateFile)
        {
            var scriptPath = Path.Combine(destinationPath, updateFile);
            if (updateFile.EndsWith(".sh"))
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
        }

        private static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnReleaseDownloadProgressed(e);
        }

        private static void WebClient_DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null)
                {
                    OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, e.Error.Message));
                    return;
                }

                var wc = sender as WebClient;


                ZipFile.ExtractToDirectory(downloadPath, Path.GetDirectoryName(downloadPath), true);
                File.Delete(downloadPath);

                // start update-batch, that waits until files are accessible; then copy new release to assembly location and delete temp files
                using (var process = new Process())
                {
                    try
                    {
                        var updateFile = GetUpdateFileByOs();
                        if (String.IsNullOrEmpty(updateFile)) throw new Exception("There is no update-script for your specific OS.");

                        EnsureExecutablePermissions(updateFile);

                        process.StartInfo.WorkingDirectory = destinationPath;
                        process.StartInfo.FileName = updateFile;
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.RedirectStandardError = false;
                        process.StartInfo.UseShellExecute = false;
                        process.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred trying to start \"update-script\":\n{ex.Message}");
                        throw;
                    }
                }
                OnReleaseInstallInitialized(new ReleaseEventArgs(latestRepoVersion, downloadPath));
            }
            catch (Exception ex)
            {
                OnReleaseDownloadFailed(new ReleaseEventArgs(latestRepoVersion, ex.Message));
            }
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
