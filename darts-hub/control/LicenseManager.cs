using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using darts_hub.model;

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

            var hardwareId = GetHardwareId();
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
        /// Generates a deterministic hardware ID for the current machine.
        /// </summary>
        public static string GetHardwareId()
        {
            try
            {
                var machineName = Environment.MachineName;
                var userName = Environment.UserName;
                var osVersion = Environment.OSVersion.ToString();

                var raw = $"{machineName}|{userName}|{osVersion}";
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
