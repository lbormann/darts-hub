using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using autodarts_desktop.model;

namespace autodarts_desktop.control
{

    /// <summary>
    /// Manage new releases and initialize update-process
    /// </summary>
    public static class Updater
    {
        // ATTRIBUTES

        // Increase for new build ..
        public static readonly string version = "v1.2.22";

        
        public static event EventHandler<ReleaseEventArgs>? NewReleaseFound;
        public static event EventHandler<ReleaseEventArgs>? ReleaseInstallInitialized;
        public static event EventHandler<ReleaseEventArgs>? ReleaseDownloadStarted;
        public static event EventHandler<ReleaseEventArgs>? ReleaseDownloadFailed;
        public static event EventHandler<DownloadProgressChangedEventArgs>? ReleaseDownloadProgressed;

        private static string latestRepoVersion = string.Empty;
        private const string appSourceUrl = "https://github.com/Semtexmagix/autodarts-desktop/releases/download";
        private const string appSourceUrlLatest = "https://api.github.com/repos/Semtexmagix/autodarts-desktop/releases/latest";
        private const string appSourceFile = "autodarts-desktop.zip";
        private const string appDestination = "updates";
        private const string requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";

        private static string destinationPath = String.Empty;
        private static string downloadPath = String.Empty;
        private static string downloadDirectory = String.Empty;



        // METHODS

        public static async void CheckNewVersion()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", requestUserAgent);
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
                    OnNewReleaseFound(new ReleaseEventArgs(latestRepoVersion, string.Empty));
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
                destinationPath = Helper.GetAppBasePath();
                downloadPath = Path.Join(destinationPath, appDestination, appSourceFile);
                downloadDirectory = Path.GetDirectoryName(downloadPath);

                try
                {
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
                        process.StartInfo.WorkingDirectory = destinationPath;
                        process.StartInfo.FileName = "update.bat";
                        process.StartInfo.RedirectStandardOutput = false;
                        process.StartInfo.RedirectStandardError = false;
                        process.StartInfo.UseShellExecute = true;
                        process.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred trying to start \"update.bat\":\n{ex.Message}");
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
