using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using darts_hub.model;
using System.IO;

namespace darts_hub.control
{
    /// <summary>
    /// WLED API client for reading effects and presets from local data file or fallback to WLED controllers
    /// </summary>
    public class WledApi
    {
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

        // WLED API Data Models
        public class WledInfo
        {
            [JsonProperty("effects")]
            public List<string>? Effects { get; set; }
            
            [JsonProperty("palettes")]
            public List<string>? Palettes { get; set; }
        }

        public class WledPreset
        {
            [JsonProperty("n")]
            public string? Name { get; set; }
            
            [JsonProperty("ql")]
            public string? QuickLabel { get; set; }
        }

        public class WledState
        {
            [JsonProperty("seg")]
            public List<WledSegment>? Segments { get; set; }
        }

        public class WledSegment
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            
            [JsonProperty("start")]
            public int Start { get; set; }
            
            [JsonProperty("stop")]
            public int Stop { get; set; }
            
            [JsonProperty("len")]
            public int Length { get; set; }
        }

        // Data model for a single endpoint entry (used in both v1 flat format and v2 per-endpoint entries)
        public class WledDataFile
        {
            [JsonProperty("endpoint")]
            public string? Endpoint { get; set; }

            [JsonProperty("effects")]
            public WledDataEffects? Effects { get; set; }

            [JsonProperty("presets")]
            public Dictionary<string, object>? Presets { get; set; }

            [JsonProperty("palettes")]
            public WledDataPalettes? Palettes { get; set; }

            [JsonProperty("info")]
            public object? Info { get; set; }

            [JsonProperty("state")]
            public object? State { get; set; }

            [JsonProperty("segments")]
            public object? Segments { get; set; }

            [JsonProperty("last_updated")]
            public string? LastUpdated { get; set; }

            [JsonProperty("data_hash")]
            public string? DataHash { get; set; }
        }

        // Schema version 2 container with per-endpoint data
        public class WledDataFileV2
        {
            [JsonProperty("schema_version")]
            public int SchemaVersion { get; set; }

            [JsonProperty("primary_endpoint")]
            public string? PrimaryEndpoint { get; set; }

            [JsonProperty("configured_endpoints")]
            public List<string>? ConfiguredEndpoints { get; set; }

            [JsonProperty("endpoints")]
            public Dictionary<string, WledDataFile>? Endpoints { get; set; }

            [JsonProperty("last_updated")]
            public string? LastUpdated { get; set; }
        }

        public class WledDataEffects
        {
            [JsonProperty("names")]
            public List<string>? Names { get; set; }

            [JsonProperty("ids")]
            public List<int>? Ids { get; set; }
        }

        public class WledDataPalettes
        {
            [JsonProperty("names")]
            public List<string>? Names { get; set; }

            [JsonProperty("ids")]
            public List<int>? Ids { get; set; }
        }

        // Cached v2 data to avoid repeated file reads within the same session
        private static WledDataFileV2? _cachedV2Data;
        private static DateTime _cachedV2Timestamp = DateTime.MinValue;

        // Fallback effect categories
        public static readonly Dictionary<string, List<string>> FallbackEffectCategories = new()
        {
            ["Basic Effects"] = new List<string>
            {
                "Solid", "Blink", "Breathe", "Wipe", "Wipe Random", "Random Colors", "Sweep", "Dynamic", 
                "Colorloop", "Rainbow", "Scan", "Dual Scan", "Fade", "Theater", "Running", "Saw", 
                "Twinkle", "Dissolve", "Dissolve Rnd", "Sparkle", "Dark Sparkle", "Sparkle+", 
                "Strobe", "Strobe Rainbow", "Blink Rainbow"
            },
            ["Advanced Effects"] = new List<string>
            {
                "Android", "Chase", "Chase 2", "Chase 3", "Chase Rainbow", "Chase Flash", "Chase Flash Rnd", 
                "Rainbow Runner", "Colorful", "Traffic Light", "Sweep Random", "Running 2", "Red & Blue", 
                "Stream", "Scanner", "Lighthouse", "Fireworks", "Rain", "Merry Christmas", "Fire Flicker", 
                "Gradient", "Loading", "Police", "Police All", "Two Dots", "Two Areas", "Circus", "Halloween", 
                "Tri Chase", "Tri Wipe", "Tri Fade", "Lightning", "ICU", "Multi Comet", "Dual Scanner", 
                "Stream 2", "Oscillate", "Pride 2015", "Juggle", "Palette", "Fire 2012", "Colorwaves", 
                "Bpm", "Fill Noise", "Noise 1", "Noise 2", "Noise 3", "Noise 4"
            },
            ["Matrix Effects"] = new List<string>
            {
                "Pacifica", "Sunrise", "Phased", "Twinklefox", "Twinklecat", "Halloween Eyes", "Solid Pattern", 
                "Solid Pattern Tri", "Spots", "Spots Fade", "Glitter", "Candle", "Fireworks Starburst", 
                "Fireworks 1D", "Bouncing Balls", "Sinelon", "Sinelon Dual", "Sinelon Rainbow", "Popcorn", 
                "Drip", "Plasma", "Percent", "Ripple Rainbow", "Heartbeat", "Candle Multi", "Solid Glitter"
            }
        };

        // Fallback presets
        public static readonly List<string> FallbackPresets = new List<string>
        {
            "Preset 1 - Warm White", "Preset 2 - Cool White", "Preset 3 - Red", "Preset 4 - Green", 
            "Preset 5 - Blue", "Preset 6 - Yellow", "Preset 7 - Orange", "Preset 8 - Pink", 
            "Preset 9 - Purple", "Preset 10 - Cyan", "Preset 11 - Rainbow", "Preset 12 - Fire", 
            "Preset 13 - Ocean", "Preset 14 - Forest", "Preset 15 - Party", "Preset 16 - Sunset", 
            "Preset 17 - Sunrise", "Preset 18 - Christmas", "Preset 19 - Halloween", "Preset 20 - Custom"
        };

        // Fallback palettes
        public static readonly List<string> FallbackPalettes = new List<string>
        {
            "Default", "Random Cycle", "Primary Color", "Based on Primary", "Set Colors", "Based on Set",
            "Party", "Cloud", "Lava", "Ocean", "Forest", "Rainbow", "Rainbow Bands", "Sunset",
            "Rivendell", "Breeze", "Red & Blue", "Yellowout", "Analogous", "Splash", "Pastel",
            "Sunset 2", "Beech", "Vintage", "Departure", "Landscape", "Beach", "Sherbet", "Hult",
            "Hult 64", "Drywet", "Jul", "Grintage", "Rewhi", "Tertiary", "Fire", "Icefire",
            "Cyane", "Light Pink", "Autumn", "Magenta", "Magred", "Yelmag", "Yelblu", "Orange & Teal",
            "Tiamat", "April Night", "Orangery", "C9", "Sakura", "Aurora", "Atlantica"
        };

        /// <summary>
        /// Resolves the path to the wled_data.json file
        /// </summary>
        private static string? ResolveWledDataFilePath()
        {
            var executableDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ??
                                     Directory.GetCurrentDirectory();

            var dataFilePath = Path.Combine(executableDirectory, "darts-wled", "wled_data.json");
            if (File.Exists(dataFilePath))
                return dataFilePath;

            var fallbackPath = Path.Combine("darts-wled", "wled_data.json");
            if (File.Exists(fallbackPath))
                return fallbackPath;

            System.Diagnostics.Debug.WriteLine($"WLED data file not found at: {dataFilePath} or {fallbackPath}");
            return null;
        }

        /// <summary>
        /// Loads the full v2 container from the wled_data.json file.
        /// Supports both old flat format (auto-migrated) and new schema_version 2 format.
        /// Uses a short-lived cache (5 seconds) to avoid excessive file reads.
        /// </summary>
        public static WledDataFileV2? LoadWledDataFileV2()
        {
            try
            {
                // Return cached data if still fresh
                if (_cachedV2Data != null && (DateTime.UtcNow - _cachedV2Timestamp).TotalSeconds < 5)
                    return _cachedV2Data;

                var filePath = ResolveWledDataFilePath();
                if (filePath == null)
                    return null;

                var jsonContent = File.ReadAllText(filePath);
                var raw = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);

                var schemaVersion = raw["schema_version"]?.ToObject<int>() ?? 0;

                WledDataFileV2 v2;

                if (schemaVersion >= 2)
                {
                    // New multi-endpoint format
                    v2 = raw.ToObject<WledDataFileV2>() ?? new WledDataFileV2();
                    System.Diagnostics.Debug.WriteLine($"Loaded wled_data.json schema v{schemaVersion}: primary={v2.PrimaryEndpoint}, endpoints={v2.Endpoints?.Count ?? 0}");
                }
                else
                {
                    // Old flat format — wrap into v2 structure
                    var legacy = raw.ToObject<WledDataFile>();
                    v2 = new WledDataFileV2
                    {
                        SchemaVersion = 1,
                        PrimaryEndpoint = legacy?.Endpoint,
                        ConfiguredEndpoints = legacy?.Endpoint != null ? new List<string> { legacy.Endpoint } : new List<string>(),
                        Endpoints = new Dictionary<string, WledDataFile>(),
                        LastUpdated = legacy?.LastUpdated
                    };

                    if (legacy != null && !string.IsNullOrEmpty(legacy.Endpoint))
                    {
                        v2.Endpoints[legacy.Endpoint] = legacy;
                    }
                    else if (legacy != null)
                    {
                        // No endpoint field — store under a placeholder key so data is still accessible
                        v2.Endpoints["_default"] = legacy;
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded wled_data.json old format, migrated to v2 wrapper: endpoint={legacy?.Endpoint}");
                }

                _cachedV2Data = v2;
                _cachedV2Timestamp = DateTime.UtcNow;
                return v2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading WLED data file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads WLED data for the primary endpoint (backward compatible).
        /// Returns the same WledDataFile as before for all existing callers.
        /// </summary>
        public static WledDataFile? LoadWledDataFile()
        {
            var v2 = LoadWledDataFileV2();
            if (v2 == null) return null;

            return GetEndpointData(v2, null);
        }

        /// <summary>
        /// Loads WLED data for a specific endpoint from the v2 container.
        /// Falls back to the primary endpoint if the specific endpoint is not found.
        /// </summary>
        public static WledDataFile? LoadWledDataForEndpoint(string? endpoint)
        {
            var v2 = LoadWledDataFileV2();
            if (v2 == null) return null;

            return GetEndpointData(v2, endpoint);
        }

        /// <summary>
        /// Resolves endpoint data from the v2 container.
        /// When endpoint is null or not found, returns data for the primary endpoint.
        /// </summary>
        private static WledDataFile? GetEndpointData(WledDataFileV2 v2, string? endpoint)
        {
            if (v2.Endpoints == null || v2.Endpoints.Count == 0)
                return null;

            // Try exact match first
            if (!string.IsNullOrEmpty(endpoint) && v2.Endpoints.TryGetValue(endpoint, out var exactMatch))
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Loaded data for endpoint: {endpoint}");
                return exactMatch;
            }

            // Try case-insensitive / trimmed match
            if (!string.IsNullOrEmpty(endpoint))
            {
                var normalised = endpoint.Trim().TrimEnd('/');
                var match = v2.Endpoints.FirstOrDefault(kvp =>
                    string.Equals(kvp.Key.Trim().TrimEnd('/'), normalised, StringComparison.OrdinalIgnoreCase));
                if (match.Value != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] Loaded data for endpoint (normalised match): {endpoint} -> {match.Key}");
                    return match.Value;
                }
            }

            // Fall back to primary endpoint
            if (!string.IsNullOrEmpty(v2.PrimaryEndpoint) && v2.Endpoints.TryGetValue(v2.PrimaryEndpoint, out var primary))
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Falling back to primary endpoint: {v2.PrimaryEndpoint}" +
                    (endpoint != null ? $" (requested: {endpoint})" : ""));
                return primary;
            }

            // Last resort: return the first available entry
            var first = v2.Endpoints.Values.FirstOrDefault();
            if (first != null)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Falling back to first available endpoint: {first.Endpoint}");
            }
            return first;
        }

        /// <summary>
        /// Queries a WLED controller for available effects
        /// </summary>
        /// <param name="wledEndpoint">WLED IP address or hostname</param>
        /// <returns>List of available effects or null if query failed</returns>
        public static async Task<List<string>?> QueryEffectsAsync(string wledEndpoint)
        {
            try
            {
                // Ensure proper URL format
                if (!wledEndpoint.StartsWith("http://") && !wledEndpoint.StartsWith("https://"))
                {
                    wledEndpoint = "http://" + wledEndpoint;
                }

                var url = $"{wledEndpoint}/json";
                var response = await httpClient.GetStringAsync(url);
                var info = JsonConvert.DeserializeObject<WledInfo>(response);
                
                return info?.Effects?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to query WLED effects from {wledEndpoint}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queries a WLED controller for available palettes
        /// </summary>
        /// <param name="wledEndpoint">WLED IP address or hostname</param>
        /// <returns>List of available palettes or null if query failed</returns>
        public static async Task<List<string>?> QueryPalettesAsync(string wledEndpoint)
        {
            try
            {
                // Ensure proper URL format
                if (!wledEndpoint.StartsWith("http://") && !wledEndpoint.StartsWith("https://"))
                {
                    wledEndpoint = "http://" + wledEndpoint;
                }

                var url = $"{wledEndpoint}/json";
                var response = await httpClient.GetStringAsync(url);
                var info = JsonConvert.DeserializeObject<WledInfo>(response);
                
                return info?.Palettes?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to query WLED palettes from {wledEndpoint}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queries a WLED controller for available presets
        /// </summary>
        /// <param name="wledEndpoint">WLED IP address or hostname</param>
        /// <returns>Dictionary of preset ID to name or null if query failed</returns>
        public static async Task<Dictionary<int, string>?> QueryPresetsAsync(string wledEndpoint)
        {
            try
            {
                // Ensure proper URL format
                if (!wledEndpoint.StartsWith("http://") && !wledEndpoint.StartsWith("https://"))
                {
                    wledEndpoint = "http://" + wledEndpoint;
                }

                var url = $"{wledEndpoint}/presets.json";
                var response = await httpClient.GetStringAsync(url);
                var presets = JsonConvert.DeserializeObject<Dictionary<string, WledPreset>>(response);
                
                var result = new Dictionary<int, string>();
                if (presets != null)
                {
                    foreach (var kvp in presets)
                    {
                        if (int.TryParse(kvp.Key, out var presetId) && !string.IsNullOrWhiteSpace(kvp.Value?.Name))
                        {
                            result[presetId] = kvp.Value.Name;
                        }
                    }
                }
                
                return result.Count > 0 ? result : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to query WLED presets from {wledEndpoint}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extracts WLED endpoints from app configuration
        /// </summary>
        /// <param name="app">The app to extract endpoints from</param>
        /// <returns>List of WLED endpoints</returns>
        public static List<string> ExtractWledEndpoints(AppBase app)
        {
            var endpoints = new List<string>();
            
            if (app.Configuration?.Arguments != null)
            {
                // Debug output
                System.Diagnostics.Debug.WriteLine($"Searching for WLED endpoints in {app.Configuration.Arguments.Count} arguments");
                
                // Look for WLED endpoints argument (WEPS in darts-wled)
                var wepsArg = app.Configuration.Arguments.FirstOrDefault(arg => 
                    arg.Name.Equals("WEPS", StringComparison.OrdinalIgnoreCase) ||
                    arg.Name.Contains("wled_endpoint", StringComparison.OrdinalIgnoreCase) ||
                    (arg.NameHuman != null && arg.NameHuman.Contains("wled_endpoint", StringComparison.OrdinalIgnoreCase)));
                
                if (wepsArg != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Found WEPS argument: Name='{wepsArg.Name}', Value='{wepsArg.Value}', NameHuman='{wepsArg.NameHuman}'");
                    
                    if (!string.IsNullOrWhiteSpace(wepsArg.Value))
                    {
                        // Split multiple endpoints (space or comma separated)
                        var endpointValues = wepsArg.Value.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var endpoint in endpointValues)
                        {
                            var cleanEndpoint = endpoint.Trim().Trim('"');
                            if (!string.IsNullOrWhiteSpace(cleanEndpoint))
                            {
                                endpoints.Add(cleanEndpoint);
                                System.Diagnostics.Debug.WriteLine($"Added WLED endpoint: {cleanEndpoint}");
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("WEPS argument found but value is empty");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No WEPS argument found. Available arguments:");
                    foreach (var arg in app.Configuration.Arguments.Take(10)) // Show first 10 for debugging
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Name: '{arg.Name}', NameHuman: '{arg.NameHuman}', Value: '{arg.Value}'");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Extracted {endpoints.Count} WLED endpoints");
            return endpoints;
        }

        /// <summary>
        /// Checks if the provided endpoint is reachable
        /// </summary>
        /// <param name="wledEndpoint">WLED IP address or hostname</param>
        /// <returns>True if reachable, false otherwise</returns>
        public static async Task<bool> IsEndpointReachableAsync(string wledEndpoint)
        {
            try
            {
                // Ensure proper URL format
                if (!wledEndpoint.StartsWith("http://") && !wledEndpoint.StartsWith("https://"))
                {
                    wledEndpoint = "http://" + wledEndpoint;
                }

                var url = $"{wledEndpoint}/json/info";
                using var response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resolves the list of WLED endpoints from v2 data or app configuration.
        /// Prefers v2 configured_endpoints, then v2 endpoint keys, then app WEPS argument.
        /// </summary>
        private static List<string> ResolveEndpoints(WledDataFileV2? v2, AppBase app)
        {
            if (v2?.ConfiguredEndpoints != null && v2.ConfiguredEndpoints.Count > 0)
                return new List<string>(v2.ConfiguredEndpoints);

            if (v2?.Endpoints != null && v2.Endpoints.Count > 0)
                return v2.Endpoints.Keys.ToList();

            return ExtractWledEndpoints(app);
        }

        /// <summary>
        /// Gets all effects from local data file or fallback data.
        /// When endpoint is specified, loads data for that specific endpoint.
        /// When null, uses the primary endpoint.
        /// </summary>
        public static async Task<(List<string> effects, string source, bool isLive)> GetEffectsWithFallbackAsync(AppBase app, string? endpoint = null)
        {
            var wledData = LoadWledDataForEndpoint(endpoint);
            if (wledData?.Effects?.Names != null && wledData.Effects.Names.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded {wledData.Effects.Names.Count} effects from local data for endpoint: {wledData.Endpoint ?? "primary"}");
                return (wledData.Effects.Names.Where(e => !string.IsNullOrWhiteSpace(e)).ToList(), 
                       $"Local Data ({wledData.Endpoint})", true);
            }

            System.Diagnostics.Debug.WriteLine($"Local WLED data not available for endpoint '{endpoint ?? "primary"}', using fallback effects");
            var fallbackEffects = FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
            return (fallbackEffects, "Fallback Data", false);
        }

        /// <summary>
        /// Gets all palettes from local data file or fallback data.
        /// When endpoint is specified, loads data for that specific endpoint.
        /// When null, uses the primary endpoint.
        /// </summary>
        public static async Task<(List<string> palettes, string source, bool isLive)> GetPalettesWithFallbackAsync(AppBase app, string? endpoint = null)
        {
            var wledData = LoadWledDataForEndpoint(endpoint);
            if (wledData?.Palettes?.Names != null && wledData.Palettes.Names.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Loaded {wledData.Palettes.Names.Count} palettes from local data for endpoint: {wledData.Endpoint ?? "primary"}");
                return (wledData.Palettes.Names.Where(p => !string.IsNullOrWhiteSpace(p)).ToList(),
                       $"Local Data ({wledData.Endpoint})", true);
            }

            System.Diagnostics.Debug.WriteLine($"Local WLED data not available for endpoint '{endpoint ?? "primary"}', using fallback palettes");
            return (FallbackPalettes, "Fallback Data", false);
        }

        /// <summary>
        /// Gets all presets from local data file or fallback data.
        /// When endpoint is specified, loads data for that specific endpoint.
        /// When null, uses the primary endpoint.
        /// </summary>
        public static async Task<(Dictionary<int, string> presets, string source, bool isLive)> GetPresetsWithFallbackAsync(AppBase app, string? endpoint = null)
        {
            var wledData = LoadWledDataForEndpoint(endpoint);
            if (wledData?.Presets != null && wledData.Presets.Count > 0)
            {
                var presets = new Dictionary<int, string>();

                foreach (var kvp in wledData.Presets)
                {
                    if (int.TryParse(kvp.Key, out var presetId) && presetId > 0)
                    {
                        string presetName = $"Preset {presetId}";

                        if (kvp.Value is Newtonsoft.Json.Linq.JObject presetObj)
                        {
                            var nameToken = presetObj["n"];
                            if (nameToken != null && !string.IsNullOrWhiteSpace(nameToken.ToString()))
                            {
                                presetName = nameToken.ToString();
                            }
                        }

                        presets[presetId] = presetName;
                    }
                }

                if (presets.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Loaded {presets.Count} presets from local data for endpoint: {wledData.Endpoint ?? "primary"}");
                    return (presets, $"Local Data ({wledData.Endpoint})", true);
                }
            }

            System.Diagnostics.Debug.WriteLine($"Local WLED data not available for endpoint '{endpoint ?? "primary"}', using fallback presets");
            var fallbackPresets = new Dictionary<int, string>();
            for (int i = 0; i < FallbackPresets.Count; i++)
            {
                fallbackPresets[i + 1] = FallbackPresets[i].Replace($"Preset {i + 1} - ", "");
            }

            return (fallbackPresets, "Fallback Data", false);
        }

        /// <summary>
        /// Tests an effect by sending it to all segments of the first reachable WLED endpoint
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <param name="effectName">Name of the effect to test</param>
        /// <param name="palette">Optional palette to test with</param>
        /// <param name="speed">Optional speed (1-255)</param>
        /// <param name="intensity">Optional intensity (1-255)</param>
        /// <returns>True if effect was sent successfully, false otherwise</returns>
        public static async Task<bool> TestEffectAsync(AppBase app, string effectName, string? palette = null, int? speed = null, int? intensity = null)
        {
            var v2 = LoadWledDataFileV2();
            var endpoints = ResolveEndpoints(v2, app);

            foreach (var endpoint in endpoints)
            {
                try
                {
                    // Load per-endpoint data for effect/palette ID lookup
                    var epData = GetEndpointData(v2!, endpoint) ?? LoadWledDataForEndpoint(endpoint);

                    int effectId = -1;

                    if (epData?.Effects?.Names != null && epData.Effects.Ids != null)
                    {
                        var effectIndex = epData.Effects.Names.IndexOf(effectName);
                        if (effectIndex >= 0 && effectIndex < epData.Effects.Ids.Count)
                        {
                            effectId = epData.Effects.Ids[effectIndex];
                        }
                        else
                        {
                            effectIndex = epData.Effects.Names.FindIndex(e => 
                                string.Equals(e, effectName, StringComparison.OrdinalIgnoreCase));
                            if (effectIndex >= 0 && effectIndex < epData.Effects.Ids.Count)
                            {
                                effectId = epData.Effects.Ids[effectIndex];
                            }
                        }
                    }
                    
                    // If not found in local data, query the endpoint directly
                    if (effectId < 0)
                    {
                        var effects = await QueryEffectsAsync(endpoint);
                        if (effects != null)
                        {
                            effectId = effects.IndexOf(effectName);
                            if (effectId < 0)
                            {
                                // Try case-insensitive search
                                effectId = effects.FindIndex(e => 
                                    string.Equals(e, effectName, StringComparison.OrdinalIgnoreCase));
                            }
                            
                            if (effectId >= 0)
                            {
                                System.Diagnostics.Debug.WriteLine($"Found effect '{effectName}' via live query with ID: {effectId}");
                            }
                        }
                    }
                    
                    if (effectId < 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Effect '{effectName}' not found on {endpoint}");
                        continue; // Effect not found, try next endpoint
                    }
                
                    // Get palette ID if palette is specified
                    int? paletteId = null;
                    if (!string.IsNullOrEmpty(palette))
                    {
                        if (epData?.Palettes?.Names != null && epData.Palettes.Ids != null)
                        {
                            var paletteIndex = epData.Palettes.Names.IndexOf(palette);
                            if (paletteIndex >= 0 && paletteIndex < epData.Palettes.Ids.Count)
                            {
                                paletteId = epData.Palettes.Ids[paletteIndex];
                            }
                            else
                            {
                                paletteIndex = epData.Palettes.Names.FindIndex(p => 
                                    string.Equals(p, palette, StringComparison.OrdinalIgnoreCase));
                                if (paletteIndex >= 0 && paletteIndex < epData.Palettes.Ids.Count)
                                {
                                    paletteId = epData.Palettes.Ids[paletteIndex];
                                }
                            }
                        }
                        
                        // If not found in local data, query the endpoint
                        if (!paletteId.HasValue)
                        {
                            var palettes = await QueryPalettesAsync(endpoint);
                            if (palettes != null)
                            {
                                var paletteIndex = palettes.IndexOf(palette);
                                if (paletteIndex < 0)
                                {
                                    // Try case-insensitive search
                                    paletteIndex = palettes.FindIndex(p => 
                                        string.Equals(p, palette, StringComparison.OrdinalIgnoreCase));
                                }
                                
                                if (paletteIndex >= 0)
                                {
                                    paletteId = paletteIndex;
                                    System.Diagnostics.Debug.WriteLine($"Found palette '{palette}' via live query with ID: {paletteId}");
                                }
                            }
                        }
                        
                        if (!paletteId.HasValue)
                        {
                            System.Diagnostics.Debug.WriteLine($"Palette '{palette}' not found, using default");
                        }
                    }
                
                    // Query available segments
                    var segmentIds = await QuerySegmentsAsync(endpoint);
                
                    // Ensure proper URL format
                    string endpointUrl = endpoint;
                    if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    {
                        endpointUrl = "http://" + endpointUrl;
                    }

                    var url = $"{endpointUrl}/json/state";
                    
                    // Create segments array - apply effect to all available segments
                    var segments = new List<object>();
                    
                    if (segmentIds != null && segmentIds.Count > 0)
                    {
                        // Create segment configuration for each available segment
                        foreach (var segmentId in segmentIds)
                        {
                            var segment = new
                            {
                                id = segmentId,                            // Segment ID
                                fx = effectId,                             // Effect ID
                                sx = speed ?? 128,                         // Speed (default 128 if not specified)
                                ix = intensity ?? 128,                     // Intensity (default 128 if not specified)  
                                pal = paletteId ?? 0,                      // Palette ID (default 0 = Default if not specified)
                                col = new int[][]                          // Color array with primary, secondary, background
                                {
                                    new int[] { 255, 160, 0 },             // Primary color (orange)
                                    new int[] { 0, 255, 0 },               // Secondary color (green)
                                    new int[] { 0, 0, 255 }                // Background color (blue)
                                }
                            };
                            segments.Add(segment);
                        }
                    }
                    else
                    {
                        // Fallback: create default segment if no segments found
                        System.Diagnostics.Debug.WriteLine($"No segments found on {endpoint}, creating default segment 0");
                        
                        var defaultSegment = new
                        {
                            id = 0,                                        // Default segment ID
                            fx = effectId,                                 // Effect ID
                            sx = speed ?? 128,                             // Speed (default 128 if not specified)
                            ix = intensity ?? 128,                         // Intensity (default 128 if not specified)
                            pal = paletteId ?? 0,                          // Palette ID (default 0 = Default if not specified)
                            col = new int[][]                              // Color array with primary, secondary, background
                            {
                                new int[] { 255, 160, 0 },                 // Primary color (orange)
                                new int[] { 0, 255, 0 },                   // Secondary color (green)
                                new int[] { 0, 0, 255 }                    // Background color (blue)
                            }
                        };
                        segments.Add(defaultSegment);
                    }
                    
                    // Create the complete JSON payload according to specification
                    var payload = new
                    {
                        on = true,                                         // Turn on WLED
                        bri = 255,                                         // Brightness (max for testing)
                        seg = segments.ToArray(),                          // Segments array
                        transition = 7,                                    // Transition duration in 100ms steps (700ms)
                        tt = 7,                                            // Alternative transition property (WLED 0.14+)
                        ps = -1                                            // No preset save/activate
                    };
                    
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        Formatting = Formatting.Indented
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Sending WLED effect test payload to {endpoint} for {segments.Count} segment(s):");
                    System.Diagnostics.Debug.WriteLine($"Effect: '{effectName}' (ID: {effectId})");
                    if (paletteId.HasValue) 
                        System.Diagnostics.Debug.WriteLine($"Palette: '{palette}' (ID: {paletteId})");
                    else 
                        System.Diagnostics.Debug.WriteLine("Palette: Default (ID: 0)");
                    System.Diagnostics.Debug.WriteLine($"Speed: {speed ?? 128}, Intensity: {intensity ?? 128}");
                    System.Diagnostics.Debug.WriteLine(json);
                    
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent effect '{effectName}' (ID: {effectId}) to {segments.Count} segment(s) on {endpoint}");
                        if (paletteId.HasValue) System.Diagnostics.Debug.WriteLine($"  with palette '{palette}' (ID: {paletteId})");
                        System.Diagnostics.Debug.WriteLine($"  with speed: {speed ?? 128}");
                        System.Diagnostics.Debug.WriteLine($"  with intensity: {intensity ?? 128}");
                        if (segmentIds != null) System.Diagnostics.Debug.WriteLine($"  applied to segments: [{string.Join(", ", segmentIds)}]");
                        return true;
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Failed to send effect to {endpoint}: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Response body: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending effect to {endpoint}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Tests a preset by sending it to the first reachable WLED endpoint
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <param name="presetId">ID of the preset to test (1-based)</param>
        /// <returns>True if preset was sent successfully, false otherwise</returns>
        public static async Task<bool> TestPresetAsync(AppBase app, int presetId)
        {
            var v2 = LoadWledDataFileV2();
            var endpoints = ResolveEndpoints(v2, app);
            
            foreach (var endpoint in endpoints)
            {
                if (!await IsEndpointReachableAsync(endpoint)) continue;
                
                try
                {
                    // Ensure proper URL format
                    string endpointUrl = endpoint;
                    if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    {
                        endpointUrl = "http://" + endpointUrl;
                    }

                    var url = $"{endpointUrl}/json/state";
                    
                    // Create the JSON payload to activate the preset according to specification
                    var payload = new
                    {
                        on = true,                                         // Turn on WLED
                        bri = 255,                                         // Brightness (max for testing)
                        ps = presetId,                                     // Preset ID to activate
                        transition = 7,                                    // Transition duration in 100ms steps (700ms)
                        tt = 7                                             // Alternative transition property (WLED 0.14+)
                    };
                    
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        Formatting = Formatting.Indented
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Sending WLED preset test payload to {endpoint}:");
                    System.Diagnostics.Debug.WriteLine(json);
                    
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent preset {presetId} to {endpoint}");
                        return true;
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Failed to send preset to {endpoint}: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Response body: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending preset to {endpoint}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Tests a color effect by sending it to all segments of the first reachable WLED endpoint
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <param name="colorEffect">Color effect string (e.g., "red", "solid|green", "#FF0000")</param>
        /// <returns>True if color was sent successfully, false otherwise</returns>
        public static async Task<bool> TestColorAsync(AppBase app, string colorEffect)
        {
            var v2 = LoadWledDataFileV2();
            var endpoints = ResolveEndpoints(v2, app);
            
            foreach (var endpoint in endpoints)
            {
                if (!await IsEndpointReachableAsync(endpoint)) continue;
                
                try
                {
                    // Ensure proper URL format
                    string endpointUrl = endpoint;
                    if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    {
                        endpointUrl = "http://" + endpointUrl;
                    }

                    var url = $"{endpointUrl}/json/state";
                    
                    // For testing, use the original color name without "solid|" prefix
                    var originalColorName = WledColorDefinitions.GetOriginalColorName(colorEffect);
                    
                    // Parse color effect to RGB values using the original color name
                    var (r, g, b) = WledColorDefinitions.ParseColorEffect(originalColorName);
                    
                    // Query available segments
                    var segmentIds = await QuerySegmentsAsync(endpoint);
                    
                    // Create segments array - apply color to all available segments
                    var segments = new List<object>();
                    
                    if (segmentIds != null && segmentIds.Count > 0)
                    {
                        // Create segment configuration for each available segment
                        foreach (var segmentId in segmentIds)
                        {
                            var segment = new
                            {
                                id = segmentId,                            // Segment ID
                                fx = 0,                                    // Solid color effect
                                sx = 128,                                  // Speed (not really used for solid color)
                                ix = 255,                                  // Intensity (max for solid color)
                                pal = 0,                                   // Default palette for solid color
                                col = new int[][]                          // Color array with the selected color
                                {
                                    new int[] { r, g, b },                 // Primary color (selected color)
                                    new int[] { 0, 0, 0 },                 // Secondary color (black)
                                    new int[] { 0, 0, 0 }                  // Background color (black)
                                }
                            };
                            segments.Add(segment);
                        }
                    }
                    else
                    {
                        // Fallback: create default segment if no segments found
                        System.Diagnostics.Debug.WriteLine($"No segments found on {endpoint}, creating default segment 0");
                        
                        var defaultSegment = new
                        {
                            id = 0,                                        // Default segment ID
                            fx = 0,                                        // Solid color effect
                            sx = 128,                                      // Speed (not really used for solid color)
                            ix = 255,                                      // Intensity (max for solid color)
                            pal = 0,                                       // Default palette for solid color
                            col = new int[][]                              // Color array with the selected color
                            {
                                new int[] { r, g, b },                     // Primary color (selected color)
                                new int[] { 0, 0, 0 },                     // Secondary color (black)
                                new int[] { 0, 0, 0 }                      // Background color (black)
                            }
                        };
                        segments.Add(defaultSegment);
                    }
                    
                    // Create the complete JSON payload according to specification
                    var payload = new
                    {
                        on = true,                                         // Turn on WLED
                        bri = 255,                                         // Brightness (max for testing)
                        seg = segments.ToArray(),                          // Segments array
                        transition = 7,                                    // Transition duration in 100ms steps (700ms)
                        tt = 7,                                            // Alternative transition property (WLED 0.14+)
                        ps = -1                                            // No preset save/activate
                    };
                    
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        Formatting = Formatting.Indented
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Sending WLED color test payload to {endpoint} for {segments.Count} segment(s):");
                    System.Diagnostics.Debug.WriteLine($"Original color input: '{colorEffect}' -> Test color name: '{originalColorName}' -> RGB: ({r},{g},{b})");
                    System.Diagnostics.Debug.WriteLine(json);
                    
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent color '{originalColorName}' (RGB: {r},{g},{b}) to {segments.Count} segment(s) on {endpoint}");
                        if (segmentIds != null) System.Diagnostics.Debug.WriteLine($"  applied to segments: [{string.Join(", ", segmentIds)}]");
                        return true;
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Failed to send color to {endpoint}: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Response body: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending color to {endpoint}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Stops all effects on the first reachable WLED endpoint (turns off)
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <returns>True if stop command was sent successfully, false otherwise</returns>
        public static async Task<bool> StopEffectsAsync(AppBase app)
        {
            var v2 = LoadWledDataFileV2();
            var endpoints = ResolveEndpoints(v2, app);
            
            foreach (var endpoint in endpoints)
            {
                if (!await IsEndpointReachableAsync(endpoint)) continue;
                
                try
                {
                    // Ensure proper URL format
                    string endpointUrl = endpoint;
                    if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    {
                        endpointUrl = "http://" + endpointUrl;
                    }

                    var url = $"{endpointUrl}/json/state";
                    
                    // Create JSON payload to turn off WLED with transition
                    var payload = new
                    {
                        on = false,                                        // Turn off WLED
                        transition = 7,                                    // Transition duration in 100ms steps (700ms)
                        tt = 7                                             // Alternative transition property (WLED 0.14+)
                    };
                    
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Include,
                        Formatting = Formatting.Indented
                    });
                    
                    System.Diagnostics.Debug.WriteLine($"Sending WLED stop payload to {endpoint}:");
                    System.Diagnostics.Debug.WriteLine(json);
                    
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully stopped effects on {endpoint}");
                        return true;
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Failed to stop effects on {endpoint}: {response.StatusCode}");
                        System.Diagnostics.Debug.WriteLine($"Response body: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping effects on {endpoint}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Queries a WLED controller for segment information
        /// </summary>
        /// <param name="wledEndpoint">WLED IP address or hostname</param>
        /// <returns>List of segment IDs or null if query failed</returns>
        public static async Task<List<int>?> QuerySegmentsAsync(string wledEndpoint)
        {
            try
            {
                // Ensure proper URL format
                if (!wledEndpoint.StartsWith("http://") && !wledEndpoint.StartsWith("https://"))
                {
                    wledEndpoint = "http://" + wledEndpoint;
                }

                var url = $"{wledEndpoint}/json/state";
                var response = await httpClient.GetStringAsync(url);
                
                // Parse as dynamic object to extract segment IDs
                dynamic? state = JsonConvert.DeserializeObject(response);
                var segmentIds = new List<int>();
                
                if (state?.seg != null)
                {
                    foreach (var segment in state.seg)
                    {
                        if (segment?.id != null)
                        {
                            segmentIds.Add((int)segment.id);
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Found {segmentIds.Count} segments on {wledEndpoint}: [{string.Join(", ", segmentIds)}]");
                
                return segmentIds.Count > 0 ? segmentIds : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to query WLED segments from {wledEndpoint}: {ex.Message}");
                return null;
            }
        }

        // ===== Endpoint-specific methods for multi-device support =====

        /// <summary>
        /// Tests a parsed WLED effect value string on a specific endpoint.
        /// Parses the value to extract effect ID, speed, intensity, palette, and duration.
        /// </summary>
        public static async Task<bool> TestEffectValueOnEndpointAsync(string endpoint, string effectValue)
        {
            try
            {
                var epData = LoadWledDataForEndpoint(endpoint);
                var effectIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (epData?.Effects?.Names != null && epData.Effects.Ids != null)
                {
                    var count = Math.Min(epData.Effects.Names.Count, epData.Effects.Ids.Count);
                    for (var i = 0; i < count; i++)
                    {
                        var name = epData.Effects.Names[i];
                        var id = epData.Effects.Ids[i];
                        if (!string.IsNullOrWhiteSpace(name))
                            effectIdByName[name] = id;
                    }
                }

                var parts = effectValue.Split('|');
                if (parts.Length == 0) return false;

                var effectToken = parts[0];
                if (effectToken.StartsWith("fx", StringComparison.OrdinalIgnoreCase))
                    effectToken = effectToken.Substring(2);

                int effectId;
                if (!int.TryParse(effectToken, out effectId))
                {
                    if (!effectIdByName.TryGetValue(parts[0], out effectId))
                    {
                        var effects = await QueryEffectsAsync(endpoint);
                        if (effects != null)
                        {
                            effectId = effects.FindIndex(e =>
                                string.Equals(e, parts[0], StringComparison.OrdinalIgnoreCase));
                        }
                        if (effectId < 0) return false;
                    }
                }

                int speed = 128, intensity = 128;
                int? paletteId = null;

                for (int i = 1; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;

                    if (part.StartsWith("s") && int.TryParse(part.Substring(1), out var s))
                        speed = Math.Max(1, Math.Min(255, s));
                    else if (part.StartsWith("i") && int.TryParse(part.Substring(1), out var ix))
                        intensity = Math.Max(1, Math.Min(255, ix));
                    else if (part.StartsWith("p") && int.TryParse(part.Substring(1), out var p))
                        paletteId = p;
                }

                var segmentIds = await QuerySegmentsAsync(endpoint);

                string endpointUrl = endpoint;
                if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    endpointUrl = "http://" + endpointUrl;

                var url = $"{endpointUrl}/json/state";

                var segments = new List<object>();
                var segIds = segmentIds ?? new List<int> { 0 };
                foreach (var segId in segIds)
                {
                    segments.Add(new
                    {
                        id = segId,
                        fx = effectId,
                        sx = speed,
                        ix = intensity,
                        pal = paletteId ?? 0,
                        col = new int[][] { new[] { 255, 160, 0 }, new[] { 0, 255, 0 }, new[] { 0, 0, 255 } }
                    });
                }

                var payload = new { on = true, bri = 255, seg = segments.ToArray(), transition = 7, tt = 7, ps = -1 };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);

                System.Diagnostics.Debug.WriteLine($"[WLED] TestEffectValueOnEndpoint {endpoint}: {(response.IsSuccessStatusCode ? "OK" : response.StatusCode.ToString())}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] TestEffectValueOnEndpoint {endpoint} error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests a preset on a specific WLED endpoint
        /// </summary>
        public static async Task<bool> TestPresetOnEndpointAsync(string endpoint, int presetId)
        {
            try
            {
                string endpointUrl = endpoint;
                if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    endpointUrl = "http://" + endpointUrl;

                var url = $"{endpointUrl}/json/state";
                var payload = new { on = true, ps = presetId };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);

                System.Diagnostics.Debug.WriteLine($"[WLED] TestPresetOnEndpoint {endpoint} preset={presetId}: {(response.IsSuccessStatusCode ? "OK" : response.StatusCode.ToString())}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] TestPresetOnEndpoint {endpoint} error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests a color effect on a specific WLED endpoint
        /// </summary>
        public static async Task<bool> TestColorOnEndpointAsync(string endpoint, string colorEffect)
        {
            try
            {
                var originalColorName = WledColorDefinitions.GetOriginalColorName(colorEffect);
                var (r, g, b) = WledColorDefinitions.ParseColorEffect(originalColorName);

                var segmentIds = await QuerySegmentsAsync(endpoint);

                string endpointUrl = endpoint;
                if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    endpointUrl = "http://" + endpointUrl;

                var url = $"{endpointUrl}/json/state";

                var segments = new List<object>();
                var segIds = segmentIds ?? new List<int> { 0 };
                foreach (var segId in segIds)
                {
                    segments.Add(new
                    {
                        id = segId,
                        fx = 0,
                        sx = 128,
                        ix = 255,
                        pal = 0,
                        col = new int[][] { new[] { r, g, b }, new[] { 0, 0, 0 }, new[] { 0, 0, 0 } }
                    });
                }

                var payload = new { on = true, bri = 255, seg = segments.ToArray(), transition = 7, tt = 7, ps = -1 };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);

                System.Diagnostics.Debug.WriteLine($"[WLED] TestColorOnEndpoint {endpoint} color={colorEffect}: {(response.IsSuccessStatusCode ? "OK" : response.StatusCode.ToString())}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] TestColorOnEndpoint {endpoint} error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops effects on a specific WLED endpoint
        /// </summary>
        public static async Task<bool> StopEffectsOnEndpointAsync(string endpoint)
        {
            try
            {
                string endpointUrl = endpoint;
                if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    endpointUrl = "http://" + endpointUrl;

                var url = $"{endpointUrl}/json/state";
                var payload = new { on = false, transition = 7, tt = 7 };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);

                System.Diagnostics.Debug.WriteLine($"[WLED] StopEffectsOnEndpoint {endpoint}: {(response.IsSuccessStatusCode ? "OK" : response.StatusCode.ToString())}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] StopEffectsOnEndpoint {endpoint} error: {ex.Message}");
                return false;
            }
        }
    }
}