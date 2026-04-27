using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net.Http;


namespace darts_hub.control
{
    /// <summary>
    /// Provides common functions
    /// </summary>
    public static class Helper
    {
        public static bool DirectoryOrFileStartsWith(string path, string searchString)
        {
            if (Directory.Exists(path) && Path.GetFileName(path).StartsWith(searchString))
            {
                return true;
            }

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).StartsWith(searchString))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static long GetFileSizeByUrl(string url)
        {
            long result = -1;

            var req = WebRequest.Create(url);
            req.Method = "HEAD";
            req.Timeout = 4000;
            using (WebResponse resp = req.GetResponse())
            {
                if (long.TryParse(resp.Headers.Get("Content-Length"), out long ContentLength))
                {
                    result = ContentLength;
                }
            }
            return result;
        }

        public static long GetFileSizeByLocal(string pathToFile)
        {
            if (File.Exists(pathToFile))
            {
                return new FileInfo(pathToFile).Length;
            }
            return -2;
        }

        public static async Task<string> AsyncHttpGet(string url, double timeout)
        {
            try
            {
                var changelog = String.Empty;
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(timeout);
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        changelog = await response.Content.ReadAsStringAsync();
                    }
                }
                return changelog;
            }
            catch (Exception ex)
            {

            }
            return string.Empty;
        }

        public static string GetAppBasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var executablePath = Process.GetCurrentProcess().MainModule.FileName;
                return Path.GetDirectoryName(executablePath);
            }
            return Path.GetDirectoryName(AppContext.BaseDirectory);
        }

        public static string GetUserDirectoryPath()
        {
            // https://stackoverflow.com/questions/1140383/how-can-i-get-the-current-user-directory
            string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            if (Environment.OSVersion.Version.Major >= 6)
            {
                path = Directory.GetParent(path).ToString();
            }
            return path;
        }

        public static void RemoveDirectory(string directory, bool createAfterRemove = false)
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            if (createAfterRemove) Directory.CreateDirectory(directory);
        }

        public static string GetStringByBool(bool input, string trueValue = "1", string falseValue = "0")
        {
            return input ? trueValue : falseValue;
        }

        public static bool GetBoolByString(string input, string trueValue = "1")
        {
            return input == trueValue ? true : false;
        }

        public static string GetStringByInt(int input)
        {
            return input.ToString();
        }

        public static double GetIntByString(string input)
        {
            return int.Parse(input);
        }

        public static string GetStringByDouble(double input)
        {
            return Math.Round(input, 2).ToString().Replace(",", ".");
        }

        public static double GetDoubleByString(string input)
        {   
            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (decimalSeparator == ",")
            {
                input = input.Replace('.', ',');
            }
            else if (decimalSeparator == ".")
            {
                input = input.Replace(',', '.');
            }
            return Double.Parse(input);
        }

        public static string GetFileNameByUrl(string url)
        {
            string[] urlSplitted = url.Split("/");
            return urlSplitted[urlSplitted.Length - 1];
        }

        public static string? SearchExecutableOnDrives(string filename)
        {
            string[] drives = Directory.GetLogicalDrives();
            string pathToExecutable = null;
            foreach (string drive in drives)
            {
                pathToExecutable =
                    Directory
                    .EnumerateFiles(drive, filename, SearchOption.AllDirectories)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(pathToExecutable)) break;
            }
            return pathToExecutable;
        }

        public static string? SearchExecutable(string path)
        {
            if (!Directory.Exists(path)) return null;

            string[] executableExtensions;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executableExtensions = new[] { "exe" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                executableExtensions = new[] { "" }; // Keine Erweiterung für ausführbare Dateien unter Linux und macOS
            }
            else
            {
                return null; // Nicht unterstützte Plattform
            }

            string executable = Directory
                .EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(s => executableExtensions.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

            return executable;
        }

        public static bool IsProcessRunning(int processId)
        {
            return processId != -1 && Process.GetProcessById(processId) != null;
        }

        public static bool IsProcessRunning(string? processName)
        {
            return Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.ToLower().Contains(processName.ToLower())) != null;
        }

        public static void KillProcess(int processId)
        {
            if (processId == -1) return;

            var process = Process.GetProcessById(processId);
            process.Kill(false);

            process = Process.GetProcessById(processId);
            process.Kill(false);
        }

        // libc kill() for Unix-like systems (Linux + macOS).
        // Used to send SIGTERM (15) before falling back to SIGKILL (9).
        [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
        private static extern int sys_kill(int pid, int sig);

        private const int SIGTERM = 15;

        /// <summary>
        /// Tries to terminate a process gracefully (SIGTERM on Linux/macOS,
        /// WM_CLOSE via CloseMainWindow on Windows). If the process does not
        /// exit within <paramref name="timeoutMs"/>, a hard kill (SIGKILL /
        /// TerminateProcess) is performed. Cross-platform.
        /// </summary>
        public static void KillProcessGracefully(int processId, int timeoutMs = 10000)
        {
            if (processId <= 0) return;

            Process? process;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch
            {
                return; // already gone
            }

            if (process == null || process.HasExited) return;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows has no SIGTERM. CloseMainWindow sends WM_CLOSE which
                    // allows GUI apps to shut down cleanly. Console apps without a
                    // window will simply ignore it and be killed after the timeout.
                    try { process.CloseMainWindow(); } catch { /* ignore */ }
                }
                else
                {
                    SendSigTerm(processId);
                }

                if (process.WaitForExit(timeoutMs))
                {
                    return;
                }

                // Timeout: hard kill
                if (!process.HasExited)
                {
                    process.Kill(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KillProcessGracefully({processId}) error: {ex.Message}");
                try
                {
                    if (!process.HasExited) process.Kill(false);
                }
                catch { /* ignore */ }
            }
        }

        private static void SendSigTerm(int pid)
        {
            try
            {
                if (sys_kill(pid, SIGTERM) == 0) return;
            }
            catch
            {
                // P/Invoke failed (e.g. libc not resolvable) - fall back to /bin/kill
            }

            try
            {
                using var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "/bin/kill",
                    Arguments = $"-{SIGTERM} {pid}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                p?.WaitForExit(2000);
            }
            catch { /* ignore - caller will hard-kill on timeout */ }
        }

        public static void KillProcess(string processName)
        {
            processName = Path.GetFileNameWithoutExtension(processName);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
                if(process != null) { process.Kill(false); }

                process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
                if (process != null) { process.Kill(false); }
                return;
            }
            KillProcessesByNameOsX(processName);
        }

        private static void KillProcessesByNameOsX(string processName)
        {
            var processIds = FindProcessesByNameOsx(processName).Reverse();
            foreach (var processId in processIds)
            {
                KillProcess(processId);
            }
        }

        private static int[] FindProcessesByNameOsx(string processName)
        {
            var output = ExecuteCommand($"pgrep {processName}");
            var processIds = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(id => int.Parse(id))
                                   .ToArray();
            return processIds;
        }

        private static string ExecuteCommand(string command)
        {
            var escapedArgs = command.Replace("\"", "\\\"");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output;
        }

    }
}
