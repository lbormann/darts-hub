using System;
using System.Diagnostics;
using System.IO;

namespace autodarts_desktop.model
{
    /// <summary>
    /// App that can be downloaded from the internet and gets installed after download
    /// </summary>
    public class AppInstallable : AppDownloadable
    {

        // ATTRIBUTES

        public string Executable { get; private set; }
        public string? DefaultPathExecutable { get; private set; }
        public bool StartsAfterInstallation { get; private set; }
        public bool RunAsAdminInstall { get; private set; }
        public bool IsService { get; private set; }

        public event EventHandler<AppEventArgs>? InstallStarted;
        public event EventHandler<AppEventArgs>? InstallFinished;
        public event EventHandler<AppEventArgs>? InstallFailed;






        // METHODS

        public AppInstallable(string executable,
                                string downloadUrl,
                                string name,
                                string? defaultPathExecutable = null,
                                bool startsAfterInstallation = false,
                                bool runAsAdminInstall = false,
                                bool isService = false,
                                string? helpUrl = null,
                                string? descriptionShort = null,
                                string? descriptionLong = null,
                                bool runAsAdmin = false,
                                ProcessWindowStyle? startWindowState = null,
                                Configuration? configuration = null)
    : base(downloadUrl: downloadUrl, 
              name: name,
              helpUrl: helpUrl,
              descriptionShort: descriptionShort,
              descriptionLong: descriptionLong,
              runAsAdmin: runAsAdmin,
              startWindowState: startWindowState,
              configuration: configuration
              )
        {
            Executable = executable;
            DefaultPathExecutable = defaultPathExecutable;
            StartsAfterInstallation = startsAfterInstallation;
            RunAsAdminInstall = runAsAdminInstall;
            IsService = isService;
        }




           

         
        protected void InstallProcess_Exited(object? sender, EventArgs e)
        {
            var installProcess = (Process?)sender;
            if (installProcess == null) throw new FileNotFoundException("install-process not available");
            if (installProcess.ExitCode == 0)
            {
                OnInstallFinished(new AppEventArgs(this, "success"));

                if(!StartsAfterInstallation) Run(runtimeArguments);
            }
            else
            {
                OnInstallFailed(new AppEventArgs(this, $"error: {installProcess.ExitCode}"));
            }
        }

        protected override bool IsRunnable()
        {
            return !IsService;
        }

        protected override bool IsReadyToRun()
        {
            return false;
        }

        protected override void OnDownloadFinished(AppEventArgs e)
        {
            base.OnDownloadFinished(e);

            OnInstallStarted(e);

            try
            {
                string? downloadExecutable = GetDownloadExecutable();
                if (downloadExecutable == null) throw new FileNotFoundException($"Installer for {Name} not found");

                Process installProcess = new Process();
                {
                    installProcess.StartInfo.FileName = downloadExecutable;
                    installProcess.StartInfo.Arguments = " /qb ALLUSERS=1";
                    installProcess.StartInfo.RedirectStandardOutput = false;
                    installProcess.StartInfo.RedirectStandardError = false;
                    installProcess.StartInfo.UseShellExecute = true;
                    if (RunAsAdminInstall) installProcess.StartInfo.Verb = "runas";
                    installProcess.EnableRaisingEvents = true;
                    installProcess.Exited += InstallProcess_Exited;
                    installProcess.Start();
                    installProcess.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                OnInstallFailed(new AppEventArgs(this, ex.Message));
            }
        }

        protected override string? SetRunExecutable()
        {
            var pathToExecutableProb = Path.Combine(DefaultPathExecutable, Executable);

            // Check if executable exists by given default path
            if (!String.IsNullOrEmpty(DefaultPathExecutable) &&
                Directory.Exists(DefaultPathExecutable) &&
                File.Exists(pathToExecutableProb))
            {
                return pathToExecutableProb;
            }

            return null;
            // else .. browse file-system (TODO: rechte probleme)
            //return Helper.SearchExecutableOnDrives(Executable);
        }

        protected virtual void OnInstallStarted(AppEventArgs e)
        {
            InstallStarted?.Invoke(this, e);
        }

        protected virtual void OnInstallFinished(AppEventArgs e)
        {
            InstallFinished?.Invoke(this, e);
        }

        protected virtual void OnInstallFailed(AppEventArgs e)
        {
            InstallFailed?.Invoke(this, e);
        }




    }
}
