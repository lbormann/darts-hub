using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// Result of a license API call
    /// </summary>
    public class LicenseResult
    {
        public bool Success { get; set; }
        public bool Valid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = "unknown";
        public JObject? Features { get; set; }
        public string? ExpiresAt { get; set; }
        public string? ErrorDetail { get; set; }

        public bool HasFeature(string featureKey)
        {
            if (Features == null) return false;
            return Features.TryGetValue(featureKey, out var val) && val.ToString() == "1";
        }
    }

    /// <summary>
    /// HTTP client for the Darts-Hub License Server API.
    /// Handles HMAC-SHA256 authentication and license validation/activation.
    /// </summary>
    public class LicenseClient
    {
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly string baseUrl;
        private readonly string apiKey;
        private readonly string secretKey;

        public LicenseClient(string baseUrl, string apiKey, string secretKey)
        {
            ArgumentNullException.ThrowIfNull(baseUrl);
            ArgumentNullException.ThrowIfNull(apiKey);
            ArgumentNullException.ThrowIfNull(secretKey);

            this.baseUrl = baseUrl.TrimEnd('/');
            this.apiKey = apiKey;
            this.secretKey = secretKey;
        }

        /// <summary>
        /// Validates a license key against the server.
        /// </summary>
        public async Task<LicenseResult> ValidateAsync(string licenseKey, string? hardwareId = null, CancellationToken ct = default)
        {
            var bodyObj = new JObject { ["license_key"] = licenseKey };
            if (!string.IsNullOrWhiteSpace(hardwareId))
                bodyObj["hardware_id"] = hardwareId;

            var body = bodyObj.ToString(Formatting.None);
            return await PostAsync("/api/license/validate", body, ct);
        }

        /// <summary>
        /// Activates a license with a hardware ID binding.
        /// </summary>
        public async Task<LicenseResult> ActivateAsync(string licenseKey, string hardwareId, CancellationToken ct = default)
        {
            var bodyObj = new JObject
            {
                ["license_key"] = licenseKey,
                ["hardware_id"] = hardwareId
            };

            var body = bodyObj.ToString(Formatting.None);
            return await PostAsync("/api/license/activate", body, ct);
        }

        /// <summary>
        /// Retrieves feature flags for a license.
        /// </summary>
        public async Task<LicenseResult> GetFeaturesAsync(string licenseKey, CancellationToken ct = default)
        {
            var url = $"/api/license/features?license_key={Uri.EscapeDataString(licenseKey)}";
            return await GetAsync(url, ct);
        }

        private async Task<LicenseResult> PostAsync(string path, string body, CancellationToken ct)
        {
            var url = baseUrl + path;
            Debug.WriteLine($"[LicenseClient] POST {url}");
            Debug.WriteLine($"[LicenseClient] Request body: {body}");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                AddAuthHeaders(request, body);

                LogRequestHeaders(request);

                var response = await httpClient.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                Debug.WriteLine($"[LicenseClient] Response status: {(int)response.StatusCode} {response.StatusCode}");
                Debug.WriteLine($"[LicenseClient] Response body: {responseBody}");

                var result = ParseResponse(responseBody);
                Debug.WriteLine($"[LicenseClient] Parsed result: Success={result.Success}, Valid={result.Valid}, Status={result.Status}, Message={result.Message}");
                if (result.Features != null)
                    Debug.WriteLine($"[LicenseClient] Features: {result.Features.ToString(Formatting.None)}");

                return result;
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"[LicenseClient] POST {url} timed out");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Request timed out.",
                    ErrorDetail = "The license server did not respond in time."
                };
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[LicenseClient] POST {url} connection error: {ex.Message}");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Connection error.",
                    ErrorDetail = ex.Message
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseClient] POST {url} unexpected error: {ex.Message}");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Unexpected error.",
                    ErrorDetail = ex.Message
                };
            }
        }

        private async Task<LicenseResult> GetAsync(string path, CancellationToken ct)
        {
            var url = baseUrl + path;
            Debug.WriteLine($"[LicenseClient] GET {url}");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddAuthHeaders(request, string.Empty);

                LogRequestHeaders(request);

                var response = await httpClient.SendAsync(request, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                Debug.WriteLine($"[LicenseClient] Response status: {(int)response.StatusCode} {response.StatusCode}");
                Debug.WriteLine($"[LicenseClient] Response body: {responseBody}");

                var result = ParseResponse(responseBody);
                Debug.WriteLine($"[LicenseClient] Parsed result: Success={result.Success}, Valid={result.Valid}, Status={result.Status}, Message={result.Message}");

                return result;
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"[LicenseClient] GET {url} timed out");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Request timed out.",
                    ErrorDetail = "The license server did not respond in time."
                };
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"[LicenseClient] GET {url} connection error: {ex.Message}");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Connection error.",
                    ErrorDetail = ex.Message
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseClient] GET {url} unexpected error: {ex.Message}");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Unexpected error.",
                    ErrorDetail = ex.Message
                };
            }
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

            Debug.WriteLine($"[LicenseClient] Auth: API-Key={apiKey}, Timestamp={timestamp}, Signature={signature[..16]}...");
        }

        private static void LogRequestHeaders(HttpRequestMessage request)
        {
            foreach (var header in request.Headers)
            {
                Debug.WriteLine($"[LicenseClient] Header: {header.Key}={string.Join(", ", header.Value)}");
            }
        }

        private static LicenseResult ParseResponse(string responseBody)
        {
            try
            {
                var json = JObject.Parse(responseBody);
                var data = json["data"] as JObject;

                return new LicenseResult
                {
                    Success = json["success"]?.Value<bool>() ?? false,
                    Valid = json["valid"]?.Value<bool>() ?? false,
                    Message = json["message"]?.Value<string>() ?? string.Empty,
                    Status = data?["status"]?.Value<string>() ?? "unknown",
                    Features = data?["features"] as JObject,
                    ExpiresAt = data?["expires_at"]?.Value<string>()
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseClient] Failed to parse response: {ex.Message}");
                Debug.WriteLine($"[LicenseClient] Raw response: {responseBody}");
                return new LicenseResult
                {
                    Success = false,
                    Valid = false,
                    Message = "Failed to parse server response.",
                    ErrorDetail = ex.Message
                };
            }
        }
    }
}
