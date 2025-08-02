using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using darts_hub.model;

namespace darts_hub.control
{
    /// <summary>
    /// WLED API client for querying effects and presets from WLED controllers
    /// </summary>
    public class WledApi
    {
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

        // WLED API Data Models
        public class WledInfo
        {
            [JsonProperty("effects")]
            public List<string>? Effects { get; set; }
        }

        public class WledPreset
        {
            [JsonProperty("n")]
            public string? Name { get; set; }
            
            [JsonProperty("ql")]
            public string? QuickLabel { get; set; }
        }

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

                var url = $"{wledEndpoint}/json/info";
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
        /// Gets all effects from the first reachable endpoint or fallback data
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <returns>Tuple of (effects list, source description, is live data)</returns>
        public static async Task<(List<string> effects, string source, bool isLive)> GetEffectsWithFallbackAsync(AppBase app)
        {
            var endpoints = ExtractWledEndpoints(app);
            
            // Try live data first
            foreach (var endpoint in endpoints)
            {
                var liveEffects = await QueryEffectsAsync(endpoint);
                if (liveEffects != null && liveEffects.Count > 0)
                {
                    return (liveEffects, endpoint, true);
                }
            }
            
            // Fallback to static data
            var fallbackEffects = FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
            return (fallbackEffects, "Fallback Data", false);
        }

        /// <summary>
        /// Gets all presets from the first reachable endpoint or fallback data
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <returns>Tuple of (presets dictionary, source description, is live data)</returns>
        public static async Task<(Dictionary<int, string> presets, string source, bool isLive)> GetPresetsWithFallbackAsync(AppBase app)
        {
            var endpoints = ExtractWledEndpoints(app);
            
            // Try live data first
            foreach (var endpoint in endpoints)
            {
                var livePresets = await QueryPresetsAsync(endpoint);
                if (livePresets != null && livePresets.Count > 0)
                {
                    return (livePresets, endpoint, true);
                }
            }
            
            // Fallback to static data - convert to dictionary
            var fallbackPresets = new Dictionary<int, string>();
            for (int i = 0; i < FallbackPresets.Count; i++)
            {
                fallbackPresets[i + 1] = FallbackPresets[i].Replace($"Preset {i + 1} - ", "");
            }
            
            return (fallbackPresets, "Fallback Data", false);
        }
    }
}