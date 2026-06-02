using darts_hub.control;
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Diagnostics;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Linq;

namespace darts_hub.model
{
    /// <summary>
    /// App that can be downloaded from the internet
    /// </summary>
    public class AppDownloadable : AppBase
    {

        // ATTRIBUTES

        public string DownloadUrl { get; set; }

        /// <summary>
        /// When true, an additional signature manifest file
        /// (named "manifest.sig.json-{filename}" alongside the main download in the
        /// release) is downloaded together with the main file and stored next to it
        /// as "manifest.sig.json".
        /// </summary>
        public bool DownloadsManifest { get; set; }

        public event EventHandler<AppEventArgs>? DownloadStarted;
        public event EventHandler<AppEventArgs>? DownloadFinished;
        public event EventHandler<AppEventArgs>? DownloadFailed;
        public event EventHandler<DownloadProgressChangedEventArgs>? DownloadProgressed;
        

        protected string downloadPath;
        protected string downloadPathFile;
        private bool skipRun;



        // METHODS

        public AppDownloadable(string downloadUrl,
                                string name,
                                string? customName = null,
                                string? helpUrl = null,
                                string? changelogUrl = null,
                                string? descriptionShort = null,
                                string? descriptionLong = null,
                                bool runAsAdmin = false,
                                bool chmod = true,
                                ProcessWindowStyle? startWindowState = null,
                                Configuration? configuration = null,
                                bool downloadsManifest = false) 
            : base(name: name,
                    customName: customName,
                    helpUrl: helpUrl,
                    changelogUrl: changelogUrl,
                    descriptionShort: descriptionShort,
                    descriptionLong: descriptionLong,
                    runAsAdmin: runAsAdmin,
                    chmod: chmod,
                    startWindowState: startWindowState,
                    configuration: configuration
                    )
        {
            DownloadUrl = downloadUrl;
            DownloadsManifest = downloadsManifest;

            GeneratePaths();
        }



        public override bool Install()
        {
            try
            {
                if (Helper.DirectoryOrFileStartsWith(downloadPath, "my_version"))
                {
                    return false;
                }
            }
            catch ( Exception ex)
            {
                return false;
            }

            try
            {
                var urlFileSize = Helper.GetFileSizeByUrl(DownloadUrl);
                var localFileSize = Helper.GetFileSizeByLocal(downloadPathFile);

                skipRun = localFileSize == -2 ? false : true;

                // Console.WriteLine($"url-file: {urlFileSize}  - local-file: {downloadPathFile}");
                if (urlFileSize == localFileSize)
                {
                    // Main file is up-to-date but make sure the manifest signature is present
                    if (DownloadsManifest)
                    {
                        var manifestTargetPath = Path.Combine(downloadPath, "manifest.sig.json");
                        if (!File.Exists(manifestTargetPath))
                        {
                            try { DownloadManifestSignature(); } catch { /* non-fatal */ }
                        }
                    }
                    return false;
                }

                // removes existing app and creates a new directory
                Helper.RemoveDirectory(downloadPath, true);

                // inform subscribers about a pending download
                OnDownloadStarted(new AppEventArgs(this, ""));

                // start the download
                var webclient = new WebClient();
                webclient.DownloadFileCompleted += WebClient_DownloadCompleted;
                webclient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webclient.DownloadFileAsync(new Uri(DownloadUrl), downloadPathFile);
                return true;
            }
            catch (Exception ex)
            {
                OnDownloadFailed(new AppEventArgs(this, ex.Message)); 
            }
            return false;
        }

        public override bool IsConfigurable()
        {
            return Configuration != null;
        }

        public override bool IsInstallable()
        {
            return true;
        }





        private void GeneratePaths()
        {
            downloadPath = Path.Join(Helper.GetAppBasePath(), Name);
            string appFileName = Helper.GetFileNameByUrl(DownloadUrl);
            downloadPathFile = Path.Join(downloadPath, appFileName);
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressed(e);
        }

        private void DownloadManifestSignature()
        {
            try
            {
                var assetFileName = Helper.GetFileNameByUrl(DownloadUrl);
                var manifestAssetName = $"manifest.sig.json-{assetFileName}";
                var lastSlash = DownloadUrl.LastIndexOf('/');
                if (lastSlash < 0) return;

                var manifestUrl = DownloadUrl.Substring(0, lastSlash + 1) + manifestAssetName;
                var manifestTargetPath = Path.Combine(downloadPath, "manifest.sig.json");

                Directory.CreateDirectory(downloadPath);
                if (File.Exists(manifestTargetPath)) File.Delete(manifestTargetPath);

                using var client = new WebClient();
                client.DownloadFile(new Uri(manifestUrl), manifestTargetPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{Name}] Failed to download manifest.sig.json: {ex.Message}");
                throw;
            }
        }

        private void WebClient_DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null) throw e.Error;

                // Download accompanying signature manifest if requested
                if (DownloadsManifest)
                {
                    DownloadManifestSignature();
                }

                // Extract download if zip-file
                var ext = Path.GetExtension(downloadPathFile).ToLower();
                if (ext == ".zip")
                {
                    ZipFile.ExtractToDirectory(downloadPathFile, downloadPath);
                }
                else if (ext == ".gz")
                {
                    using (FileStream stream = File.OpenRead(downloadPathFile))
                    using (var reader = ReaderFactory.Open(stream))
                    {
                        while (reader.MoveToNextEntry())
                        {
                            if (!reader.Entry.IsDirectory)
                            {
                                reader.WriteEntryToDirectory(downloadPath,
                                    new ExtractionOptions()
                                    {
                                        ExtractFullPath = true,
                                        Overwrite = true
                                    });
                            }
                        }
                    }
                }

                PreparePixelitTemplates();

                OnDownloadFinished(new AppEventArgs(this, "success"));
                if(IsReadyToRun()) Run(runtimeArguments);
            }
            catch (Exception ex)
            {
                Helper.RemoveDirectory(downloadPath);
                OnDownloadFailed(new AppEventArgs(this, ex.Message));
            }
        }

        private void PreparePixelitTemplates()
        {
            if (!string.Equals(Name, "darts-pixelit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var templatesPath = Path.Combine(Helper.GetAppBasePath(), "Pixelit-templates");
                PixelitTemplateDownloader.EnsureTemplatesDownloaded(templatesPath);

                var templateArg = Configuration?.Arguments?.FirstOrDefault(a => a.Name.Equals("TP", StringComparison.OrdinalIgnoreCase));
                if (templateArg != null)
                {
                    if (string.IsNullOrWhiteSpace(templateArg.Value) || !Directory.Exists(templateArg.Value))
                    {
                        templateArg.Value = templatesPath;
                        templateArg.IsValueChanged = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Pixelit] Failed to prepare templates: {ex.Message}");
            }
        }



        protected string? GetDownloadExecutable()
        {
            return Helper.SearchExecutable(downloadPath);
        }

        public string? GetExecutablePath()
        {
            return GetDownloadExecutable();
        }

        protected override string? SetRunExecutable()
        {
            if (string.Equals(Name, "darts-pixelit", StringComparison.OrdinalIgnoreCase))
            {
                PreparePixelitTemplates();
            }

            return GetDownloadExecutable();
        }

        protected virtual bool IsReadyToRun()
        {
            if (skipRun)
            {
                skipRun = !skipRun;
                return false;
            }
            else
            {
                skipRun = !skipRun;
                return true;
            }
        }

        protected virtual void OnDownloadStarted(AppEventArgs e)
        {
            DownloadStarted?.Invoke(this, e);
        }

        protected virtual void OnDownloadFinished(AppEventArgs e)
        {
            DownloadFinished?.Invoke(this, e);
        }

        protected virtual void OnDownloadFailed(AppEventArgs e)
        {
            DownloadFailed?.Invoke(this, e);
        }

        protected virtual void OnDownloadProgressed(DownloadProgressChangedEventArgs e)
        {
            DownloadProgressed?.Invoke(this, e);
        }




    }
}
