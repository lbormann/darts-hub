using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace darts_hub.model
{
    /// <summary>
    /// Defines what action to perform on a WLED device when darts-hub closes.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WledCloseAction
    {
        TurnOff,
        ActivatePreset
    }

    /// <summary>
    /// Defines what action to perform on a WLED device when darts-hub starts.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WledStartAction
    {
        TurnOn,
        ActivatePreset
    }

    /// <summary>
    /// Configuration for a single WLED device that should be controlled when darts-hub closes.
    /// </summary>
    public class WledDeviceConfig
    {
        /// <summary>
        /// IP address or hostname of the WLED device (e.g. "192.168.1.100").
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Action to execute on this device when darts-hub closes.
        /// </summary>
        public WledCloseAction Action { get; set; } = WledCloseAction.TurnOff;

        /// <summary>
        /// Preset ID to activate (only used when Action is ActivatePreset).
        /// </summary>
        public int PresetId { get; set; } = 1;

        /// <summary>
        /// Cached preset names fetched from the device (preset-id ? name).
        /// Persisted so the dropdown is available after restart without re-fetching.
        /// </summary>
        public Dictionary<int, string> CachedPresets { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a single WLED device that should be controlled when darts-hub starts.
    /// </summary>
    public class WledStartDeviceConfig
    {
        /// <summary>
        /// IP address or hostname of the WLED device (e.g. "192.168.1.100").
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Action to execute on this device when darts-hub starts.
        /// </summary>
        public WledStartAction Action { get; set; } = WledStartAction.TurnOn;

        /// <summary>
        /// Preset ID to activate (only used when Action is ActivatePreset).
        /// </summary>
        public int PresetId { get; set; } = 1;

        /// <summary>
        /// Cached preset names fetched from the device (preset-id ? name).
        /// Persisted so the dropdown is available after restart without re-fetching.
        /// </summary>
        public Dictionary<int, string> CachedPresets { get; set; } = new();
    }
}
