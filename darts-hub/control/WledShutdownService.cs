using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using darts_hub.model;
using Newtonsoft.Json;

namespace darts_hub.control
{
    /// <summary>
    /// Executes WLED close actions (turn off or activate preset) for configured devices.
    /// </summary>
    public static class WledShutdownService
    {
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

        /// <summary>
        /// Executes the configured close action for every WLED device in the list.
        /// Errors are logged but never thrown so that application shutdown is not blocked.
        /// </summary>
        public static async Task ExecuteAllAsync(IReadOnlyList<WledDeviceConfig> devices)
        {
            if (devices == null || devices.Count == 0)
                return;

            var tasks = new List<Task>(devices.Count);
            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.Endpoint))
                    continue;

                tasks.Add(ExecuteDeviceActionAsync(device));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WledShutdown] One or more device actions failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends the configured action to a single WLED device.
        /// </summary>
        private static async Task ExecuteDeviceActionAsync(WledDeviceConfig device)
        {
            try
            {
                var baseUrl = NormalizeEndpoint(device.Endpoint);
                var url = $"{baseUrl}/json/state";

                object payload = device.Action switch
                {
                    WledCloseAction.TurnOff => new { on = false },
                    WledCloseAction.ActivatePreset => new { on = true, ps = device.PresetId },
                    _ => new { on = false }
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[WledShutdown] {device.Action} executed on {device.Endpoint}");
                }
                else
                {
                    Debug.WriteLine($"[WledShutdown] {device.Action} failed on {device.Endpoint}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WledShutdown] Error on {device.Endpoint}: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries available presets from a WLED device.
        /// Returns a dictionary of preset-id to preset-name, or null on failure.
        /// </summary>
        public static async Task<Dictionary<int, string>?> QueryPresetsAsync(string endpoint)
        {
            return await WledApi.QueryPresetsAsync(endpoint);
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return "http://" + endpoint;
            }
            return endpoint;
        }
    }
}
