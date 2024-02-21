using autodarts_desktop.control;
using System;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Diagnostics;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace autodarts_desktop.model
{
    /// <summary>
    /// App that can be downloaded from the internet
    /// </summary>
    public class AppDownloadable : AppBase
    {

        // ATTRIBUTES

        public string DownloadUrl { get; set; }



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
                                string? descriptionShort = null,
                                string? descriptionLong = null,
                                bool runAsAdmin = false,
                                bool chmod = true,
                                ProcessWindowStyle? startWindowState = null,
                                Configuration? configuration = null) 
            : base(name: name,
                   customName: customName,
                        helpUrl: helpUrl,
                        descriptionShort: descriptionShort,
                        descriptionLong: descriptionLong,
                        runAsAdmin: runAsAdmin,
                        chmod: chmod,
                        startWindowState: startWindowState,
                        configuration: configuration
                        )
        {
            DownloadUrl = downloadUrl;
            
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
                if (urlFileSize == localFileSize) return false;

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

        private void WebClient_DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Error != null) throw e.Error;

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
                OnDownloadFinished(new AppEventArgs(this, "success"));
                if(IsReadyToRun()) Run(runtimeArguments);
            }
            catch (Exception ex)
            {
                Helper.RemoveDirectory(downloadPath);
                OnDownloadFailed(new AppEventArgs(this, ex.Message));
            }
        }



        protected string? GetDownloadExecutable()
        {
            return Helper.SearchExecutable(downloadPath);
        }

        protected override string? SetRunExecutable()
        {
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
