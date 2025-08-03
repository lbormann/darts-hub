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

        /// <summary>
        /// Tests an effect by sending it to the first reachable WLED endpoint
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <param name="effectName">Name of the effect to test</param>
        /// <returns>True if effect was sent successfully, false otherwise</returns>
        public static async Task<bool> TestEffectAsync(AppBase app, string effectName)
        {
            var endpoints = ExtractWledEndpoints(app);
            
            foreach (var endpoint in endpoints)
            {
                // First get the effect ID from the effects list
                var effects = await QueryEffectsAsync(endpoint);
                if (effects == null) continue;
                
                var effectId = effects.IndexOf(effectName);
                if (effectId < 0) continue; // Effect not found
                
                try
                {
                    // Ensure proper URL format
                    string endpointUrl = endpoint;
                    if (!endpointUrl.StartsWith("http://") && !endpointUrl.StartsWith("https://"))
                    {
                        endpointUrl = "http://" + endpointUrl;
                    }

                    var url = $"{endpointUrl}/json/state";
                    
                    // Create JSON payload to set the effect
                    var payload = new
                    {
                        on = true,
                        fx = effectId,
                        bri = 128 // Medium brightness for testing
                    };
                    
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent effect '{effectName}' (ID: {effectId}) to {endpoint}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send effect to {endpoint}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending effect to {endpoint}: {ex.Message}");
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
            var endpoints = ExtractWledEndpoints(app);
            
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
                    
                    // Create JSON payload to activate the preset
                    var payload = new
                    {
                        on = true,
                        ps = presetId,
                        bri = 128 // Medium brightness for testing
                    };
                    
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent preset {presetId} to {endpoint}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send preset to {endpoint}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending preset to {endpoint}: {ex.Message}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Tests a color effect by sending it to the first reachable WLED endpoint
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <param name="colorEffect">Color effect string (e.g., "red", "green", "#FF0000")</param>
        /// <returns>True if color was sent successfully, false otherwise</returns>
        public static async Task<bool> TestColorAsync(AppBase app, string colorEffect)
        {
            var endpoints = ExtractWledEndpoints(app);
            
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
                    
                    // Parse color effect to RGB values
                    var (r, g, b) = ParseColorEffect(colorEffect);
                    
                    // Create JSON payload to set solid color
                    var payload = new
                    {
                        on = true,
                        fx = 0, // Solid color effect
                        col = new[] { new[] { r, g, b } },
                        bri = 128 // Medium brightness for testing
                    };
                    
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent color '{colorEffect}' (RGB: {r},{g},{b}) to {endpoint}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to send color to {endpoint}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending color to {endpoint}: {ex.Message}");
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
            var endpoints = ExtractWledEndpoints(app);
            
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
                    
                    // Create JSON payload to turn off WLED
                    var payload = new
                    {
                        on = false
                    };
                    
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully stopped effects on {endpoint}");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to stop effects on {endpoint}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping effects on {endpoint}: {ex.Message}");
                }
            }
            
            return false;
        }

        /// <summary>
        /// Parses a color effect string to RGB values
        /// </summary>
        /// <param name="colorEffect">Color effect string</param>
        /// <returns>RGB values as tuple</returns>
        private static (int r, int g, int b) ParseColorEffect(string colorEffect)
        {
            if (string.IsNullOrEmpty(colorEffect))
                return (255, 255, 255); // Default white

            colorEffect = colorEffect.ToLowerInvariant().Trim();

            // Handle hex colors
            if (colorEffect.StartsWith("#"))
            {
                try
                {
                    var hex = colorEffect.Substring(1);
                    if (hex.Length == 6)
                    {
                        var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        var b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return (r, g, b);
                    }
                }
                catch
                {
                    // Fall through to default colors
                }
            }

            // Handle named colors
            return colorEffect switch
            {
                "red" => (255, 0, 0),
                "green" => (0, 255, 0),
                "blue" => (0, 0, 255),
                "yellow" => (255, 255, 0),
                "cyan" => (0, 255, 255),
                "magenta" => (255, 0, 255),
                "orange" => (255, 165, 0),
                "purple" => (128, 0, 128),
                "pink" => (255, 192, 203),
                "white" => (255, 255, 255),
                "black" => (0, 0, 0),
                _ => (255, 255, 255) // Default white
            };
        }
    }
}