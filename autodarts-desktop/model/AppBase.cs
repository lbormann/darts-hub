using autodarts_desktop.control;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace autodarts_desktop.model
{
    /// <summary>
    /// Main functions for using an app
    /// </summary>
    public abstract class AppBase : IApp
    {

        // ATTRIBUTES

        public string Name { get; private set; }
        public string? HelpUrl { get; set; }
        public string? DescriptionShort { get; private set; }
        public string? DescriptionLong { get; private set; }
        public bool RunAsAdmin { get; private set; }
        public ProcessWindowStyle StartWindowState { get; private set; }
        public Configuration? Configuration { get; protected set; }

        public event EventHandler<AppEventArgs>? AppConfigurationRequired;

        [JsonIgnore]
        public Argument? ArgumentRequired { get; private set; }


        private string? executable;
        private readonly ProcessWindowStyle DefaultStartWindowState = ProcessWindowStyle.Minimized;
        protected Dictionary<string, string>? runtimeArguments;
        private TaskCompletionSource<bool> eventHandled;
        private Process process;
        private const int defaultProcessId = 0;
        private int processId;





        // METHODS


        public AppBase(string name,
                        string? helpUrl,
                        string? descriptionShort,
                        string? descriptionLong,
                        bool runAsAdmin,
                        ProcessWindowStyle? startWindowState,
                        Configuration? configuration = null
            )
        {
            Name = name;
            HelpUrl = helpUrl;
            DescriptionShort = descriptionShort;
            DescriptionLong = descriptionLong;
            RunAsAdmin = runAsAdmin;
            StartWindowState = (ProcessWindowStyle)(startWindowState == null ? DefaultStartWindowState : startWindowState);
            Configuration = configuration;

            processId = defaultProcessId;
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

        

        public bool IsInstalled()
        {
            executable = SetRunExecutable();
            return String.IsNullOrEmpty(executable) ? false : File.Exists(executable);
        }

        public bool IsRunning()
        {
            return process != null && !process.HasExited;
        }

        public void Close()
        {
            if(!IsRunning()) return;
            if (IsRunnable())
            {
                //Console.WriteLine(Name + " tries to exit");
                process.CloseMainWindow();

                if (processId != defaultProcessId)
                {
                    try
                    {
                        Helper.KillProcess(processId);
                    }
                    catch
                    {
                    }
                }
                if (executable != null)
                {
                    Helper.KillProcess(executable);
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
            if (arguments == null) return;

            eventHandled = new TaskCompletionSource<bool>();

            try
            {
                process = new Process();
                process.StartInfo.WindowStyle = StartWindowState;
                process.EnableRaisingEvents = true;
                process.Exited += process_Exited;

                bool isUri = Uri.TryCreate(executable, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!isUri) process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executable);


                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.FileName = executable;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                    if (RunAsAdmin) process.StartInfo.Verb = "runas";
                    process.Start();
                    processId = process.Id;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    process.StartInfo.UseShellExecute = false;
                    TryStartLinuxTerminalEmulator(process, new[] { "konsole", "xterm", "lxterminal", "xfce4-terminal", "gnome-terminal"}, executable, arguments);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    process.StartInfo.UseShellExecute = false;
                    TryStartMacTerminalEmulator(process, executable, arguments);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred trying to start \"{executable}\" with \"{arguments}\":\n{ex.Message}");
                throw;
            }

            // Wait for Exited event
            await Task.WhenAny(eventHandled.Task);
            
        }



        private void TryStartLinuxTerminalEmulator(Process process, string[] terminalEmulators, string executable, string arguments)
        {
            foreach (string terminalEmulator in terminalEmulators)
            {
                try
                {
                    process.StartInfo.FileName = terminalEmulator;


                    string command = $"\"{Path.Combine(process.StartInfo.WorkingDirectory, executable)} {arguments}; read\"";
                    if (terminalEmulator == "gnome-terminal")
                    {
                        process.StartInfo.Arguments = $"-- bash -c \"{command}\"";
                    }
                    else
                    {
                        process.StartInfo.Arguments = $"-e \"{command}\"";
                    }


                    //process.StartInfo.Arguments = $"-e \"{executable} {arguments}\"";
                    process.Start();
                    return;
                }
                catch
                {
                    
                }
            }

            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = arguments;
            process.Start();
        }

        private void TryStartMacTerminalEmulator(Process process, string executable, string arguments)
        {
            try
            {
                process.StartInfo.FileName = "open";
                process.StartInfo.Arguments = $"-a Terminal \"{Path.Combine(process.StartInfo.WorkingDirectory, executable)} {arguments}\"";
                process.Start();
                return;
            }
            catch
            {

            }

            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = arguments;
            process.Start();
        }


        private void process_Exited(object sender, EventArgs e)
        {
            //Console.WriteLine(
            //    $"Exit time    : {process.ExitTime}\n" +
            //    $"Exit code    : {process.ExitCode}\n" +
            //    $"Elapsed time : {Math.Round((process.ExitTime - process.StartTime).TotalMilliseconds)}");
            //Console.WriteLine("Process " + Name + " exited");
            processId = defaultProcessId;
            eventHandled.TrySetResult(true);
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

    }
}
