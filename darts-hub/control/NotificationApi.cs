using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using darts_hub.model;

namespace darts_hub.control
{
    /// <summary>
    /// Response from /api/notifications/poll.
    /// </summary>
    public class NotificationPollResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long ServerTime { get; set; }
        public int PollInterval { get; set; } = 60;
        public List<Notification> Notifications { get; set; } = new();
    }

    /// <summary>
    /// Response payload returned after casting a poll vote.
    /// </summary>
    public class PollVoteResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long PollId { get; set; }
        public int Voters { get; set; }
        public List<NotificationPollOption> Options { get; set; } = new();
    }

    /// <summary>
    /// HTTP client for the Darts-Hub notification endpoints
    /// (same HMAC-SHA256 scheme as the license API).
    /// </summary>
    public class NotificationApi
    {
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };

        private readonly string baseUrl;
        private readonly string apiKey;
        private readonly string secretKey;

        public NotificationApi(string baseUrl, string apiKey, string secretKey)
        {
            ArgumentNullException.ThrowIfNull(baseUrl);
            ArgumentNullException.ThrowIfNull(apiKey);
            ArgumentNullException.ThrowIfNull(secretKey);

            this.baseUrl = baseUrl.TrimEnd('/');
            this.apiKey = apiKey;
            this.secretKey = secretKey;
        }

        public string BaseUrl => baseUrl;

        public async Task<NotificationPollResult> PollAsync(string? licenseKey, string hardwareId, long sinceId, CancellationToken ct = default)
        {
            var body = new JObject
            {
                ["license_key"] = licenseKey ?? string.Empty,
                ["hardware_id"] = hardwareId,
                ["since_id"] = sinceId
            }.ToString(Formatting.None);

            var raw = await PostAsync("/api/notifications/poll", body, ct);
            return ParsePollResponse(raw);
        }

        public async Task<bool> AckAsync(string? licenseKey, string hardwareId, long notificationId, string action, CancellationToken ct = default)
        {
            var body = new JObject
            {
                ["license_key"] = licenseKey ?? string.Empty,
                ["hardware_id"] = hardwareId,
                ["notification_id"] = notificationId,
                ["action"] = action
            }.ToString(Formatting.None);

            try
            {
                var raw = await PostAsync("/api/notifications/ack", body, ct);
                var json = JObject.Parse(raw);
                return json["success"]?.Value<bool>() ?? false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationApi] AckAsync failed: {ex.Message}");
                return false;
            }
        }

        public async Task<PollVoteResult> VoteAsync(string? licenseKey, string hardwareId, long pollId, IReadOnlyList<long> optionIds, CancellationToken ct = default)
        {
            var body = new JObject
            {
                ["license_key"] = licenseKey ?? string.Empty,
                ["hardware_id"] = hardwareId,
                ["poll_id"] = pollId,
                ["option_ids"] = new JArray(optionIds)
            }.ToString(Formatting.None);

            var raw = await PostAsync("/api/notifications/vote", body, ct);
            return ParseVoteResponse(raw);
        }

        private async Task<string> PostAsync(string path, string body, CancellationToken ct)
        {
            var url = baseUrl + path;
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(request, body);

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            Debug.WriteLine($"[NotificationApi] POST {path} -> {(int)response.StatusCode}");
            return responseBody;
        }

        private void AddAuthHeaders(HttpRequestMessage request, string body)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var payload = apiKey + timestamp + body;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            request.Headers.Add("X-API-Key", apiKey);
            request.Headers.Add("X-API-Timestamp", timestamp);
            request.Headers.Add("X-API-Signature", signature);
        }

        private static NotificationPollResult ParsePollResponse(string body)
        {
            var result = new NotificationPollResult();
            try
            {
                var json = JObject.Parse(body);
                result.Success = json["success"]?.Value<bool>() ?? false;
                result.Message = json["message"]?.Value<string>();

                var data = json["data"] as JObject;
                if (data != null)
                {
                    result.ServerTime = data["server_time"]?.Value<long>() ?? 0;
                    result.PollInterval = data["poll_interval"]?.Value<int>() ?? 60;

                    if (data["notifications"] is JArray arr)
                    {
                        foreach (var n in arr)
                        {
                            var notif = n.ToObject<Notification>();
                            if (notif != null) result.Notifications.Add(notif);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationApi] ParsePollResponse failed: {ex.Message}");
                result.Success = false;
                result.Message = "Failed to parse response.";
            }
            return result;
        }

        private static PollVoteResult ParseVoteResponse(string body)
        {
            var result = new PollVoteResult();
            try
            {
                var json = JObject.Parse(body);
                result.Success = json["success"]?.Value<bool>() ?? false;
                result.Message = json["message"]?.Value<string>();

                var data = json["data"] as JObject;
                if (data != null)
                {
                    result.PollId = data["poll_id"]?.Value<long>() ?? 0;
                    var results = data["results"] as JObject;
                    if (results != null)
                    {
                        result.Voters = results["voters"]?.Value<int>() ?? 0;
                        if (results["options"] is JArray opts)
                        {
                            foreach (var o in opts)
                            {
                                var opt = o.ToObject<NotificationPollOption>();
                                if (opt != null) result.Options.Add(opt);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationApi] ParseVoteResponse failed: {ex.Message}");
                result.Success = false;
                result.Message = "Failed to parse response.";
            }
            return result;
        }
    }
}
