using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace darts_hub.control
{
    /// <summary>
    /// Result of a security/environment inspection. The inspector tries to gather
    /// the most useful diagnostic info that helps to debug "doesn't run / blocked"
    /// issues, without ever requiring elevated privileges itself.
    /// </summary>
    public class SecurityReport
    {
        public string OsKind { get; set; } = "Unknown";
        public bool IsElevated { get; set; }
        public string ElevationDetails { get; set; } = string.Empty;
        public string ProcessUser { get; set; } = string.Empty;

        public List<SecurityProduct> AntivirusProducts { get; } = new();
        public List<SecurityProduct> FirewallProducts { get; } = new();
        public List<SecurityProduct> AntispywareProducts { get; } = new();

        public WindowsDefenderInfo? WindowsDefender { get; set; }
        public List<FirewallProfileStatus> FirewallProfiles { get; } = new();

        public List<ExclusionCheck> ExclusionChecks { get; } = new();
        public List<string> Notes { get; } = new();
    }

    public class SecurityProduct
    {
        public string Name { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string? ExePath { get; set; }
        public string? Source { get; set; }
    }

    public class FirewallProfileStatus
    {
        public string Profile { get; set; } = string.Empty; // Domain / Private / Public / Default / Active
        public string? State { get; set; }                  // ON / OFF / unknown
        public string? Notes { get; set; }
    }

    public class WindowsDefenderInfo
    {
        public bool? RealTimeProtectionEnabled { get; set; }
        public bool? TamperProtectionEnabled { get; set; }
        public List<string> ExcludedPaths { get; } = new();
        public List<string> ExcludedProcesses { get; } = new();
        public List<string> ExcludedExtensions { get; } = new();
        public string? RawError { get; set; }
    }

    public class ExclusionCheck
    {
        public string Name { get; set; } = string.Empty;       // e.g. "darts-hub" or extension name
        public string Path { get; set; } = string.Empty;
        public bool ExcludedInDefender { get; set; }
        public string? MatchingExclusion { get; set; }
    }

    /// <summary>
    /// Inspects the host for AV / Firewall / elevation information that is useful
    /// for support. Designed to be safe to call from a UI thread - all subprocess
    /// calls are time-boxed and exceptions are swallowed into the report.
    /// </summary>
    public static class SecurityEnvironmentInspector
    {
        public class PathToCheck
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
        }

        public static Task<SecurityReport> InspectAsync(IEnumerable<PathToCheck>? exclusionPathsToCheck = null)
        {
            var paths = exclusionPathsToCheck?.ToList() ?? new List<PathToCheck>();
            return Task.Run(() => Inspect(paths));
        }

        private static SecurityReport Inspect(List<PathToCheck> pathsToCheck)
        {
            var report = new SecurityReport
            {
                OsKind = GetOsKind(),
                ProcessUser = SafeGet(() => Environment.UserName)
            };

            try
            {
                DetectElevation(report);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    InspectWindows(report, pathsToCheck);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    InspectLinux(report);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    InspectMacOs(report);
                }
                else
                {
                    report.Notes.Add($"Unsupported OS for security inspection: {RuntimeInformation.OSDescription}");
                }
            }
            catch (Exception ex)
            {
                report.Notes.Add($"Inspection aborted with exception: {ex.Message}");
            }

            return report;
        }

        private static string GetOsKind()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macOS";
            return "Unknown";
        }

        private static void DetectElevation(SecurityReport report)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var identity = WindowsIdentity.GetCurrent();
                    var principal = new WindowsPrincipal(identity);
                    report.IsElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                    report.ElevationDetails = report.IsElevated
                        ? "Process is running with administrator privileges (UAC elevated)."
                        : "Process is running as a standard user (no UAC elevation).";
                    return;
                }

                // Unix-like: rely on `id -u` (returns 0 if root).
                var (exit, stdout, _) = RunProcess("id", "-u", TimeSpan.FromSeconds(5));
                if (exit == 0 && int.TryParse(stdout.Trim(), out var uid))
                {
                    report.IsElevated = uid == 0;
                    report.ElevationDetails = report.IsElevated
                        ? $"Process is running as root (uid={uid})."
                        : $"Process is running as a standard user (uid={uid}).";
                }
                else
                {
                    report.ElevationDetails = "Could not determine elevation (id command failed).";
                }
            }
            catch (Exception ex)
            {
                report.ElevationDetails = $"Elevation detection failed: {ex.Message}";
            }
        }

        // ------------------- Windows -------------------

        private static void InspectWindows(SecurityReport report, List<PathToCheck> pathsToCheck)
        {
            // Registered security products (works even for 3rd party AVs that integrate
            // with Windows Security Center: Avast, AVG, Bitdefender, ESET, Kaspersky,
            // Norton, McAfee, Sophos, Webroot, Trend Micro, etc).
            QueryWmiSecurityCenter(report, "AntiVirusProduct", report.AntivirusProducts);
            QueryWmiSecurityCenter(report, "FirewallProduct", report.FirewallProducts);
            QueryWmiSecurityCenter(report, "AntiSpywareProduct", report.AntispywareProducts);

            // Windows Firewall profile status via netsh (no admin required for read).
            QueryWindowsFirewallProfiles(report);

            // Windows Defender preferences (read works without admin on most systems).
            QueryWindowsDefender(report, pathsToCheck);
        }

        private static void QueryWmiSecurityCenter(SecurityReport report, string wmiClass, List<SecurityProduct> target)
        {
            try
            {
                // PowerShell is the most portable way to read WMI without taking a dep
                // on System.Management. We also keep a tight timeout to avoid hangs.
                var script =
                    $"Get-CimInstance -Namespace 'root/SecurityCenter2' -ClassName {wmiClass} | " +
                    "Select-Object displayName, productState, pathToSignedProductExe, pathToSignedReportingExe | " +
                    "ConvertTo-Json -Compress";

                var (exit, stdout, stderr) = RunPowerShell(script, TimeSpan.FromSeconds(12));
                if (exit != 0)
                {
                    report.Notes.Add($"WMI query for {wmiClass} failed (exit={exit}): {Truncate(stderr, 160)}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(stdout))
                {
                    return;
                }

                var token = JToken.Parse(stdout);
                var items = token is JArray arr ? arr : new JArray { token };
                foreach (var item in items)
                {
                    var product = new SecurityProduct
                    {
                        Name = item.Value<string>("displayName") ?? "(unknown)",
                        ExePath = item.Value<string>("pathToSignedProductExe"),
                        Source = $"WMI/{wmiClass}"
                    };

                    var stateValue = item.Value<long?>("productState");
                    if (stateValue.HasValue)
                    {
                        product.Status = DecodeProductState(stateValue.Value);
                    }
                    target.Add(product);
                }
            }
            catch (Exception ex)
            {
                report.Notes.Add($"WMI query for {wmiClass} threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Decodes the productState bit-field used by Windows Security Center.
        /// Source: Microsoft Defender documentation + community reverse engineering.
        /// </summary>
        private static string DecodeProductState(long state)
        {
            try
            {
                var hex = state.ToString("X6");
                if (hex.Length < 6) hex = hex.PadLeft(6, '0');

                var enabledByte = Convert.ToInt32(hex.Substring(2, 2), 16);
                var upToDateByte = Convert.ToInt32(hex.Substring(4, 2), 16);

                var enabled = (enabledByte & 0x10) != 0; // 0x10 / 0x11 = ON, 0x00 / 0x01 = OFF
                var upToDate = upToDateByte == 0x00;     // 0x00 = up to date, 0x10 = out of date

                return $"{(enabled ? "ENABLED" : "DISABLED")} / {(upToDate ? "up-to-date" : "out-of-date")} (state=0x{hex})";
            }
            catch
            {
                return $"state=0x{state:X}";
            }
        }

        private static void QueryWindowsFirewallProfiles(SecurityReport report)
        {
            try
            {
                var (exit, stdout, stderr) = RunProcess("netsh", "advfirewall show allprofiles", TimeSpan.FromSeconds(8));
                if (exit != 0)
                {
                    report.Notes.Add($"netsh firewall query failed (exit={exit}): {Truncate(stderr, 160)}");
                    return;
                }

                // Parse the output: blocks of "Domain Profile Settings", "Private Profile Settings", "Public Profile Settings"
                string? currentProfile = null;
                foreach (var rawLine in stdout.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (line.EndsWith("Profile Settings:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProfile = line.Replace(" Profile Settings:", "", StringComparison.OrdinalIgnoreCase).Trim();
                        continue;
                    }

                    if (currentProfile != null && line.StartsWith("State", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            report.FirewallProfiles.Add(new FirewallProfileStatus
                            {
                                Profile = currentProfile,
                                State = parts[1].Trim()
                            });
                        }
                        currentProfile = null;
                    }
                }
            }
            catch (Exception ex)
            {
                report.Notes.Add($"Windows firewall inspection threw: {ex.Message}");
            }
        }

        private static void QueryWindowsDefender(SecurityReport report, List<PathToCheck> pathsToCheck)
        {
            var info = new WindowsDefenderInfo();
            report.WindowsDefender = info;

            try
            {
                // Get-MpPreference exposes the user-configured exclusions and toggles.
                // It is read-only and works on standard user accounts.
                var script =
                    "Get-MpPreference | Select-Object DisableRealtimeMonitoring, DisableTamperProtection, " +
                    "ExclusionPath, ExclusionProcess, ExclusionExtension | ConvertTo-Json -Compress";

                var (exit, stdout, stderr) = RunPowerShell(script, TimeSpan.FromSeconds(15));
                if (exit != 0 || string.IsNullOrWhiteSpace(stdout))
                {
                    info.RawError = $"Get-MpPreference failed (exit={exit}): {Truncate(stderr, 200)}";
                    return;
                }

                var json = JObject.Parse(stdout);
                var disableRtp = json.Value<bool?>("DisableRealtimeMonitoring");
                if (disableRtp.HasValue) info.RealTimeProtectionEnabled = !disableRtp.Value;

                var disableTp = json.Value<bool?>("DisableTamperProtection");
                if (disableTp.HasValue) info.TamperProtectionEnabled = !disableTp.Value;

                AppendStringArray(json["ExclusionPath"], info.ExcludedPaths);
                AppendStringArray(json["ExclusionProcess"], info.ExcludedProcesses);
                AppendStringArray(json["ExclusionExtension"], info.ExcludedExtensions);

                // Cross-check requested paths against excluded paths.
                foreach (var p in pathsToCheck)
                {
                    var match = info.ExcludedPaths.FirstOrDefault(ex => PathStartsWith(p.Path, ex));
                    report.ExclusionChecks.Add(new ExclusionCheck
                    {
                        Name = p.Name,
                        Path = p.Path,
                        ExcludedInDefender = match != null,
                        MatchingExclusion = match
                    });
                }
            }
            catch (Exception ex)
            {
                info.RawError = $"Windows Defender query threw: {ex.Message}";
            }
        }

        private static void AppendStringArray(JToken? token, List<string> list)
        {
            if (token == null || token.Type == JTokenType.Null) return;
            if (token is JArray arr)
            {
                foreach (var v in arr) list.Add(v.ToString());
            }
            else
            {
                var s = token.ToString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
        }

        private static bool PathStartsWith(string fullPath, string prefix)
        {
            try
            {
                var fp = Path.TrimEndingDirectorySeparator(Path.GetFullPath(fullPath));
                var pf = Path.TrimEndingDirectorySeparator(Path.GetFullPath(prefix));
                return fp.StartsWith(pf, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(fullPath, prefix, StringComparison.OrdinalIgnoreCase);
            }
        }

        // ------------------- Linux -------------------

        private static void InspectLinux(SecurityReport report)
        {
            // ufw
            TryShellTool(report, report.FirewallProducts, "ufw", "ufw status",
                output => new SecurityProduct
                {
                    Name = "Uncomplicated Firewall (ufw)",
                    Status = ExtractFirstLine(output, "Status:"),
                    Source = "ufw status"
                });

            // firewalld
            TryShellTool(report, report.FirewallProducts, "firewall-cmd", "firewall-cmd --state",
                output => new SecurityProduct
                {
                    Name = "firewalld",
                    Status = output.Trim(),
                    Source = "firewall-cmd --state"
                });

            // iptables (just presence + version, listing rules typically requires root)
            TryShellTool(report, report.FirewallProducts, "iptables", "iptables --version",
                output => new SecurityProduct
                {
                    Name = "iptables (kernel firewall)",
                    Status = output.Trim(),
                    Source = "iptables --version"
                });

            // nftables
            TryShellTool(report, report.FirewallProducts, "nft", "nft --version",
                output => new SecurityProduct
                {
                    Name = "nftables",
                    Status = output.Trim(),
                    Source = "nft --version"
                });

            // ClamAV
            TryShellTool(report, report.AntivirusProducts, "clamscan", "clamscan --version",
                output => new SecurityProduct
                {
                    Name = "ClamAV",
                    Status = output.Trim(),
                    Source = "clamscan --version"
                });

            // SELinux / AppArmor (not strictly AV/FW but commonly block app execution).
            TryShellTool(report, report.AntispywareProducts, "getenforce", "getenforce",
                output => new SecurityProduct
                {
                    Name = "SELinux",
                    Status = output.Trim(),
                    Source = "getenforce"
                });
            TryShellTool(report, report.AntispywareProducts, "aa-status", "aa-status --enabled",
                output => new SecurityProduct
                {
                    Name = "AppArmor",
                    Status = string.IsNullOrWhiteSpace(output) ? "enabled" : output.Trim(),
                    Source = "aa-status"
                });

            report.Notes.Add(
                "Linux antivirus / firewall exclusion lists are vendor-specific and not " +
                "exposed through a common API. Please attach the relevant config files manually if needed.");
        }

        // ------------------- macOS -------------------

        private static void InspectMacOs(SecurityReport report)
        {
            // Application Layer Firewall
            try
            {
                var (exit, stdout, _) = RunProcess("defaults",
                    "read /Library/Preferences/com.apple.alf globalstate", TimeSpan.FromSeconds(5));
                if (exit == 0)
                {
                    var state = stdout.Trim();
                    var label = state switch
                    {
                        "0" => "OFF",
                        "1" => "ON (allow signed apps)",
                        "2" => "ON (block all incoming)",
                        _ => $"unknown ({state})"
                    };
                    report.FirewallProfiles.Add(new FirewallProfileStatus
                    {
                        Profile = "ApplicationLayerFirewall",
                        State = label
                    });
                }
            }
            catch (Exception ex)
            {
                report.Notes.Add($"macOS firewall query threw: {ex.Message}");
            }

            // Gatekeeper
            try
            {
                var (exit, stdout, _) = RunProcess("spctl", "--status", TimeSpan.FromSeconds(5));
                if (exit == 0)
                {
                    report.AntispywareProducts.Add(new SecurityProduct
                    {
                        Name = "Gatekeeper",
                        Status = stdout.Trim(),
                        Source = "spctl --status"
                    });
                }
            }
            catch (Exception ex)
            {
                report.Notes.Add($"Gatekeeper query threw: {ex.Message}");
            }

            // System Integrity Protection
            try
            {
                var (exit, stdout, _) = RunProcess("csrutil", "status", TimeSpan.FromSeconds(5));
                if (exit == 0)
                {
                    report.AntispywareProducts.Add(new SecurityProduct
                    {
                        Name = "System Integrity Protection",
                        Status = stdout.Trim(),
                        Source = "csrutil status"
                    });
                }
            }
            catch
            {
                // csrutil only available on macOS, ignore
            }

            // XProtect is built in and not configurable - just note its presence.
            report.AntivirusProducts.Add(new SecurityProduct
            {
                Name = "XProtect (built-in)",
                Status = "always active (cannot be disabled or configured)",
                Source = "macOS"
            });

            report.Notes.Add(
                "macOS antivirus exclusions are vendor-specific and only available for " +
                "third-party AV/EDR products if installed.");
        }

        // ------------------- Helpers -------------------

        private static void TryShellTool(
            SecurityReport report,
            List<SecurityProduct> target,
            string toolName,
            string commandLine,
            Func<string, SecurityProduct> mapper)
        {
            try
            {
                var split = commandLine.Split(' ', 2);
                var args = split.Length > 1 ? split[1] : string.Empty;

                var (exit, stdout, stderr) = RunProcess(split[0], args, TimeSpan.FromSeconds(5));
                if (exit == -1)
                {
                    // Tool not installed, that's fine - skip silently.
                    return;
                }

                if (exit != 0 && string.IsNullOrWhiteSpace(stdout))
                {
                    report.Notes.Add($"{toolName} returned exit={exit}: {Truncate(stderr, 160)}");
                    return;
                }

                target.Add(mapper(stdout));
            }
            catch (Exception ex)
            {
                report.Notes.Add($"{toolName} inspection threw: {ex.Message}");
            }
        }

        private static (int ExitCode, string StdOut, string StdErr) RunProcess(string fileName, string arguments, TimeSpan timeout)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null) return (-1, string.Empty, "process not started");

                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();
                process.OutputDataReceived += (_, e) => { if (e.Data != null) stdoutBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit((int)timeout.TotalMilliseconds))
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                    return (-2, stdoutBuilder.ToString(), $"timeout after {timeout.TotalSeconds:F0}s");
                }

                // Ensure async readers flush.
                process.WaitForExit();
                return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Tool not found.
                return (-1, string.Empty, $"'{fileName}' not found");
            }
            catch (Exception ex)
            {
                return (-3, string.Empty, ex.Message);
            }
        }

        private static (int ExitCode, string StdOut, string StdErr) RunPowerShell(string script, TimeSpan timeout)
        {
            // Prefer Windows PowerShell which is available on every supported Windows version;
            // fall back to pwsh if PS5 is missing (very rare).
            var (exit, stdout, stderr) = RunProcess("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                timeout);

            if (exit == -1)
            {
                return RunProcess("pwsh",
                    $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
                    timeout);
            }

            return (exit, stdout, stderr);
        }

        private static string ExtractFirstLine(string text, string prefix)
        {
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed;
                }
            }
            return text.Trim();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            value = Regex.Replace(value, @"\s+", " ").Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
        }

        private static T SafeGet<T>(Func<T> getter)
        {
            try { return getter(); } catch { return default!; }
        }
    }
}
