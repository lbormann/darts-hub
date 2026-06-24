using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using darts_hub.model;
using Microsoft.Win32;

namespace darts_hub.control
{
    /// <summary>
    /// Represents the current license state in the application.
    /// </summary>
    public enum LicenseStatus
    {
        Unknown,
        Valid,
        Invalid,
        Expired,
        Blocked,
        Revoked,
        Pending,
        ConnectionError
    }

    /// <summary>
    /// Event args for license status changes.
    /// </summary>
    public class LicenseStatusChangedEventArgs : EventArgs
    {
        public LicenseStatus Status { get; }
        public string Message { get; }

        public LicenseStatusChangedEventArgs(LicenseStatus status, string message)
        {
            Status = status;
            Message = message;
        }
    }

    /// <summary>
    /// Manages license validation, caching, and hardware-ID generation.
    /// Acts as the single source of truth for the current license state.
    /// </summary>
    public class LicenseManager
    {
        private const string BaseUrl = "https://license.darts-hub.i3ull3t.de";
        private const string ApiKey = "darts-hub-client-v1";
        private const string SecretKey = "c06da280e3b810f8bb600a1b491bf131075afa70bc49a4246d97d69794f04456eadd393ed186da3510d5762eb905b172a615ae46a9411d4d0180b1a6de0d362c";

        private readonly Configurator configurator;
        private readonly LicenseClient client;

        private LicenseStatus currentStatus = LicenseStatus.Unknown;
        private string currentMessage = string.Empty;
        private LicenseResult? lastResult;

        /// <summary>
        /// Fires whenever the license status changes.
        /// </summary>
        public event EventHandler<LicenseStatusChangedEventArgs>? StatusChanged;

        public LicenseStatus CurrentStatus => currentStatus;
        public string CurrentMessage => currentMessage;
        public LicenseResult? LastResult => lastResult;

        /// <summary>
        /// Returns true if a license key is stored locally.
        /// </summary>
        public bool HasStoredLicenseKey => !string.IsNullOrWhiteSpace(configurator.Settings.LicenseKey);

        /// <summary>
        /// Gets the stored license key, or empty string if none.
        /// </summary>
        public string StoredLicenseKey => configurator.Settings.LicenseKey ?? string.Empty;

        /// <summary>
        /// Returns true if the current license includes the given feature key.
        /// </summary>
        public bool HasFeature(string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey))
                return true;

            return lastResult != null && lastResult.HasFeature(featureKey);
        }

        /// <summary>
        /// Returns true if an argument is accessible under the current license.
        /// Arguments without a RequiredFeature are always accessible.
        /// </summary>
        public bool IsArgumentAccessible(Argument argument)
        {
            ArgumentNullException.ThrowIfNull(argument);
            return string.IsNullOrWhiteSpace(argument.RequiredFeature) || HasFeature(argument.RequiredFeature);
        }

        public LicenseManager(Configurator configurator)
        {
            this.configurator = configurator;
            this.client = new LicenseClient(BaseUrl, ApiKey, SecretKey);
        }

        /// <summary>
        /// Saves a license key and immediately validates it.
        /// </summary>
        public async Task<LicenseResult> SaveAndValidateAsync(string licenseKey, CancellationToken ct = default)
        {
            configurator.Settings.LicenseKey = licenseKey.Trim();
            configurator.SaveSettings();

            return await ValidateAsync(ct);
        }

        /// <summary>
        /// Validates the stored license key against the server.
        /// Updates status and fires StatusChanged.
        /// </summary>
        public async Task<LicenseResult> ValidateAsync(CancellationToken ct = default)
        {
            if (!HasStoredLicenseKey)
            {
                Debug.WriteLine("[LicenseManager] No license key configured, skipping validation.");
                SetStatus(LicenseStatus.Unknown, "No license key configured.");
                return new LicenseResult { Success = false, Valid = false, Message = "No license key configured." };
            }

            var hardwareId = GetOrCreateHardwareId();
            Debug.WriteLine($"[LicenseManager] Validating license key: {StoredLicenseKey}");
            Debug.WriteLine($"[LicenseManager] Hardware ID: {hardwareId}");

            var result = await client.ValidateAsync(StoredLicenseKey, hardwareId, ct);
            lastResult = result;

            Debug.WriteLine($"[LicenseManager] Validation result: Success={result.Success}, Valid={result.Valid}, Status={result.Status}, Message={result.Message}");
            if (!string.IsNullOrEmpty(result.ErrorDetail))
                Debug.WriteLine($"[LicenseManager] Error detail: {result.ErrorDetail}");

            if (!string.IsNullOrEmpty(result.ErrorDetail))
            {
                SetStatus(LicenseStatus.ConnectionError, result.ErrorDetail);
                return result;
            }

            var status = MapStatus(result);
            Debug.WriteLine($"[LicenseManager] Mapped status: {status}");
            SetStatus(status, result.Message);
            return result;
        }

        /// <summary>
        /// Removes the stored license key and resets the status.
        /// </summary>
        public void ClearLicense()
        {
            configurator.Settings.LicenseKey = string.Empty;
            configurator.SaveSettings();
            lastResult = null;
            SetStatus(LicenseStatus.Unknown, "License removed.");
        }

        /// <summary>
        /// Returns the hardware ID currently bound to this installation.
        /// If no ID is cached yet in <see cref="AppConfiguration.HardwareId"/>, a new one is
        /// generated from stable per-machine sources and persisted, so subsequent application
        /// or PC restarts always yield the same value.
        /// </summary>
        public string GetOrCreateHardwareId()
        {
            var cached = configurator.Settings.HardwareId;
            if (!string.IsNullOrWhiteSpace(cached))
                return cached;

            var generated = GetHardwareId();
            configurator.Settings.HardwareId = generated;

            try
            {
                configurator.SaveSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to persist hardware ID: {ex.Message}");
            }

            return generated;
        }

        /// <summary>
        /// Generates a deterministic hardware ID for the current machine using stable,
        /// platform-specific identifiers (Windows MachineGuid, Linux /etc/machine-id,
        /// macOS IOPlatformUUID). Falls back to a less-stable identifier only when
        /// none of those sources are available.
        /// </summary>
        /// <remarks>
        /// The returned value is a lowercase SHA-256 hex string (64 chars) to preserve
        /// the exact format previously sent to the license server.
        /// </remarks>
        public static string GetHardwareId()
        {
            try
            {
                var raw = TryGetStableMachineIdentifier();

                if (string.IsNullOrWhiteSpace(raw))
                {
                    // Last-resort fallback. We deliberately exclude OSVersion / UserName here,
                    // because both change without any hardware change and would cause the
                    // license server to register a new "device" on every Windows update or
                    // when the app is started under a different user (e.g. via UAC elevation
                    // or a scheduled task).
                    raw = "machine:" + Environment.MachineName;
                }

                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to generate hardware ID: {ex.Message}");
                return "unknown-hardware-id";
            }
        }

        private static string? TryGetStableMachineIdentifier()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var winId = TryReadWindowsMachineGuid();
                    if (!string.IsNullOrWhiteSpace(winId))
                        return "win:" + winId;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var linuxId = TryReadFirstLine("/etc/machine-id")
                                  ?? TryReadFirstLine("/var/lib/dbus/machine-id");
                    if (!string.IsNullOrWhiteSpace(linuxId))
                        return "linux:" + linuxId;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var macId = TryReadMacPlatformUuid();
                    if (!string.IsNullOrWhiteSpace(macId))
                        return "mac:" + macId;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to read stable machine identifier: {ex.Message}");
            }

            return null;
        }

        [SupportedOSPlatform("windows")]
        private static string? TryReadWindowsMachineGuid()
        {
            // HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid is created by Windows at
            // install time and survives updates, user changes and hostname changes.
            // It only changes when Windows is reinstalled.
            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

                var value = key?.GetValue("MachineGuid") as string;
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to read MachineGuid (64-bit view): {ex.Message}");
            }

            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

                return key?.GetValue("MachineGuid") as string;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to read MachineGuid (32-bit view): {ex.Message}");
                return null;
            }
        }

        private static string? TryReadFirstLine(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var content = File.ReadAllText(path).Trim();
                return string.IsNullOrWhiteSpace(content) ? null : content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to read {path}: {ex.Message}");
                return null;
            }
        }

        [SupportedOSPlatform("macos")]
        private static string? TryReadMacPlatformUuid()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);

                const string marker = "IOPlatformUUID";
                var idx = output.IndexOf(marker, StringComparison.Ordinal);
                if (idx < 0) return null;

                var start = output.IndexOf('"', idx + marker.Length);
                if (start < 0) return null;
                start = output.IndexOf('"', start + 1);
                if (start < 0) return null;
                var end = output.IndexOf('"', start + 1);
                if (end < 0) return null;

                var uuid = output.Substring(start + 1, end - start - 1).Trim();
                return string.IsNullOrWhiteSpace(uuid) ? null : uuid;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseManager] Failed to read macOS IOPlatformUUID: {ex.Message}");
                return null;
            }
        }

        private void SetStatus(LicenseStatus status, string message)
        {
            currentStatus = status;
            currentMessage = message;
            StatusChanged?.Invoke(this, new LicenseStatusChangedEventArgs(status, message));
        }

        private static LicenseStatus MapStatus(LicenseResult result)
        {
            if (result.Valid)
                return LicenseStatus.Valid;

            return result.Status?.ToLowerInvariant() switch
            {
                "active" => LicenseStatus.Valid,
                "expired" => LicenseStatus.Expired,
                "blocked" => LicenseStatus.Blocked,
                "revoked" => LicenseStatus.Revoked,
                "pending" => LicenseStatus.Pending,
                _ => LicenseStatus.Invalid
            };
        }
    }
}
