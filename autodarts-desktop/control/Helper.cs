using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Reflection;

namespace autodarts_desktop.control
{
    /// <summary>
    /// Provides common functions
    /// </summary>
    public static class Helper
    {

        public static long GetFileSizeByUrl(string url)
        {
            long result = -1;

            var req = WebRequest.Create(url);
            req.Method = "HEAD";
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
            return Double.Parse(input.Replace(".", ","));
        }

        public static string GetFileNameByUrl(string url)
        {
            string[] urlSplitted = url.Split("/");
            return urlSplitted[urlSplitted.Length - 1];
        }

        public static bool IsProcessRunning(int processId)
        {
            return processId != -1 && Process.GetProcessById(processId) != null;
        }
        
        public static bool IsProcessRunning(string? processName)
        {
            return Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.ToLower().Contains(processName.ToLower())) != null;
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

        public static void KillProcess(int processId)
        {
            if (processId == -1) return;

            var process = Process.GetProcessById(processId);
            KillProcessAndChildren(process);

            process = Process.GetProcessById(processId);
            KillProcessAndChildren(process);
        }
        
        public static void KillProcess(string processName)
        {
            processName = Path.GetFileNameWithoutExtension(processName);

            var process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
            KillProcessAndChildren(process);

            process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
            KillProcessAndChildren(process);
        }



        private static void KillProcessAndChildren(Process process)
        {
            if (process == null) return;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                process.Kill();
                return;
            }

            // Get child processes
            var childProcesses = Process.GetProcesses().Where(p => p.Parent().Id == process.Id);

            // Kill child processes
            foreach (var childProcess in childProcesses)
            {
                KillProcessAndChildren(childProcess);
            }

            // Kill the main process
            process.Kill();
        }


        public static Process Parent(this Process process)
        {
            int parentPid = -1;
            var procInfo = new ProcTaskAllInfo();
            int bufferSize = Marshal.SizeOf(procInfo);
            int status = proc_pidinfo(process.Id, ProcInfoFlavor.PROC_PIDTASKALLINFO, 0, ref procInfo, bufferSize);

            if (status == bufferSize)
            {
                parentPid = procInfo.pbsd.pbi_ppid;
            }

            if (parentPid != -1)
            {
                try
                {
                    return Process.GetProcessById(parentPid);
                }
                catch
                {
                    // Parent process not found or an error occurred
                }
            }
            return null;
        }

        // macOS specific P/Invoke
        [DllImport("libproc", SetLastError = true)]
        private static extern int proc_pidinfo(int pid, ProcInfoFlavor flavor, uint arg, ref ProcTaskAllInfo buffer, int buffersize);

        private enum ProcInfoFlavor : int
        {
            PROC_PIDTASKALLINFO = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcTaskAllInfo
        {
            public ProcTaskInfo ptinfo;
            public ProcBsdInfo pbsd;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcTaskInfo
        {
            public ulong pti_virtual_size;
            public ulong pti_resident_size;
            // Other fields are omitted for brevity
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcBsdInfo
        {
            public int pbi_ppid;
            // Other fields are omitted for brevity
        }


    }
}
