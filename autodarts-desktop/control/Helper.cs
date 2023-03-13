using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

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

            string executable = Directory
                .EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                .FirstOrDefault(s => Path.GetExtension(s).TrimStart('.').ToLowerInvariant() == "exe");
            return executable;
        }

        public static void KillProcess(int processId)
        {
            if (processId == -1) return;

            var process = Process.GetProcessById(processId);
            if (process != null)
            {
                process.Kill();
            }
            process = Process.GetProcessById(processId);
            if (process != null)
            {
                process.Kill();
            }
        }
        public static void KillProcess(string processName)
        {
            processName = Path.GetFileNameWithoutExtension(processName);

            var process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
            if (process != null)
            {
                process.Kill();
            }
            process = Process.GetProcessesByName(processName).FirstOrDefault(p => p.ProcessName.Contains(processName));
            if (process != null)
            {
                process.Kill();
            }
        }


    }
}
