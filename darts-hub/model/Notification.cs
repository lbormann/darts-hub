using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace darts_hub.model
{
    /// <summary>
    /// Severity of a notification (drives icon/colour in the UI).
    /// </summary>
    public enum NotificationSeverity
    {
        Info,
        Success,
        Warning,
        Critical,
        Announcement
    }

    /// <summary>
    /// File or image attached to a notification.
    /// </summary>
    public class NotificationAttachment
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("kind")] public string Kind { get; set; } = string.Empty;
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("mime_type")] public string MimeType { get; set; } = string.Empty;
        [JsonProperty("size_bytes")] public long SizeBytes { get; set; }
        [JsonProperty("url")] public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// External link rendered as a clickable item.
    /// </summary>
    public class NotificationLink
    {
        [JsonProperty("label")] public string Label { get; set; } = string.Empty;
        [JsonProperty("url")] public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Selectable answer option of a poll.
    /// </summary>
    public class NotificationPollOption
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("label")] public string Label { get; set; } = string.Empty;
        [JsonProperty("votes")] public int? Votes { get; set; }
        [JsonProperty("percentage")] public double? Percentage { get; set; }
        [JsonProperty("option_id")] public long? OptionId { get; set; }
    }

    /// <summary>
    /// Poll attached to a notification.
    /// </summary>
    public class NotificationPoll
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("question")] public string Question { get; set; } = string.Empty;
        [JsonProperty("is_multi_select")] public bool IsMultiSelect { get; set; }
        [JsonProperty("is_anonymous")] public bool IsAnonymous { get; set; }
        [JsonProperty("closes_at")] public string? ClosesAt { get; set; }
        [JsonProperty("options")] public List<NotificationPollOption> Options { get; set; } = new();

        // Local-only state
        [JsonProperty("voted")] public bool Voted { get; set; }
        [JsonProperty("voted_option_ids")] public List<long> VotedOptionIds { get; set; } = new();
        [JsonProperty("total_voters")] public int? TotalVoters { get; set; }
    }

    /// <summary>
    /// Per-device state (delivered / read / acknowledged / dismissed).
    /// </summary>
    public class NotificationState
    {
        [JsonProperty("delivered_at")] public string? DeliveredAt { get; set; }
        [JsonProperty("read_at")] public string? ReadAt { get; set; }
        [JsonProperty("acknowledged_at")] public string? AcknowledgedAt { get; set; }
        [JsonProperty("dismissed_at")] public string? DismissedAt { get; set; }
    }

    /// <summary>
    /// A single notification from the server, plus local state.
    /// </summary>
    public class Notification
    {
        [JsonProperty("id")] public long Id { get; set; }
        [JsonProperty("title")] public string Title { get; set; } = string.Empty;
        [JsonProperty("body_html")] public string BodyHtml { get; set; } = string.Empty;
        [JsonProperty("body_markdown")] public string? BodyMarkdown { get; set; }
        [JsonProperty("severity")] public string SeverityRaw { get; set; } = "info";
        [JsonProperty("is_pinned")] public bool IsPinned { get; set; }
        [JsonProperty("requires_ack")] public bool RequiresAck { get; set; }
        [JsonProperty("publish_at")] public string? PublishAt { get; set; }
        [JsonProperty("expires_at")] public string? ExpiresAt { get; set; }
        [JsonProperty("min_version")] public string? MinVersion { get; set; }
        [JsonProperty("max_version")] public string? MaxVersion { get; set; }

        [JsonProperty("attachments")] public List<NotificationAttachment> Attachments { get; set; } = new();
        [JsonProperty("links")] public List<NotificationLink> Links { get; set; } = new();
        [JsonProperty("polls")] public List<NotificationPoll> Polls { get; set; } = new();
        [JsonProperty("state")] public NotificationState State { get; set; } = new();

        [JsonIgnore]
        public NotificationSeverity Severity => SeverityRaw?.ToLowerInvariant() switch
        {
            "success" => NotificationSeverity.Success,
            "warning" => NotificationSeverity.Warning,
            "critical" => NotificationSeverity.Critical,
            "announcement" => NotificationSeverity.Announcement,
            _ => NotificationSeverity.Info
        };

        [JsonIgnore]
        public bool IsUnread =>
            State.ReadAt is null &&
            State.DismissedAt is null &&
            !IsExpired;

        [JsonIgnore]
        public bool IsExpired
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExpiresAt)) return false;
                return DateTime.TryParse(ExpiresAt, out var dt) && dt < DateTime.UtcNow;
            }
        }
    }
}
