using darts_hub.control;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace darts_hub.model
{
    /// <summary>
    /// Main functions for using an app
    /// </summary>
    public abstract class AppBase : IApp, INotifyPropertyChanged
    {

        // ATTRIBUTES
        public const int MaxAppMonitorEntries = 300;

        public string Name { get; set; }
        public string CustomName { get; set; }
        public string? HelpUrl { get; set; }
        public string? ChangelogUrl { get; set; }
        public string? DescriptionShort { get; set; }
        public string? DescriptionLong { get; private set; }
        public bool RunAsAdmin { get; private set; }
        public bool Chmod { get; set; }
        public ProcessWindowStyle StartWindowState { get; private set; }
        public Configuration? Configuration { get; protected set; }

        public event EventHandler<AppEventArgs>? AppConfigurationRequired;

        [JsonIgnore]
        public Argument? ArgumentRequired { get; private set; }

        [JsonIgnore]
        public string AppConsoleStdOutput { get; private set; }

        [JsonIgnore]
        public string AppConsoleStdError { get; private set; }

        
        private bool _appRunningState;
        [JsonIgnore]
        public bool AppRunningState
        {
            get => _appRunningState;
            private set
            {
                if (_appRunningState != value)
                {
                    _appRunningState = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public int AppMonitorEntries { get; private set; }


        private string _appMonitor;
        [JsonIgnore]
        public string AppMonitor
        {
            get => _appMonitor;
            private set
            {
                if (_appMonitor != value)
                {
                    _appMonitor = value;
                    AppMonitorAvailable = _appMonitor != String.Empty ? true : false;
                    OnPropertyChanged();
                }
            }
        }



        
        private bool _appMonitorAvailable;
        [JsonIgnore]
        public bool AppMonitorAvailable
        {
            get => _appMonitorAvailable;
            private set
            {
                if (_appMonitorAvailable != value)
                {
                    _appMonitorAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        private string? executable;
        private readonly ProcessWindowStyle DefaultStartWindowState = ProcessWindowStyle.Minimized;
        protected Dictionary<string, string>? runtimeArguments;
        private TaskCompletionSource<bool> eventHandled;
        private Process process;
        private const int defaultProcessId = 0;
        private int processId;
        public event PropertyChangedEventHandler PropertyChanged;





        // METHODS

        public AppBase(string name,
                        string? customName,
                        string? helpUrl,
                        string? changelogUrl,
                        string? descriptionShort,
                        string? descriptionLong,
                        bool runAsAdmin,
                        bool chmod,
                        ProcessWindowStyle? startWindowState,
                        Configuration? configuration = null
            )
        {
            Name = name;
            CustomName = customName;
            HelpUrl = helpUrl;
            ChangelogUrl = changelogUrl;
            DescriptionShort = descriptionShort;
            DescriptionLong = descriptionLong;
            RunAsAdmin = runAsAdmin;
            Chmod = chmod;
            StartWindowState = (ProcessWindowStyle)(startWindowState == null ? DefaultStartWindowState : startWindowState);
            Configuration = configuration;

            processId = defaultProcessId;
            AppRunningState = false;
        }



        public bool Run(Dictionary<string, string>? runtimeArguments = null)
        {
            this.runtimeArguments = runtimeArguments;
            executable = SetRunExecutable();
            if (IsRunning()) return true;
            if (Install()) return false;
            RunProcess(runtimeArguments);
            return true;
        }

        public bool ReRun(Dictionary<string, string>? runtimeArguments = null)
        {
            if (!IsRunning()) return false;
            Close();
            return Run(runtimeArguments);
        }

        public bool IsInstalled()
        {
            executable = SetRunExecutable();
            return String.IsNullOrEmpty(executable) ? false : File.Exists(executable);
        }

        public bool IsRunning()
        {
            return AppRunningState;
            //return process != null && !process.HasExited;
        }

        public bool IsConfigurationChanged()
        {
            return Configuration != null ? Configuration.IsChanged() : false;
        }

        public void Close()
        {
            executable = SetRunExecutable();

            //if(!IsRunning()) return;
            if (IsRunnable())
            {
                try
                {
                    //Console.WriteLine(Name + " tries to exit");
                    
                    if(process != null)
                    {
                        process.CloseMainWindow();
                        process.Close();
                    }
                    if (processId != defaultProcessId)
                    {
                        Helper.KillProcess(processId);
                    }
                    if (executable != null)
                    {
                        Helper.KillProcess(executable);
                    }
                    AppRunningState = false;
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Can't close {executable}: {ex.Message}");
                }
            }
        }

        public abstract bool Install();

        public abstract bool IsConfigurable();

        public abstract bool IsInstallable();


        private async Task RunProcess(Dictionary<string, string>? runtimeArguments = null)
        {
            if (!IsRunnable()) return;

            if (String.IsNullOrEmpty(executable)) return;
            var arguments = ComposeArguments(this, runtimeArguments);
            //if (arguments == null) return;



            eventHandled = new TaskCompletionSource<bool>();

            try
            {
                AppConsoleStdOutput = String.Empty;
                AppConsoleStdError = String.Empty;
                AppMonitor = String.Empty;

                // For testing purposes
                //AppConsoleStdOutput = arguments;

                bool isUri = Uri.TryCreate(executable, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                process = new Process();
                process.StartInfo.WindowStyle = StartWindowState;
                process.EnableRaisingEvents = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Exited += (sender, e) =>
                {
                    //Console.WriteLine(
                    //    $"Exit time    : {process.ExitTime}\n" +
                    //    $"Exit code    : {process.ExitCode}\n" +
                    //    $"Elapsed time : {Math.Round((process.ExitTime - process.StartTime).TotalMilliseconds)}");
                    //Console.WriteLine("Process " + Name + " exited");
                    processId = defaultProcessId;
                    if (!isUri) {
                        AppRunningState = false;
                    }
                    eventHandled.TrySetResult(true);
                };
                process.OutputDataReceived += (sender, e) =>
                {   
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        if (AppMonitorEntries >= MaxAppMonitorEntries)
                        {
                            AppConsoleStdOutput = String.Empty;
                            AppConsoleStdError = String.Empty;
                            AppMonitorEntries = 0;
                        }
                        AppConsoleStdOutput += e.Data + Environment.NewLine;
                        AppMonitor = AppConsoleStdOutput + Environment.NewLine + Environment.NewLine + AppConsoleStdError;
                        AppMonitorEntries++;
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        if (AppMonitorEntries >= MaxAppMonitorEntries)
                        {
                            AppConsoleStdError = String.Empty;
                            AppConsoleStdOutput = String.Empty;
                            AppMonitorEntries = 0;
                        }
                        AppConsoleStdError += e.Data + Environment.NewLine;
                        AppMonitor = AppConsoleStdOutput + Environment.NewLine + Environment.NewLine + AppConsoleStdError;
                        AppMonitorEntries++;
                    }
                };
                process.StartInfo.FileName = executable;
                process.StartInfo.Arguments = arguments == null ? String.Empty : arguments;

                

                if (isUri)
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                }
                else 
                {
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executable);
                }
                

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (RunAsAdmin) process.StartInfo.Verb = "runas";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (!isUri && Chmod) EnsureExecutablePermissions(executable);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (!isUri && Chmod) EnsureExecutablePermissions(executable);
                }

                process.Start();
                if(process.StartInfo.RedirectStandardOutput == true)
                {
                    process.BeginOutputReadLine();
                }
                if (process.StartInfo.RedirectStandardError == true)
                {
                    process.BeginErrorReadLine();
                }
                processId = process.Id;
                AppRunningState = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred trying to start \"{executable}\" with \"{arguments}\":\n{ex.Message}");
                throw;
            }

            // Wait for Exited event
            await Task.WhenAny(eventHandled.Task);
            
        }

        private void EnsureExecutablePermissions(string scriptPath)
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

        protected virtual bool IsRunnable()
        {
            return true;
        }

        protected abstract string? SetRunExecutable();

        protected string? ComposeArguments(AppBase app, Dictionary<string, string>? runtimeArguments = null)
        {
            try
            {
                var ret = IsConfigurable() ? Configuration.GenerateArgumentString(app, runtimeArguments) : "";
                ArgumentRequired = null;
                return ret;
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith(Configuration.ArgumentErrorKey))
                {
                    string invalidArgumentErrorMessage = ex.Message.Substring(Configuration.ArgumentErrorKey.Length, ex.Message.Length - Configuration.ArgumentErrorKey.Length);
                    ArgumentRequired = (Argument)ex.Data["argument"];
                    invalidArgumentErrorMessage += " - Please correct it.";
                    OnAppConfigurationRequired(new AppEventArgs(this, invalidArgumentErrorMessage));
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }
   
        protected virtual void OnAppConfigurationRequired(AppEventArgs e)
        {
            AppConfigurationRequired?.Invoke(this, e);
        }



        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
