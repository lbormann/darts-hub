using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using darts_hub.model;

namespace darts_hub.control
{
    public class NotificationsChangedEventArgs : EventArgs
    {
        public IReadOnlyList<Notification> All { get; }
        public IReadOnlyList<Notification> Added { get; }
        public int UnreadCount { get; }

        public NotificationsChangedEventArgs(IReadOnlyList<Notification> all, IReadOnlyList<Notification> added, int unread)
        {
            All = all;
            Added = added;
            UnreadCount = unread;
        }
    }

    /// <summary>
    /// Orchestrates background polling, persistence and user actions
    /// for the notification subsystem.
    /// </summary>
    public class NotificationManager
    {
        // Shared with the License server (same HMAC scheme & credentials)
        private const string BaseUrl = "https://license.darts-hub.i3ull3t.de";
        private const string ApiKey = "darts-hub-client-v1";
        private const string SecretKey = "c06da280e3b810f8bb600a1b491bf131075afa70bc49a4246d97d69794f04456eadd393ed186da3510d5762eb905b172a615ae46a9411d4d0180b1a6de0d362c";

        private readonly LicenseManager licenseManager;
        private readonly NotificationApi api;
        private readonly NotificationStore store;
        private readonly string hardwareId;

        private CancellationTokenSource? loopCts;
        private int pollIntervalSeconds = 60;
        private int failureCount;

        public event EventHandler<NotificationsChangedEventArgs>? Changed;

        public NotificationManager(LicenseManager licenseManager)
        {
            ArgumentNullException.ThrowIfNull(licenseManager);
            this.licenseManager = licenseManager;
            this.api = new NotificationApi(BaseUrl, ApiKey, SecretKey);
            this.store = new NotificationStore();
            this.hardwareId = LicenseManager.GetHardwareId();
        }

        public string BaseUrl_ => api.BaseUrl;

        public IReadOnlyList<Notification> Notifications => store.All
            .Where(n => n.State.DismissedAt == null && !n.IsExpired)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.Id)
            .ToList();

        public int UnreadCount => Notifications.Count(n => n.IsUnread);

        public void Start()
        {
            if (loopCts != null) return;
            loopCts = new CancellationTokenSource();
            _ = Task.Run(() => PollLoopAsync(loopCts.Token));
        }

        public void Stop()
        {
            try
            {
                loopCts?.Cancel();
            }
            catch { /* ignore */ }
            loopCts = null;
        }

        public async Task RefreshNowAsync(CancellationToken ct = default)
        {
            await PollOnceAsync(ct).ConfigureAwait(false);
        }

        public Task<bool> MarkReadAsync(long notificationId, CancellationToken ct = default)
        {
            var n = store.GetById(notificationId);
            if (n == null || n.State.ReadAt != null) return Task.FromResult(true);
            store.MarkRead(notificationId);
            RaiseChanged(Array.Empty<Notification>());
            return api.AckAsync(licenseManager.StoredLicenseKey, hardwareId, notificationId, "read", ct);
        }

        public Task<bool> AcknowledgeAsync(long notificationId, CancellationToken ct = default)
        {
            store.MarkAcknowledged(notificationId);
            RaiseChanged(Array.Empty<Notification>());
            return api.AckAsync(licenseManager.StoredLicenseKey, hardwareId, notificationId, "acknowledged", ct);
        }

        public Task<bool> DismissAsync(long notificationId, CancellationToken ct = default)
        {
            store.MarkDismissed(notificationId);
            RaiseChanged(Array.Empty<Notification>());
            return api.AckAsync(licenseManager.StoredLicenseKey, hardwareId, notificationId, "dismissed", ct);
        }

        public async Task<PollVoteResult> VoteAsync(long notificationId, long pollId, IReadOnlyList<long> optionIds, CancellationToken ct = default)
        {
            try
            {
                var result = await api.VoteAsync(licenseManager.StoredLicenseKey, hardwareId, pollId, optionIds, ct).ConfigureAwait(false);
                store.RecordVote(notificationId, pollId, optionIds, result);
                RaiseChanged(Array.Empty<Notification>());
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationManager] Vote failed: {ex.Message}");
                return new PollVoteResult { Success = false, Message = ex.Message };
            }
        }

        private async Task PollLoopAsync(CancellationToken ct)
        {
            // First poll immediately
            try { await PollOnceAsync(ct).ConfigureAwait(false); } catch { /* swallow */ }

            while (!ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Max(15, pollIntervalSeconds));
                if (failureCount > 0)
                {
                    var backoff = Math.Min(900, pollIntervalSeconds * Math.Pow(2, failureCount));
                    delay = TimeSpan.FromSeconds(backoff);
                }

                try
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }

                try { await PollOnceAsync(ct).ConfigureAwait(false); }
                catch (Exception ex) { Debug.WriteLine($"[NotificationManager] Poll loop error: {ex.Message}"); }
            }
        }

        private async Task PollOnceAsync(CancellationToken ct)
        {
            try
            {
                var sinceId = store.MaxId;
                var result = await api.PollAsync(
                    licenseManager.HasStoredLicenseKey ? licenseManager.StoredLicenseKey : null,
                    hardwareId,
                    sinceId,
                    ct).ConfigureAwait(false);

                if (!result.Success)
                {
                    Debug.WriteLine($"[NotificationManager] Poll unsuccessful: {result.Message}");
                    failureCount++;
                    return;
                }

                failureCount = 0;
                if (result.PollInterval > 0)
                    pollIntervalSeconds = result.PollInterval;

                var added = store.Merge(result.Notifications);
                RaiseChanged(added);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                failureCount++;
                Debug.WriteLine($"[NotificationManager] PollOnceAsync failed: {ex.Message}");
            }
        }

        private void RaiseChanged(IReadOnlyList<Notification> added)
        {
            try
            {
                Changed?.Invoke(this, new NotificationsChangedEventArgs(Notifications, added, UnreadCount));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationManager] Changed handler threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolve a possibly-relative URL returned by the server to an absolute one.
        /// </summary>
        public string ResolveUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (Uri.TryCreate(url, UriKind.Absolute, out _)) return url;
            return api.BaseUrl + (url.StartsWith("/") ? url : "/" + url);
        }
    }
}
