using System;
using System.Runtime.InteropServices;

namespace autodarts_desktop.control
{
    // examples:
    // https://github.com/autodarts/releases/releases/download/v0.18.0/autodarts0.18.0.windows-amd64.zip
    // https://github.com/lbormann/autodarts-caller/releases/download/v2.0.14/autodarts-caller.exe

    public class DownloadMap
    {
        // ATTRIBUTES

        public const string VERSIONPATTERN = "***VERSION***";

        public string VersionPattern { get; set; }

        public string? LinuxX64 { get; set; }
        public string? LinuxX86 { get; set; }
        public string? LinuxArm64 { get; set; }
        public string? LinuxArm { get; set; }

        public string? WindowsX64 { get; set; }
        public string? WindowsX86 { get; set; }
        public string? WindowsArm64 { get; set; }
        public string? WindowsArm { get; set; }

        public string? MacX64 { get; set; }
        public string? MacX86 { get; set; }
        public string? MacArm64 { get; set; }
        public string? MacArm { get; set; }




        // METHODS

        public DownloadMap(string versionPattern = VERSIONPATTERN)
        {
            VersionPattern = versionPattern;
        }

        public string? GetDownloadUrlByOs(string version = "MISSING-VERSION")
        {
            string? downloadUrl = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && !String.IsNullOrEmpty(LinuxX64))
                {
                    downloadUrl = LinuxX64.Replace(VersionPattern, version);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && !String.IsNullOrEmpty(LinuxX86))
                {
                    downloadUrl = LinuxX86.Replace(VersionPattern, version);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && !String.IsNullOrEmpty(LinuxArm64))
                {
                    downloadUrl = LinuxArm64.Replace(VersionPattern, version);
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm && !String.IsNullOrEmpty(LinuxArm))
                {
                    downloadUrl = LinuxArm.Replace(VersionPattern, version);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && !String.IsNullOrEmpty(WindowsX64))
                {
                    downloadUrl = WindowsX64.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && !String.IsNullOrEmpty(WindowsX86))
                {
                    downloadUrl = WindowsX86.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && !String.IsNullOrEmpty(WindowsArm64))
                {
                    downloadUrl = WindowsArm64.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm && !String.IsNullOrEmpty(WindowsArm))
                {
                    downloadUrl = WindowsArm.Replace(VersionPattern, version); ;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X64 && !String.IsNullOrEmpty(MacX64))
                {
                    downloadUrl = MacX64.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.X86 && !String.IsNullOrEmpty(MacX86))
                {
                    downloadUrl = MacX86.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && !String.IsNullOrEmpty(MacArm64))
                {
                    downloadUrl = MacArm64.Replace(VersionPattern, version); ;
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm && !String.IsNullOrEmpty(MacArm))
                {
                    downloadUrl = MacArm.Replace(VersionPattern, version); ;
                }
            }
            return downloadUrl;
        }



    }
}
