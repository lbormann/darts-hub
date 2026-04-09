using System;
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
    /// Executes WLED lifecycle actions (start / close) for configured devices.
    /// </summary>
    public static class WledShutdownService
    {
        private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(3) };

        #region Close Actions

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

                tasks.Add(ExecuteCloseActionAsync(device));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WledShutdown] One or more close actions failed: {ex.Message}");
            }
        }

        private static async Task ExecuteCloseActionAsync(WledDeviceConfig device)
        {
            object payload = device.Action switch
            {
                WledCloseAction.TurnOff => new { on = false },
                WledCloseAction.ActivatePreset => new { on = true, ps = device.PresetId },
                _ => new { on = false }
            };

            await SendPayloadAsync(device.Endpoint, payload, "Close", device.Action.ToString());
        }

        #endregion

        #region Start Actions

        /// <summary>
        /// Executes the configured start action for every WLED device in the list.
        /// Errors are logged but never thrown so that application startup is not blocked.
        /// </summary>
        public static async Task ExecuteStartAllAsync(IReadOnlyList<WledStartDeviceConfig> devices)
        {
            if (devices == null || devices.Count == 0)
                return;

            var tasks = new List<Task>(devices.Count);
            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.Endpoint))
                    continue;

                tasks.Add(ExecuteStartActionAsync(device));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WledStartup] One or more start actions failed: {ex.Message}");
            }
        }

        private static async Task ExecuteStartActionAsync(WledStartDeviceConfig device)
        {
            object payload = device.Action switch
            {
                WledStartAction.TurnOn => new { on = true },
                WledStartAction.ActivatePreset => new { on = true, ps = device.PresetId },
                _ => new { on = true }
            };

            await SendPayloadAsync(device.Endpoint, payload, "Start", device.Action.ToString());
        }

        #endregion

        /// <summary>
        /// Queries available presets from a WLED device.
        /// Returns a dictionary of preset-id to preset-name, or null on failure.
        /// </summary>
        public static async Task<Dictionary<int, string>?> QueryPresetsAsync(string endpoint)
        {
            return await WledApi.QueryPresetsAsync(endpoint);
        }

        #region Helpers

        private static async Task SendPayloadAsync(string endpoint, object payload, string phase, string action)
        {
            try
            {
                var baseUrl = NormalizeEndpoint(endpoint);
                var url = $"{baseUrl}/json/state";

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[Wled{phase}] {action} executed on {endpoint}");
                }
                else
                {
                    Debug.WriteLine($"[Wled{phase}] {action} failed on {endpoint}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Wled{phase}] Error on {endpoint}: {ex.Message}");
            }
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

        #endregion
    }
}
