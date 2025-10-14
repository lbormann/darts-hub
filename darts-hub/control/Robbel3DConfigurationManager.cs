using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using darts_hub.model;

namespace darts_hub.control
{
    /// <summary>
    /// Manages Robbel3D configuration presets for one-click WLED and Caller setup
    /// Enhanced with external WLED config and presets file support
    /// </summary>
    public class Robbel3DConfigurationManager
    {
        private static readonly HttpClient httpClient = new HttpClient() 
        { 
            Timeout = TimeSpan.FromSeconds(45) // Increased timeout for large config uploads
        };
        private const string ConfigsDirectory = "configs";
        private const string Robbel3DConfigFile = "robbel3d-configuration.json";
        
        static Robbel3DConfigurationManager()
        {
            // Configure HTTP client for better WLED compatibility
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Darts-Hub-Robbel3D/1.0");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");
        }

        /// <summary>
        /// Gets all available Robbel3D configuration presets
        /// </summary>
        public static List<Robbel3DConfiguration> GetAvailablePresets()
        {
            var presets = new List<Robbel3DConfiguration>();
            
            try
            {
                var basePath = Helper.GetAppBasePath();
                var configPath = Path.Combine(basePath, ConfigsDirectory, Robbel3DConfigFile);
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Looking for config file at: {configPath}");
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] File exists: {File.Exists(configPath)}");
                
                if (!File.Exists(configPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Configuration file not found at {configPath}");
                    
                    // Try alternative locations
                    var alternatePaths = new[]
                    {
                        Path.Combine(basePath, Robbel3DConfigFile),
                        Path.Combine(Directory.GetCurrentDirectory(), ConfigsDirectory, Robbel3DConfigFile),
                        Path.Combine(Directory.GetCurrentDirectory(), Robbel3DConfigFile)
                    };
                    
                    foreach (var altPath in alternatePaths)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Trying alternative path: {altPath}");
                        if (File.Exists(altPath))
                        {
                            configPath = altPath;
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Found config file at alternative location: {configPath}");
                            break;
                        }
                    }
                }
                
                if (!File.Exists(configPath))
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] No configuration file found in any location");
                    CreateDefaultConfigurations(Path.GetDirectoryName(configPath)!);
                    return presets;
                }
                
                var jsonContent = File.ReadAllText(configPath);
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Config file content length: {jsonContent.Length} characters");
                
                var configFile = JsonConvert.DeserializeObject<Robbel3DConfigurationFile>(jsonContent);
                
                if (configFile?.Configurations != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Found {configFile.Configurations.Count} configurations in file");
                    
                    foreach (var config in configFile.Configurations)
                    {
                        // Load external WLED config file if specified
                        if (!string.IsNullOrEmpty(config.WledConfigFile))
                        {
                            var wledConfigPath = Path.Combine(Path.GetDirectoryName(configPath)!, config.WledConfigFile);
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Loading WLED config from: {wledConfigPath}");
                            
                            if (File.Exists(wledConfigPath))
                            {
                                try
                                {
                                    var wledConfigJson = File.ReadAllText(wledConfigPath);
                                    config.WledConfig = JsonConvert.DeserializeObject<WledConfiguration>(wledConfigJson);
                                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Successfully loaded WLED config: {config.WledConfig?.Hardware?.Led?.Total ?? 0} LEDs");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading WLED config: {ex.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED config file not found: {wledConfigPath}");
                            }
                        }
                        
                        // Load external WLED presets file if specified
                        if (!string.IsNullOrEmpty(config.WledPresetsFile))
                        {
                            var wledPresetsPath = Path.Combine(Path.GetDirectoryName(configPath)!, config.WledPresetsFile);
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Loading WLED presets from: {wledPresetsPath}");
                            
                            if (File.Exists(wledPresetsPath))
                            {
                                try
                                {
                                    var wledPresetsJson = File.ReadAllText(wledPresetsPath);
                                    var presetsObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(wledPresetsJson);
                                    
                                    if (presetsObject != null)
                                    {
                                        config.WledPresets = new Dictionary<int, object>();
                                        foreach (var preset in presetsObject)
                                        {
                                            if (int.TryParse(preset.Key, out var presetId))
                                            {
                                                config.WledPresets[presetId] = preset.Value;
                                            }
                                        }
                                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Successfully loaded {config.WledPresets.Count} WLED presets");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading WLED presets: {ex.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED presets file not found: {wledPresetsPath}");
                            }
                        }
                        
                        presets.Add(config);
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Added preset: {config.Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] No configurations found in file or file structure invalid");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading presets: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Stack trace: {ex.StackTrace}");
            }
            
            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Returning {presets.Count} presets total");
            return presets;
        }

        /// <summary>
        /// Loads external WLED configuration and presets files for a configuration
        /// </summary>
        private static void LoadExternalWledFiles(Robbel3DConfiguration config, string configsPath)
        {
            try
            {
                // Load WLED config file if specified
                if (!string.IsNullOrEmpty(config.WledConfigFile))
                {
                    var wledConfigPath = Path.Combine(configsPath, config.WledConfigFile);
                    if (File.Exists(wledConfigPath))
                    {
                        var json = File.ReadAllText(wledConfigPath);
                        config.WledConfig = JsonConvert.DeserializeObject<WledConfiguration>(json);
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Loaded WLED config from: {config.WledConfigFile}");
                        
                        // Update LED count from WLED config if not set
                        if (config.LedCount == 0 && config.WledConfig?.Hardware?.Led?.Total > 0)
                        {
                            config.LedCount = config.WledConfig.Hardware.Led.Total;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED config file not found: {wledConfigPath}");
                    }
                }

                // Load WLED presets file if specified
                if (!string.IsNullOrEmpty(config.WledPresetsFile))
                {
                    var wledPresetsPath = Path.Combine(configsPath, config.WledPresetsFile);
                    if (File.Exists(wledPresetsPath))
                    {
                        var json = File.ReadAllText(wledPresetsPath);
                        var presets = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        
                        if (presets != null)
                        {
                            // Convert string keys to int keys and filter out invalid presets
                            config.WledPresets = new Dictionary<int, object>();
                            foreach (var kvp in presets)
                            {
                                if (int.TryParse(kvp.Key, out var presetId) && presetId > 0)
                                {
                                    config.WledPresets[presetId] = kvp.Value;
                                }
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Loaded {config.WledPresets.Count} WLED presets from: {config.WledPresetsFile}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED presets file not found: {wledPresetsPath}");
                    }
                }

                // Fallback: try to find any WLED files if not specified
                if (config.WledConfig == null || config.WledPresets == null)
                {
                    LoadFallbackWledFiles(config, configsPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading external WLED files: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads any available WLED files as fallback
        /// </summary>
        private static void LoadFallbackWledFiles(Robbel3DConfiguration config, string configsPath)
        {
            try
            {
                // Try to load any WLED config file
                if (config.WledConfig == null)
                {
                    var wledConfigFiles = new[] { "cfg.json" };
                    
                    foreach (var fileName in wledConfigFiles)
                    {
                        var filePath = Path.Combine(configsPath, fileName);
                        if (File.Exists(filePath))
                        {
                            var json = File.ReadAllText(filePath);
                            config.WledConfig = JsonConvert.DeserializeObject<WledConfiguration>(json);
                            
                            if (config.WledConfig != null)
                            {
                                config.WledConfigFile = fileName;
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Fallback: Loaded WLED config from: {fileName}");
                                
                                // Update LED count
                                if (config.LedCount == 0 && config.WledConfig.Hardware?.Led?.Total > 0)
                                {
                                    config.LedCount = config.WledConfig.Hardware.Led.Total;
                                }
                                break;
                            }
                        }
                    }
                }

                // Try to load any WLED presets file
                if (config.WledPresets == null)
                {
                    var wledPresetFiles = new[] { "presets.json" };
                    
                    foreach (var fileName in wledPresetFiles)
                    {
                        var filePath = Path.Combine(configsPath, fileName);
                        if (File.Exists(filePath))
                        {
                            var json = File.ReadAllText(filePath);
                            var presets = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        
                            if (presets != null)
                            {
                                config.WledPresets = new Dictionary<int, object>();
                                foreach (var kvp in presets)
                                {
                                    if (int.TryParse(kvp.Key, out var presetId) && presetId > 0)
                                    {
                                        config.WledPresets[presetId] = kvp.Value;
                                    }
                                }
                                
                                config.WledPresetsFile = fileName;
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Fallback: Loaded {config.WledPresets.Count} WLED presets from: {fileName}");
                                break;
                            }
                        }
                    }
                }

                // Don't create defaults - configuration must be complete in robbel3d-configuration.json
                if (config.WledConfig == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Warning: No WLED config found - configuration must include external WLED config file");
                }

                if (config.WledPresets == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Warning: No WLED presets found - configuration must include external WLED presets file");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error in fallback WLED file loading: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates default Robbel3D configurations in the configs directory
        /// Only creates minimal structure - all parameters must be in robbel3d-configuration.json
        /// </summary>
        private static void CreateDefaultConfigurations(string configsPath)
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(configsPath);
                
                // Only create empty structure - no default parameters
                var configFile = new Robbel3DConfigurationFile
                {
                    Version = "2.0",
                    Created = DateTime.Now,
                    Description = "Robbel3D Configuration File",
                    Configurations = new List<Robbel3DConfiguration>()
                };

                var jsonString = JsonConvert.SerializeObject(configFile, Formatting.Indented);
                var filePath = Path.Combine(configsPath, Robbel3DConfigFile);
                File.WriteAllText(filePath, jsonString);

                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Created empty configuration structure at {filePath}");
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Please configure all parameters in robbel3d-configuration.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error creating configuration structure: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates complete Caller settings with ALL possible arguments
        /// Removed: All settings must be configured in robbel3d-configuration.json
        /// </summary>
        private static Dictionary<string, string> CreateCompleteCallerSettings()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default Caller settings created - configure in robbel3d-configuration.json");
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates standard Caller settings with essential arguments only
        /// Removed: All settings must be configured in robbel3d-configuration.json
        /// </summary>
        private static Dictionary<string, string> CreateStandardCallerSettings()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default Caller settings created - configure in robbel3d-configuration.json");
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates complete WLED settings with ALL possible arguments
        /// Removed: All settings must be configured in robbel3d-configuration.json
        /// </summary>
        private static Dictionary<string, string> CreateCompleteWledSettings()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default WLED settings created - configure in robbel3d-configuration.json");
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates standard WLED settings with essential arguments only
        /// Removed: All settings must be configured in robbel3d-configuration.json
        /// </summary>
        private static Dictionary<string, string> CreateStandardWledSettings()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default WLED settings created - configure in robbel3d-configuration.json");
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates default WLED configuration when no existing config is found
        /// Removed: All configuration must come from robbel3d-configuration.json and external files
        /// </summary>
        private static WledConfiguration? CreateDefaultWledConfig()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default WLED config created - use external cfg.json file");
            return null;
        }

        /// <summary>
        /// Creates default WLED presets when no existing presets are found
        /// Removed: All presets must come from external preset.json file
        /// </summary>
        private static Dictionary<int, object>? CreateDefaultWledPresets()
        {
            System.Diagnostics.Debug.WriteLine("[Robbel3D] No default WLED presets created - use external preset.json file");
            return null;
        }

        /// <summary>
        /// Applies a Robbel3D configuration to a WLED device and updates darts-hub settings
        /// Enhanced to upload both config.json and presets.json to the WLED device from external files
        /// Now supports UI parameter overrides and proper restart timing
        /// </summary>
        public static async Task<bool> ApplyConfiguration(Robbel3DConfiguration config, string wledIpAddress, ProfileManager profileManager, Dictionary<string, string>? uiParameters = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Applying configuration '{config.Name}' to WLED at {wledIpAddress}");
                
                // Step 1: Upload WLED config.json if available
                if (config.WledConfig != null)
                {
                    var configUploadResult = await UploadWledConfig(config.WledConfig, wledIpAddress);
                    if (!configUploadResult)
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] Failed to upload WLED config");
                        return false;
                    }
                    
                    // Wait extra time after config upload to ensure device is fully ready
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Waiting additional time for device to be fully ready after config upload...");
                    await Task.Delay(5000); // Additional 5 seconds after config upload
                }
                
                // Step 2: Upload WLED presets.json if available
                if (config.WledPresets != null && config.WledPresets.Count > 0)
                {
                    // Validate device is responsive before presets upload
                    var isDeviceReady = await ValidateWledDevice(wledIpAddress);
                    if (!isDeviceReady)
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] Device not ready for presets upload, waiting longer...");
                        await Task.Delay(10000); // Wait another 10 seconds
                        
                        isDeviceReady = await ValidateWledDevice(wledIpAddress);
                        if (!isDeviceReady)
                        {
                            System.Diagnostics.Debug.WriteLine("[Robbel3D] Device still not ready, proceeding with caution...");
                        }
                    }
                    
                    var presetsUploadResult = await UploadWledPresetsAsFile(config.WledPresets, wledIpAddress);
                    if (!presetsUploadResult)
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] Failed to upload WLED presets file");
                        // Try individual preset upload as fallback
                        var individualPresetsResult = await UploadWledPresets(config.WledPresets, wledIpAddress);
                        if (!individualPresetsResult)
                        {
                            System.Diagnostics.Debug.WriteLine("[Robbel3D] Failed to upload WLED presets individually");
                            return false;
                        }
                    }
                }
                
                // Step 3: Apply darts-hub settings for Caller and WLED apps with UI parameter overrides
                ApplyDartsHubSettings(config, wledIpAddress, profileManager, uiParameters);
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Successfully applied configuration '{config.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error applying configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uploads WLED configuration file to the device via complete file replacement
        /// Enhanced to preserve network settings from existing config before uploading
        /// </summary>
        private static async Task<bool> UploadWledConfig(WledConfiguration config, string wledIpAddress)
        {
            try
            {
                var endpoint = wledIpAddress.StartsWith("http") ? wledIpAddress : $"http://{wledIpAddress}";
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Starting complete WLED config file replacement to {endpoint}");
                
                // Step 1: Download existing config to preserve network settings
                var existingConfig = await DownloadExistingWledConfig(endpoint);
                
                // Step 2: Merge network settings from existing config into new config
                var mergedConfig = await MergeNetworkSettings(config, existingConfig);
                
                // Step 3: Delete existing config files (try all possible names and extensions)
                await DeleteWledFile(endpoint, "/cfg.json");       // Standard WLED config file
                await DeleteWledFile(endpoint, "/cfg.jso");        // Controller might save as .jso
                
                // Step 4: Upload new config file with preserved network settings
                var configJson = JsonConvert.SerializeObject(mergedConfig, Formatting.Indented);
                var success = await UploadWledConfigFile(endpoint, "cfg.json", configJson);
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED config file uploaded successfully, triggering restart...");
                    
                    // Step 5: Trigger restart to apply config
                    await TriggerWledRestart(endpoint);
                    
                    // Step 6: Wait for restart - longer wait for config changes
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Waiting for WLED restart after config upload...");
                    await Task.Delay(15000); // 15 seconds for config restart (longer than presets)
                    
                    // Step 7: Verify file was saved (check both .json and .jso)
                    var verified = await VerifyWledFile(endpoint, "/cfg.json") || 
                                  await VerifyWledFile(endpoint, "/cfg.jso");
                    if (verified)
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED config file verified successfully after restart");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED config file verification failed");
                        return false;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED config file upload failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error in complete WLED config replacement: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads the existing WLED configuration from the controller
        /// </summary>
        private static async Task<dynamic?> DownloadExistingWledConfig(string endpoint)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Downloading existing WLED config from {endpoint}");
                
                // Try to download existing config file (check both .json and .jso)
                var downloadUrls = new[]
                {
                    $"{endpoint}/edit?download={Uri.EscapeDataString("/cfg.json")}",
                    $"{endpoint}/edit?download={Uri.EscapeDataString("/cfg.jso")}"
                };

                foreach (var url in downloadUrls)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Downloaded existing config (size: {content.Length} chars)");
                            
                            var existingConfig = JsonConvert.DeserializeObject(content);
                            System.Diagnostics.Debug.WriteLine("[Robbel3D] Successfully parsed existing WLED config");
                            return existingConfig;
                        }
                    }
                    catch (Exception urlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error downloading from {url}: {urlEx.Message}");
                        continue;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] No existing config found to download");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error downloading existing WLED config: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Merges network settings from existing config into new config
        /// </summary>
        private static async Task<object> MergeNetworkSettings(WledConfiguration newConfig, dynamic? existingConfig)
        {
            try
            {
                // Convert new config to dynamic object for easier manipulation
                var newConfigJson = JsonConvert.SerializeObject(newConfig);
                dynamic mergedConfig = JsonConvert.DeserializeObject(newConfigJson);
                
                // Extract network settings from existing config if available
                if (existingConfig?.nw != null)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Found network settings in existing config, preserving them");
                    
                    // Log network settings being preserved
                    try
                    {
                        var networkJson = JsonConvert.SerializeObject(existingConfig.nw, Formatting.Indented);
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preserving network settings:\n{networkJson}");
                        
                        // Extract key network info for logging
                        if (existingConfig.nw.ins != null && existingConfig.nw.ins.Count > 0)
                        {
                            var firstNetwork = existingConfig.nw.ins[0];
                            var ssid = firstNetwork?.ssid?.ToString() ?? "Unknown";
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preserving WiFi SSID: {ssid}");
                        }
                    }
                    catch (Exception logEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error logging network settings: {logEx.Message}");
                    }
                    
                    // Preserve the entire network section
                    mergedConfig.nw = existingConfig.nw;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] No network settings found in existing config, using new config as-is");
                }
                
                // Also preserve other critical settings if they exist
                if (existingConfig?.id != null)
                {
                    mergedConfig.id = existingConfig.id;
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preserved device ID from existing config");
                }
                
                if (existingConfig?.nme != null)
                {
                    mergedConfig.nme = existingConfig.nme;
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preserved device name: {existingConfig.nme}");
                }
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Successfully merged network settings into new config");
                return mergedConfig;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error merging network settings: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Falling back to new config without network preservation");
                return newConfig;
            }
        }

        /// <summary>
        /// Deletes a file from WLED device
        /// </summary>
        private static async Task DeleteWledFile(string endpoint, string remotePath)
        {
            try
            {
                var url = $"{endpoint}/edit?file={Uri.EscapeDataString(remotePath)}";
                var response = await httpClient.DeleteAsync(url);
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] DELETE {remotePath}: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error deleting file {remotePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Uploads a configuration file to WLED device
        /// </summary>
        private static async Task<bool> UploadWledConfigFile(string endpoint, string fileName, string jsonContent)
        {
            try
            {
                // Try multiple upload methods for WLED compatibility
                var uploadMethods = new[]
                {
                    async () => await UploadViaEditEndpoint(endpoint, fileName, jsonContent),
                    async () => await UploadViaJsonApi(endpoint, fileName, jsonContent),
                    async () => await UploadViaDirectPost(endpoint, fileName, jsonContent)
                };

                foreach (var uploadMethod in uploadMethods)
                {
                    try
                    {
                        var success = await uploadMethod();
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Successfully uploaded {fileName} via upload method");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Upload method failed for {fileName}: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Robbel3D] All upload methods failed for {fileName}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error uploading file {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload via /edit endpoint (multipart form)
        /// Enhanced with better debugging and chunked upload support
        /// </summary>
        private static async Task<bool> UploadViaEditEndpoint(string endpoint, string fileName, string jsonContent)
        {
            try
            {
                var url = $"{endpoint}/edit";
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Attempting multipart upload to {url} for {fileName} (size: {jsonContent.Length} chars)");
                
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(jsonContent));
                
                // Set proper headers for WLED compatibility
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"data\"",
                    FileName = $"\"{fileName}\""
                };
                
                content.Add(fileContent, "data", fileName);
                
                // Set a longer timeout for large files
                var originalTimeout = httpClient.Timeout;
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                
                try
                {
                    var response = await httpClient.PostAsync(url, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] /edit upload {fileName}: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Response content: {responseContent}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Give WLED time to process the file
                        await Task.Delay(2000);
                        return true;
                    }
                    
                    return false;
                }
                finally
                {
                    httpClient.Timeout = originalTimeout;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] /edit upload failed for {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload via JSON API (if supported)
        /// Enhanced with better error handling and debugging
        /// </summary>
        private static async Task<bool> UploadViaJsonApi(string endpoint, string fileName, string jsonContent)
        {
            try
            {
                var url = $"{endpoint}/json";
                
                // This method is NOT for file uploads - it's for WLED state changes
                // Skip this method for preset file uploads
                if (fileName.Contains("preset"))
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Skipping JSON API for file {fileName} - not suitable for file uploads");
                    return false;
                }
                
                // Try to upload as JSON payload
                using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, content);
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] JSON API upload {fileName}: {response.StatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] JSON API upload failed for {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload via direct POST to file endpoint
        /// Enhanced for better WLED compatibility
        /// </summary>
        private static async Task<bool> UploadViaDirectPost(string endpoint, string fileName, string jsonContent)
        {
            try
            {
                // Try different direct upload approaches
                var uploadUrls = new[]
                {
                    $"{endpoint}/edit?file={Uri.EscapeDataString("/" + fileName)}" // Direct file parameter
                    //$"{endpoint}/upload",                                           // Upload endpoint
                    //$"{endpoint}/fs"                                               // File system endpoint
                };

                foreach (var url in uploadUrls)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Trying direct POST to {url}");
                        
                        using var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(url, content);
                        
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Direct POST {fileName} to {url}: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Success response: {responseContent}");
                            return true;
                        }
                    }
                    catch (Exception urlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Direct POST to {url} failed: {urlEx.Message}");
                        continue;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Direct POST upload failed for {fileName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Triggers WLED restart to apply new configuration
        /// </summary>
        private static async Task TriggerWledRestart(string endpoint)
        {
            try
            {
                // Try different restart methods
                var restartUrls = new[]
                {
                    $"{endpoint}/reset",
                    $"{endpoint}/win&RB=1",        // Restart with RB parameter
                    $"{endpoint}/win?RB=1",        // Alternative format
                    $"{endpoint}/json/state"       // JSON restart command
                };

                foreach (var url in restartUrls)
                {
                    try
                    {
                        if (url.Contains("json/state"))
                        {
                            // Send restart command via JSON
                            var restartCommand = new { rb = true };
                            var json = JsonConvert.SerializeObject(restartCommand);
                            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                            var response = await httpClient.PostAsync(url, content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Restart triggered via JSON command");
                                return;
                            }
                        }
                        else
                        {
                            // Try GET request for restart
                            var response = await httpClient.GetAsync(url);
                            if (response.IsSuccessStatusCode)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Restart triggered via {url}");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Restart attempt failed for {url}: {ex.Message}");
                        continue;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] All restart methods failed, but continuing...");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error triggering WLED restart: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifies that a file exists on WLED device and shows its content
        /// Enhanced to check both .json and .jso extensions
        /// </summary>
        private static async Task<bool> VerifyWledFile(string endpoint, string remotePath)
        {
            try
            {
                // Try both .json and .jso extensions
                var pathsToTry = new[]
                {
                    remotePath,
                    remotePath.Replace(".json", ".jso") // Try .jso version
                };

                foreach (var path in pathsToTry)
                {
                    try
                    {
                        var url = $"{endpoint}/edit?download={Uri.EscapeDataString(path)}";
                        var response = await httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] File {path} verified successfully (size: {content.Length} chars)");
                            
                            // Try to parse and log structure info
                            try
                            {
                                var jsonDoc = JsonConvert.DeserializeObject(content);
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] File {path} contains valid JSON structure");
                            }
                            catch
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] File {path} contains non-JSON content");
                            }
                            
                            return true;
                        }
                    }
                    catch (Exception pathEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error checking {path}: {pathEx.Message}");
                        continue;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] File {remotePath} not found with any extension");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error verifying file {remotePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Uploads WLED presets as a complete presets.json file replacement to the device
        /// Enhanced to handle WLED controller limitations and provide better debugging
        /// </summary>
        private static async Task<bool> UploadWledPresetsAsFile(Dictionary<int, object> presets, string wledIpAddress)
        {
            try
            {
                var endpoint = wledIpAddress.StartsWith("http") ? wledIpAddress : $"http://{wledIpAddress}";
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Starting complete WLED presets file replacement to {endpoint}");
                
                // Step 1: Delete existing presets files (try all possible names and extensions)     
                await DeleteWledFile(endpoint, "/presets.json");     // Standard WLED presets file
                await DeleteWledFile(endpoint, "/presets.jso");      // Controller might save as .jso
                
                // Step 2: Convert presets to proper JSON format and check size
                var presetsForFile = new Dictionary<string, object>();
                foreach (var preset in presets)
                {
                    presetsForFile[preset.Key.ToString()] = preset.Value;
                }
                
                var json = JsonConvert.SerializeObject(presetsForFile, Formatting.None); // Use compact format
                var jsonSize = System.Text.Encoding.UTF8.GetByteCount(json);
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Presets file size: {jsonSize} bytes ({json.Length} chars, {presets.Count} presets)");
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] First 200 characters of JSON: {json.Substring(0, Math.Min(200, json.Length))}...");
                
                // Step 3: Check file size and decide upload strategy
                const int maxUploadSize = 30000; // 30KB safety limit for most WLED controllers
                
                if (jsonSize > maxUploadSize)
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Presets file too large ({jsonSize} bytes > {maxUploadSize} bytes), falling back to individual upload");
                    return await UploadWledPresets(presets, wledIpAddress);
                }
                
                // Step 4: Try ONLY the /edit endpoint for file uploads (skip JSON API)
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Attempting file upload via /edit endpoint only...");
                var success = await UploadViaEditEndpoint(endpoint, "presets.json", json);
                
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Edit endpoint failed, trying direct file write...");
                    success = await UploadViaDirectPost(endpoint, "presets.json", json);
                }
                
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED presets file uploaded successfully, triggering restart...");
                    
                    // Step 5: Trigger restart to apply presets
                    await TriggerWledRestart(endpoint);
                    
                    // Step 6: Wait for restart
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] Waiting for WLED restart after presets upload...");
                    await Task.Delay(8000); // 8 seconds for restart
                    
                    // Step 7: Verify file was saved and show preview (check both .json and .jso)
                    var verified = await VerifyWledFile(endpoint, "/presets.json") || 
                                  await VerifyWledFile(endpoint, "/presets.jso");
                    if (verified)
                    {
                        await ShowPresetsPreview(endpoint, presets);
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED presets file verified successfully after restart");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED presets file verification failed, falling back to individual upload");
                        return await UploadWledPresets(presets, wledIpAddress);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] All file upload methods failed, falling back to individual upload");
                    return await UploadWledPresets(presets, wledIpAddress);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error in complete WLED presets replacement: {ex.Message}");
                return await UploadWledPresets(presets, wledIpAddress);
            }
        }

        /// <summary>
        /// Uploads WLED presets individually (fallback method)
        /// Enhanced to preserve original preset names from wled_presets.json
        /// This is used as fallback when complete file replacement fails
        /// </summary>
        private static async Task<bool> UploadWledPresets(Dictionary<int, object> presets, string wledIpAddress)
        {
            try
            {
                var endpoint = wledIpAddress.StartsWith("http") ? wledIpAddress : $"http://{wledIpAddress}";
                var url = $"{endpoint}/json/state";
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Fallback: Uploading {presets.Count} presets individually to {url}");
                
                int successCount = 0;
                
                foreach (var preset in presets)
                {
                    try
                    {
                        // Extract original preset name from the preset data
                        string presetName = $"Robbel3D Preset {preset.Key}"; // Default fallback name
                        
                        try
                        {
                            // Try to extract the original name from preset data
                            var presetJson = JsonConvert.SerializeObject(preset.Value);
                            dynamic presetData = JsonConvert.DeserializeObject(presetJson);
                            

                            if (presetData?.n != null)
                            {
                                presetName = presetData.n.ToString();
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preserving original preset name: '{presetName}' for preset {preset.Key}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] No original name found for preset {preset.Key}, using default name");
                            }
                        }
                        catch (Exception nameEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error extracting preset name for {preset.Key}: {nameEx.Message}");
                        }
                        
                        // First, apply the preset data to current state
                        var presetStateJson = JsonConvert.SerializeObject(preset.Value);
                        using var presetContent = new StringContent(presetStateJson, System.Text.Encoding.UTF8, "application/json");
                        
                        var applyResponse = await httpClient.PostAsync(url, presetContent);
                        if (applyResponse.IsSuccessStatusCode)
                        {
                            await Task.Delay(200);
                            
                            // Then save the current state as a preset with original name
                            var savePresetPayload = new 
                            {
                                psave = preset.Key,
                                n = presetName // Use original or fallback name
                            };
                            
                            var saveJson = JsonConvert.SerializeObject(savePresetPayload);
                            using var saveContent = new StringContent(saveJson, System.Text.Encoding.UTF8, "application/json");
                            
                            var saveResponse = await httpClient.PostAsync(url, saveContent);
                            if (saveResponse.IsSuccessStatusCode)
                            {
                                successCount++;
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Preset {preset.Key} ('{presetName}') uploaded successfully");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Failed to save preset {preset.Key}: {saveResponse.StatusCode}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Failed to apply preset {preset.Key}: {applyResponse.StatusCode}");
                        }
                        
                        // Small delay between preset uploads to prevent overwhelming the device
                        await Task.Delay(300);
                    }
                    catch (Exception presetEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error uploading preset {preset.Key}: {presetEx.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Individual preset upload completed: {successCount}/{presets.Count} presets successful");
                
                // Consider it successful if we uploaded at least 50% of presets
                return successCount >= (presets.Count / 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error uploading WLED presets individually: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows a preview of uploaded presets by checking their names
        /// </summary>
        private static async Task ShowPresetsPreview(string endpoint, Dictionary<int, object> originalPresets)
        {
            try
            {
                // Try to read back the uploaded presets to show what was actually saved
                // Check both .json and .jso extensions
                var urls = new[]
                {
                    $"{endpoint}/edit?download={Uri.EscapeDataString("/presets.json")}",
                    $"{endpoint}/edit?download={Uri.EscapeDataString("/presets.jso")}"
                };

                foreach (var url in urls)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var uploadedPresets = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                            
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Uploaded presets verification from {url}:");
                            
                            foreach (var preset in originalPresets.Take(3)) // Show first 3 presets
                            {
                                var presetKey = preset.Key.ToString();
                                if (uploadedPresets?.ContainsKey(presetKey) == true)
                                {
                                    try
                                    {
                                        var presetJson = JsonConvert.SerializeObject(preset.Value);
                                        dynamic presetData = JsonConvert.DeserializeObject(presetJson);
                                        var presetName = presetData?.n?.ToString() ?? $"Preset {preset.Key}";
                                        System.Diagnostics.Debug.WriteLine($"[Robbel3D]   ✓ Preset {preset.Key}: '{presetName}' uploaded successfully");
                                    }
                                    catch
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[Robbel3D]   ✓ Preset {preset.Key}: uploaded successfully");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[Robbel3D]   ✗ Preset {preset.Key}: missing after upload");
                                }
                            }
                            
                            if (originalPresets.Count > 3)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D]   ... and {originalPresets.Count - 3} more presets");
                            }
                            
                            return; // Success, don't try other URLs
                        }
                    }
                    catch (Exception urlEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error checking {url}: {urlEx.Message}");
                        continue;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Could not verify presets from any URL");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error showing presets preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the complete darts-hub settings for Caller and WLED applications
        /// Enhanced to handle all arguments and UI parameter overrides
        /// </summary>
        private static void ApplyDartsHubSettings(Robbel3DConfiguration config, string wledIpAddress, ProfileManager profileManager, Dictionary<string, string>? uiParameters = null)
        {
            try
            {
                var profiles = profileManager.GetProfiles();
                if (profiles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] No profiles found to apply settings to");
                    return;
                }
                
                // Apply to the first profile (or current selected profile)
                var targetProfile = profiles.First();
                
                // Update WLED settings - apply ALL arguments
                var wledApp = targetProfile.Apps.Values.FirstOrDefault(profileState => 
                    profileState.App?.CustomName?.Contains("wled", StringComparison.OrdinalIgnoreCase) == true ||
                    profileState.App?.CustomName?.Contains("WLED", StringComparison.OrdinalIgnoreCase) == true)?.App;
                
                if (wledApp?.Configuration?.Arguments != null)
                {
                    // Update WLED settings with discovered IP
                    var updatedWledSettings = new Dictionary<string, string>(config.WledSettings);
                    updatedWledSettings["WEPS"] = wledIpAddress;
                    
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Applying {updatedWledSettings.Count} WLED settings:");
                    
                    foreach (var setting in updatedWledSettings)
                    {
                        var arg = wledApp.Configuration.Arguments.FirstOrDefault(a => 
                            a.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase));
                        if (arg != null)
                        {
                            arg.Value = setting.Value;
                            arg.IsValueChanged = true;
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Updated WLED setting {setting.Key} = {setting.Value}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED argument not found: {setting.Key}");
                        }
                    }
                }
                
                // Update Caller settings - apply ALL arguments with UI parameter overrides
                var callerApp = targetProfile.Apps.Values.FirstOrDefault(profileState => 
                    profileState.App?.CustomName?.Contains("caller", StringComparison.OrdinalIgnoreCase) == true ||
                    profileState.App?.CustomName?.Contains("Caller", StringComparison.OrdinalIgnoreCase) == true)?.App;
                
                if (callerApp?.Configuration?.Arguments != null)
                {
                    // Merge config settings with UI parameter overrides
                    var mergedCallerSettings = new Dictionary<string, string>(config.CallerSettings);
                    
                    // UI parameters override config settings
                    if (uiParameters != null)
                    {
                        foreach (var uiParam in uiParameters)
                        {
                            mergedCallerSettings[uiParam.Key] = uiParam.Value;
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI override: {uiParam.Key} = {uiParam.Value}");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Applying {mergedCallerSettings.Count} Caller settings:");
                    
                    foreach (var setting in mergedCallerSettings)
                    {
                        var arg = callerApp.Configuration.Arguments.FirstOrDefault(a => 
                            a.Name.Equals(setting.Key, StringComparison.OrdinalIgnoreCase));
                        if (arg != null)
                        {
                            // Apply setting if it has a meaningful value (not empty placeholders)
                            if (!string.IsNullOrEmpty(setting.Value))
                            {
                                arg.Value = setting.Value;
                                arg.IsValueChanged = true;
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Updated Caller setting {setting.Key} = {setting.Value}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Caller argument not found: {setting.Key}");
                        }
                    }
                }
                
                // Save the updated configuration
                profileManager.StoreApps();
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] All darts-hub settings applied and saved");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error applying darts-hub settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that a WLED device is reachable and compatible
        /// Enhanced with retry logic for post-restart validation
        /// </summary>
        public static async Task<bool> ValidateWledDevice(string wledIpAddress)
        {
            try
            {
                var endpoint = wledIpAddress.StartsWith("http") ? wledIpAddress : $"http://{wledIpAddress}";
                
                // Try multiple endpoints to validate WLED device
                var endpoints = new[]
                {
                    $"{endpoint}/json/info",
                    $"{endpoint}/json/state", 
                    $"{endpoint}/json"
                };

                // Try up to 3 times with increasing delays for post-restart scenarios
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    foreach (var testUrl in endpoints)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Validating WLED device at {testUrl} (attempt {attempt})");
                            
                            var response = await httpClient.GetAsync(testUrl);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                
                                // Check if the response looks like WLED
                                if (content.Contains("WLED") || content.Contains("wled") || 
                                    content.Contains("\"bri\":") || content.Contains("\"on\":"))
                                {
                                    try
                                    {
                                        dynamic info = JsonConvert.DeserializeObject(content);
                                        
                                        // Extract device information if available
                                        var brand = info?.brand?.ToString();
                                        var product = info?.product?.ToString();
                                        var version = info?.ver?.ToString();
                                        var name = info?.name?.ToString();
                                        
                                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED device validated: {brand} {product} v{version} ({name}) on attempt {attempt}");
                                        return true;
                                    }
                                    catch
                                    {
                                        // Even if JSON parsing fails, if it contains WLED indicators, it's likely valid
                                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] WLED device validated (JSON parsing failed but WLED indicators found) on attempt {attempt}");
                                        return true;
                                    }
                                }
                            }
                        }
                        catch (Exception endpointEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Endpoint {testUrl} failed on attempt {attempt}: {endpointEx.Message}");
                            continue;
                        }
                    }
                    
                    // Wait before next attempt (increasing delay)
                    if (attempt < 3)
                    {
                        var delay = attempt * 2000; // 2s, 4s
                        System.Diagnostics.Debug.WriteLine($"[Robbel3D] Device validation failed on attempt {attempt}, waiting {delay}ms before retry...");
                        await Task.Delay(delay);
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Device validation failed: No valid WLED endpoints found after 3 attempts");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error validating WLED device: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets WLED device to factory defaults (optional safety feature)
        /// </summary>
        public static async Task<bool> ResetWledDevice(string wledIpAddress)
        {
            try
            {
                var endpoint = wledIpAddress.StartsWith("http") ? wledIpAddress : $"http://{wledIpAddress}";
                
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Resetting WLED device at {endpoint}");
                
                // Step 1: Delete config and presets files (all possible names and extensions)
                await DeleteWledFile(endpoint, "/cfg.json");         // Standard WLED config
                await DeleteWledFile(endpoint, "/cfg.jso");          // Controller format
                await DeleteWledFile(endpoint, "/presets.json");     // Standard WLED presets
                await DeleteWledFile(endpoint, "/presets.jso");      // Controller format
                
                // Step 2: Trigger restart
                await TriggerWledRestart(endpoint);
                
                System.Diagnostics.Debug.WriteLine("[Robbel3D] WLED device reset completed");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error resetting WLED device: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates that required caller parameters are configured before applying configuration
        /// Enhanced to check UI input fields first, then app configuration
        /// </summary>
        public static (bool IsValid, List<string> MissingParameters) ValidateRequiredCallerParameters(Robbel3DConfiguration config, ProfileManager profileManager, Dictionary<string, string>? uiParameters = null)
        {
            var missingParameters = new List<string>();
            
            try
            {
                // Check required parameters: U (Email), P (Password), B (Board ID), M (Media Path)
                var requiredParams = new Dictionary<string, string>
                {
                    ["U"] = "Autodarts Email",
                    ["P"] = "Autodarts Password", 
                    ["B"] = "Autodarts Board ID",
                    ["M"] = "Media Path"
                };
                
                foreach (var requiredParam in requiredParams)
                {
                    var paramKey = requiredParam.Key;
                    var paramName = requiredParam.Value;
                    
                    var hasValue = false;
                    
                    // Check UI parameters first (highest priority)
                    if (uiParameters != null && uiParameters.TryGetValue(paramKey, out var uiValue) && !string.IsNullOrWhiteSpace(uiValue))
                    {
                        hasValue = true;
                    }
                    else
                    {
                        // Check in configuration settings
                        var configValue = config.CallerSettings.GetValueOrDefault(paramKey, "");
                        if (!string.IsNullOrWhiteSpace(configValue))
                        {
                            hasValue = true;
                        }
                        else
                        {
                            // Check in actual app arguments as fallback
                            var profiles = profileManager.GetProfiles();
                            if (profiles.Count > 0)
                            {
                                var targetProfile = profiles.First();
                                var callerApp = targetProfile.Apps.Values.FirstOrDefault(profileState => 
                                    profileState.App?.CustomName?.Contains("caller", StringComparison.OrdinalIgnoreCase) == true ||
                                    profileState.App?.CustomName?.Contains("Caller", StringComparison.OrdinalIgnoreCase) == true)?.App;
                                
                                if (callerApp?.Configuration?.Arguments != null)
                                {
                                    var appArg = callerApp.Configuration.Arguments.FirstOrDefault(a => 
                                        a.Name.Equals(paramKey, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (appArg != null && !string.IsNullOrWhiteSpace(appArg.Value))
                                    {
                                        hasValue = true;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (!hasValue)
                    {
                        missingParameters.Add($"{paramName} ({paramKey})");
                    }
                }
                
                bool isValid = missingParameters.Count == 0;
                
                if (isValid)
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] All required Caller parameters are configured");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Missing required Caller parameters: {string.Join(", ", missingParameters)}");
                }
                
                return (isValid, missingParameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error validating required Caller parameters: {ex.Message}");
                missingParameters.Add($"Validation error: {ex.Message}");
                return (false, missingParameters);
            }
        }

        /// <summary>
        /// Ensures configuration files are copied to build directory during build process
        /// </summary>
        public static void EnsureConfigurationFilesInBuildDirectory()
        {
            try
            {
                var buildDirectory = Directory.GetCurrentDirectory();
                var sourceConfigsPath = Path.Combine(buildDirectory, "..", "..", "..", ConfigsDirectory);
                var buildConfigsPath = Path.Combine(buildDirectory, ConfigsDirectory);
                
                // If we're in a different build context, find the source configs directory
                var possibleSourcePaths = new[]
                {
                    Path.Combine(buildDirectory, "..", "..", "..", ConfigsDirectory), // Debug build context
                    Path.Combine(buildDirectory, "..", ConfigsDirectory),             // Release build context
                    sourceConfigsPath                                                // Already in correct location
                };
                
                string? actualSourcePath = null;
                foreach (var path in possibleSourcePaths)
                {
                    if (Directory.Exists(path))
                    {
                        actualSourcePath = Path.GetFullPath(path);
                        break;
                    }
                }
                
                if (actualSourcePath != null && Directory.Exists(actualSourcePath))
                {
                    // Ensure build configs directory exists
                    Directory.CreateDirectory(buildConfigsPath);
                    
                    // Copy configuration files to build directory (with correct WLED file names)
                    var configFilesToCopy = new[]
                    {
                        Robbel3DConfigFile,
                        "cfg.json",            // Standard WLED config file
                        "presets.json",        // Standard WLED presets file
                    };
                    
                    foreach (var fileName in configFilesToCopy)
                    {
                        var sourcePath = Path.Combine(actualSourcePath, fileName);
                        var destPath = Path.Combine(buildConfigsPath, fileName);
                        
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destPath, overwrite: true);
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Copied {fileName} to build directory");
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Configuration files copied from {actualSourcePath} to build directory");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] Source configs directory not found, creating empty configuration structure");
                    // If no source configs found, create empty configuration structure in build directory
                    CreateDefaultConfigurations(buildConfigsPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error ensuring configuration files in build directory: {ex.Message}");
            }
        }
    }
}