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
        /// Gets all palettes from the first reachable endpoint or fallback data
        /// </summary>
        /// <param name="app">The app containing WLED configuration</param>
        /// <returns>Tuple of (palettes list, source description, is live data)</returns>
        public static async Task<(List<string> palettes, string source, bool isLive)> GetPalettesWithFallbackAsync(AppBase app)
        {
            var endpoints = ExtractWledEndpoints(app);
            
            // Try live data first
            foreach (var endpoint in endpoints)
            {
                var livePalettes = await QueryPalettesAsync(endpoint);
                if (livePalettes != null && livePalettes.Count > 0)
                {
                    return (livePalettes, endpoint, true);
                }
            }
            
            // Fallback to static data
            return (FallbackPalettes, "Fallback Data", false);
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
        /// <param name="palette">Optional palette to test with</param>
        /// <param name="speed">Optional speed (0-255)</param>
        /// <param name="intensity">Optional intensity (0-255)</param>
        /// <returns>True if effect was sent successfully, false otherwise</returns>
        public static async Task<bool> TestEffectAsync(AppBase app, string effectName, string? palette = null, int? speed = null, int? intensity = null)
        {
            var endpoints = ExtractWledEndpoints(app);
            
            foreach (var endpoint in endpoints)
            {
                // First get the effect ID from the effects list
                var effects = await QueryEffectsAsync(endpoint);
                if (effects == null) continue;
                
                var effectId = effects.IndexOf(effectName);
                if (effectId < 0) continue; // Effect not found
                
                // Get palette ID if palette is specified
                int? paletteId = null;
                if (!string.IsNullOrEmpty(palette))
                {
                    var palettes = await QueryPalettesAsync(endpoint);
                    if (palettes != null)
                    {
                        paletteId = palettes.IndexOf(palette);
                        if (paletteId < 0) paletteId = null; // Palette not found, ignore
                    }
                }
                
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
                        pal = paletteId,
                        sx = speed,
                        ix = intensity,
                        bri = 128 // Medium brightness for testing
                    };
                    
                    var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"Successfully sent effect '{effectName}' (ID: {effectId}) to {endpoint}");
                        if (paletteId.HasValue) System.Diagnostics.Debug.WriteLine($"  with palette '{palette}' (ID: {paletteId})");
                        if (speed.HasValue) System.Diagnostics.Debug.WriteLine($"  with speed: {speed}");
                        if (intensity.HasValue) System.Diagnostics.Debug.WriteLine($"  with intensity: {intensity}");
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
        /// Tests a color effect by sending it to all segments of the first reachable WLED endpoint
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
                    
                    // Parse color effect to RGB values using the extracted color definitions
                    var (r, g, b) = WledColorDefinitions.ParseColorEffect(colorEffect);
                    
                    // Query available segments
                    var segmentIds = await QuerySegmentsAsync(endpoint);
                    
                    if (segmentIds == null || segmentIds.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No segments found on {endpoint}, using fallback method");
                        
                        // Fallback to old method if no segments found
                        var fallbackPayload = new
                        {
                            on = true,
                            fx = 0, // Solid color effect
                            col = new[] { new[] { r, g, b } },
                            bri = 128 // Medium brightness for testing
                        };
                        
                        var fallbackJson = JsonConvert.SerializeObject(fallbackPayload);
                        var fallbackContent = new StringContent(fallbackJson, System.Text.Encoding.UTF8, "application/json");
                        
                        System.Diagnostics.Debug.WriteLine($"Sending fallback color command to {endpoint}: {fallbackJson}");
                        
                        var fallbackResponse = await httpClient.PostAsync(url, fallbackContent);
                        
                        if (fallbackResponse.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"Successfully sent color '{colorEffect}' (RGB: {r},{g},{b}) to {endpoint} using fallback method");
                            return true;
                        }
                    }
                    else
                    {
                        // Create segment array with the same color for all segments
                        var segments = segmentIds.Select(segmentId => new
                        {
                            id = segmentId,
                            col = new[] { new[] { r, g, b } },
                            fx = 0 // Solid color effect for each segment
                        }).ToArray();
                        
                        // Create JSON payload with segments
                        var payload = new
                        {
                            on = true,
                            seg = segments,
                            bri = 128 // Medium brightness for testing
                        };
                        
                        var json = JsonConvert.SerializeObject(payload);
                        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        
                        System.Diagnostics.Debug.WriteLine($"Sending segment-based color command to {endpoint}: {json}");
                        
                        var response = await httpClient.PostAsync(url, content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            System.Diagnostics.Debug.WriteLine($"Successfully sent color '{colorEffect}' (RGB: {r},{g},{b}) to {segmentIds.Count} segments on {endpoint}");
                            return true;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to send color to {endpoint}: {response.StatusCode}");
                        }
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
    }
}