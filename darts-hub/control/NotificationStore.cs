using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using darts_hub.model;

namespace darts_hub.control
{
    /// <summary>
    /// Persists notifications and per-device state to a small JSON file
    /// so the user can still browse messages while offline.
    /// </summary>
    public class NotificationStore
    {
        private readonly string filePath;
        private readonly object sync = new();
        private List<Notification> notifications = new();

        public NotificationStore(string? fileName = null)
        {
            var baseDir = Helper.GetAppBasePath() ?? AppContext.BaseDirectory;
            filePath = Path.Combine(baseDir, fileName ?? "notifications.json");
            Load();
        }

        public IReadOnlyList<Notification> All
        {
            get
            {
                lock (sync) return notifications.ToList();
            }
        }

        public long MaxId
        {
            get
            {
                lock (sync) return notifications.Count == 0 ? 0 : notifications.Max(n => n.Id);
            }
        }

        public int UnreadCount
        {
            get
            {
                lock (sync) return notifications.Count(n => n.IsUnread);
            }
        }

        public Notification? GetById(long id)
        {
            lock (sync) return notifications.FirstOrDefault(n => n.Id == id);
        }

        /// <summary>
        /// Merges incoming server notifications with the local cache.
        /// Returns the list of brand-new notifications (not previously known).
        /// </summary>
        public List<Notification> Merge(IEnumerable<Notification> incoming)
        {
            var added = new List<Notification>();
            lock (sync)
            {
                foreach (var inc in incoming)
                {
                    var existing = notifications.FirstOrDefault(n => n.Id == inc.Id);
                    if (existing == null)
                    {
                        notifications.Add(inc);
                        added.Add(inc);
                    }
                    else
                    {
                        // Refresh server-controlled fields, keep local state
                        existing.Title = inc.Title;
                        existing.BodyHtml = inc.BodyHtml;
                        existing.BodyMarkdown = inc.BodyMarkdown;
                        existing.SeverityRaw = inc.SeverityRaw;
                        existing.IsPinned = inc.IsPinned;
                        existing.RequiresAck = inc.RequiresAck;
                        existing.PublishAt = inc.PublishAt;
                        existing.ExpiresAt = inc.ExpiresAt;
                        existing.Attachments = inc.Attachments;
                        existing.Links = inc.Links;
                        MergePolls(existing, inc);

                        // Server-side delivery timestamp wins if newer
                        if (!string.IsNullOrEmpty(inc.State?.DeliveredAt))
                            existing.State.DeliveredAt = inc.State.DeliveredAt;
                        if (!string.IsNullOrEmpty(inc.State?.ReadAt))
                            existing.State.ReadAt ??= inc.State.ReadAt;
                        if (!string.IsNullOrEmpty(inc.State?.AcknowledgedAt))
                            existing.State.AcknowledgedAt ??= inc.State.AcknowledgedAt;
                    }
                }
                Save();
            }
            return added;
        }

        private static void MergePolls(Notification existing, Notification inc)
        {
            if (inc.Polls == null || inc.Polls.Count == 0)
                return;

            foreach (var newPoll in inc.Polls)
            {
                var oldPoll = existing.Polls.FirstOrDefault(p => p.Id == newPoll.Id);
                if (oldPoll == null)
                {
                    existing.Polls.Add(newPoll);
                }
                else
                {
                    oldPoll.Question = newPoll.Question;
                    oldPoll.IsMultiSelect = newPoll.IsMultiSelect;
                    oldPoll.IsAnonymous = newPoll.IsAnonymous;
                    oldPoll.ClosesAt = newPoll.ClosesAt;
                    oldPoll.Options = newPoll.Options;
                }
            }
        }

        public void MarkRead(long id)
        {
            lock (sync)
            {
                var n = notifications.FirstOrDefault(n => n.Id == id);
                if (n == null || n.State.ReadAt != null) return;
                n.State.ReadAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Save();
            }
        }

        public void MarkAcknowledged(long id)
        {
            lock (sync)
            {
                var n = notifications.FirstOrDefault(n => n.Id == id);
                if (n == null) return;
                n.State.AcknowledgedAt ??= DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Save();
            }
        }

        public void MarkDismissed(long id)
        {
            lock (sync)
            {
                var n = notifications.FirstOrDefault(n => n.Id == id);
                if (n == null) return;
                n.State.DismissedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Save();
            }
        }

        public void RecordVote(long notificationId, long pollId, IReadOnlyList<long> optionIds, PollVoteResult? result)
        {
            lock (sync)
            {
                var n = notifications.FirstOrDefault(n => n.Id == notificationId);
                if (n == null) return;
                var poll = n.Polls.FirstOrDefault(p => p.Id == pollId);
                if (poll == null) return;

                poll.Voted = true;
                poll.VotedOptionIds = optionIds.ToList();
                if (result != null && result.Success)
                {
                    poll.TotalVoters = result.Voters;
                    // Merge result percentages back into options
                    foreach (var resOpt in result.Options)
                    {
                        var optId = resOpt.OptionId ?? resOpt.Id;
                        var opt = poll.Options.FirstOrDefault(o => o.Id == optId);
                        if (opt != null)
                        {
                            opt.Votes = resOpt.Votes;
                            opt.Percentage = resOpt.Percentage;
                        }
                    }
                }
                n.State.AcknowledgedAt ??= DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Save();
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(filePath)) return;
                var json = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<List<Notification>>(json);
                if (list != null) notifications = list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationStore] Load failed: {ex.Message}");
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(notifications, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationStore] Save failed: {ex.Message}");
            }
        }
    }
}
