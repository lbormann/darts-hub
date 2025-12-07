using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace darts_hub.model
{
    /// <summary>
    /// Represents a Robbel3D configuration preset that includes WLED config, presets, and darts-hub settings
    /// Enhanced to support external WLED config and presets files
    /// </summary>
    public class Robbel3DConfiguration
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        
        [JsonProperty("description")]
        public string Description { get; set; } = "";
        
        [JsonProperty("version")]
        public string Version { get; set; } = "";
        
        [JsonProperty("created")]
        public DateTime Created { get; set; } = DateTime.Now;
        
        [JsonProperty("author")]
        public string? Author { get; set; }
        
        [JsonProperty("tags")]
        public List<string>? Tags { get; set; }
        
        [JsonProperty("led_count")]
        public int LedCount { get; set; }
        
        [JsonProperty("dartboard_type")]
        public string? DartboardType { get; set; }
        
        /// <summary>
        /// Reference to external WLED configuration file (e.g., "wled_cfg.json")
        /// </summary>
        [JsonProperty("wled_config_file")]
        public string? WledConfigFile { get; set; }
        
        /// <summary>
        /// Reference to external WLED presets file (e.g., "wled_presets.json")
        /// </summary>
        [JsonProperty("wled_presets_file")]
        public string? WledPresetsFile { get; set; }
        
        /// <summary>
        /// Auto-start WLED extension after configuration has been applied
        /// </summary>
        [JsonProperty("auto_start_wled")]
        public bool AutoStartWled { get; set; } = true;
        
        /// <summary>
        /// Darts-Caller application settings (all arguments)
        /// </summary>
        [JsonProperty("caller_settings")]
        public Dictionary<string, string> CallerSettings { get; set; } = new();
        
        /// <summary>
        /// Darts-WLED extension settings (all arguments)
        /// </summary>
        [JsonProperty("wled_settings")]
        public Dictionary<string, string> WledSettings { get; set; } = new();
        
        /// <summary>
        /// Additional extension settings (future extensibility)
        /// </summary>
        [JsonProperty("extension_settings")]
        public Dictionary<string, Dictionary<string, string>>? ExtensionSettings { get; set; }
        
        /// <summary>
        /// Configuration metadata
        /// </summary>
        [JsonProperty("metadata")]
        public Robbel3DConfigurationMetadata? Metadata { get; set; }

        // Runtime properties - loaded from external files
        [JsonIgnore]
        public WledConfiguration? WledConfig { get; set; }
        
        /// <summary>
        /// Raw WLED config as dynamic object (preserves ALL fields from cfg.json)
        /// This is used instead of WledConfig to avoid losing fields during serialization
        /// </summary>
        [JsonIgnore]
        public dynamic? WledConfigRaw { get; set; }
        
        [JsonIgnore]
        public Dictionary<int, object>? WledPresets { get; set; }
    }

    /// <summary>
    /// Metadata for Robbel3D configuration
    /// </summary>
    public class Robbel3DConfigurationMetadata
    {
        [JsonProperty("created_with")]
        public string? CreatedWith { get; set; }
        
        [JsonProperty("created_with_version")]
        public string? CreatedWithVersion { get; set; }
        
        [JsonProperty("target_wled_version")]
        public string? TargetWledVersion { get; set; }
        
        [JsonProperty("target_caller_version")]
        public string? TargetCallerVersion { get; set; }
        
        [JsonProperty("compatibility_notes")]
        public string? CompatibilityNotes { get; set; }
        
        [JsonProperty("changelog")]
        public List<string>? Changelog { get; set; }
    }

    /// <summary>
    /// Simple WLED configuration structure for basic config.json support
    /// </summary>
    public class WledConfiguration
    {
        [JsonProperty("hw")]
        public WledHardware Hardware { get; set; } = new();
        
        [JsonProperty("light")]
        public WledLight Light { get; set; } = new();
        
        [JsonProperty("def")]
        public WledDefaults Defaults { get; set; } = new();
        
        /// <summary>
        /// Network configuration (preserved from existing device)
        /// </summary>
        [JsonProperty("nw")]
        public object? Network { get; set; }
        
        /// <summary>
        /// Device ID (preserved from existing device)
        /// </summary>
        [JsonProperty("id")]
        public object? DeviceId { get; set; }
        
        /// <summary>
        /// Device name (preserved from existing device)
        /// </summary>
        [JsonProperty("nme")]
        public string? DeviceName { get; set; }
        
        /// <summary>
        /// Additional properties for full WLED compatibility
        /// This allows the config to handle any additional WLED fields
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }

    public class WledHardware
    {
        [JsonProperty("led")]
        public WledLedHardware Led { get; set; } = new();
    }

    public class WledLedHardware
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class WledLight
    {
        // Light properties can be added as needed
    }

    public class WledDefaults
    {
        [JsonProperty("on")]
        public bool On { get; set; }
        
        [JsonProperty("bri")]
        public int Brightness { get; set; }
    }

    /// <summary>
    /// Root configuration file structure for Robbel3D presets
    /// </summary>
    public class Robbel3DConfigurationFile
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "2.0";
        
        [JsonProperty("created")]
        public DateTime Created { get; set; }
        
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        [JsonProperty("configurations")]
        public List<Robbel3DConfiguration> Configurations { get; set; } = new();
    }
}