using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace darts_hub.control.wizard
{
    public class WizardArgumentsConfig
    {
        public Dictionary<string, ExtensionConfig> Extensions { get; set; } = new();
        public Dictionary<string, string> CommonDefaults { get; set; } = new();

        private static WizardArgumentsConfig? _instance;
        private static readonly object _lock = new object();

        public static WizardArgumentsConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        private static WizardArgumentsConfig LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "control", "wizard", "WizardArgumentsConfig.json");
                System.Diagnostics.Debug.WriteLine($"[WizardConfig] Trying to load config from: {configPath}");
                System.Diagnostics.Debug.WriteLine($"[WizardConfig] File exists: {File.Exists(configPath)}");
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    System.Diagnostics.Debug.WriteLine($"[WizardConfig] JSON file size: {json.Length} characters");
                    System.Diagnostics.Debug.WriteLine($"[WizardConfig] JSON preview: {json.Substring(0, Math.Min(200, json.Length))}...");
                    
                    var config = JsonConvert.DeserializeObject<WizardArgumentsConfig>(json);
                    if (config != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WizardConfig] Successfully loaded config with {config.Extensions.Count} extensions");
                        foreach (var ext in config.Extensions.Keys)
                        {
                            System.Diagnostics.Debug.WriteLine($"[WizardConfig]   - {ext}");
                        }
                        return config;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[WizardConfig] Config deserialization returned null");
                    }
                }
                else
                {
                    // Try alternative paths
                    var altPath1 = Path.Combine(Environment.CurrentDirectory, "control", "wizard", "WizardArgumentsConfig.json");
                    var altPath2 = Path.Combine(Environment.CurrentDirectory, "WizardArgumentsConfig.json");
                    var altPath3 = "WizardArgumentsConfig.json";
                    
                    System.Diagnostics.Debug.WriteLine($"[WizardConfig] Alternative path 1: {altPath1} - exists: {File.Exists(altPath1)}");
                    System.Diagnostics.Debug.WriteLine($"[WizardConfig] Alternative path 2: {altPath2} - exists: {File.Exists(altPath2)}");
                    System.Diagnostics.Debug.WriteLine($"[WizardConfig] Alternative path 3: {altPath3} - exists: {File.Exists(altPath3)}");
                    
                    if (File.Exists(altPath1))
                    {
                        var json = File.ReadAllText(altPath1);
                        var config = JsonConvert.DeserializeObject<WizardArgumentsConfig>(json);
                        System.Diagnostics.Debug.WriteLine($"[WizardConfig] Loaded from alt path 1 with {config?.Extensions.Count} extensions");
                        return config ?? new WizardArgumentsConfig();
                    }
                    else if (File.Exists(altPath2))
                    {
                        var json = File.ReadAllText(altPath2);
                        var config = JsonConvert.DeserializeObject<WizardArgumentsConfig>(json);
                        System.Diagnostics.Debug.WriteLine($"[WizardConfig] Loaded from alt path 2 with {config?.Extensions.Count} extensions");
                        return config ?? new WizardArgumentsConfig();
                    }
                    else if (File.Exists(altPath3))
                    {
                        var json = File.ReadAllText(altPath3);
                        var config = JsonConvert.DeserializeObject<WizardArgumentsConfig>(json);
                        System.Diagnostics.Debug.WriteLine($"[WizardConfig] Loaded from alt path 3 with {config?.Extensions.Count} extensions");
                        return config ?? new WizardArgumentsConfig();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WizardConfig] Failed to load wizard arguments config: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[WizardConfig] Stack trace: {ex.StackTrace}");
            }

            System.Diagnostics.Debug.WriteLine($"[WizardConfig] Returning empty configuration");
            return new WizardArgumentsConfig();
        }

        public ExtensionConfig? GetExtensionConfig(string extensionName)
        {
            System.Diagnostics.Debug.WriteLine($"[WizardConfig] Looking for extension config: '{extensionName}'");
            System.Diagnostics.Debug.WriteLine($"[WizardConfig] Available extensions: {string.Join(", ", Extensions.Keys)}");
            
            var key = extensionName.ToLower();
            if (!key.StartsWith("darts-"))
            {
                key = $"darts-{key}";
            }

            var result = Extensions.TryGetValue(key, out var config) ? config : null;
            System.Diagnostics.Debug.WriteLine($"[WizardConfig] Extension config found: {result != null}");
            
            return result;
        }

        public List<string> GetPrimaryArguments(string extensionName)
        {
            var config = GetExtensionConfig(extensionName);
            return config?.PrimaryArguments ?? new List<string>();
        }

        public string GetDefaultValue(string argumentName)
        {
            return CommonDefaults.TryGetValue(argumentName, out var value) ? value : "";
        }
    }

    public class ExtensionConfig
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonProperty("icon")]
        public string Icon { get; set; } = "";

        [JsonProperty("description")]
        public string Description { get; set; } = "";

        [JsonProperty("primaryArguments")]
        public List<string> PrimaryArguments { get; set; } = new();

        [JsonProperty("sections")]
        public Dictionary<string, SectionConfig> Sections { get; set; } = new();
    }

    public class SectionConfig
    {
        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("expanded")]
        public bool Expanded { get; set; }

        [JsonProperty("arguments")]
        public List<string> Arguments { get; set; } = new();
    }
}