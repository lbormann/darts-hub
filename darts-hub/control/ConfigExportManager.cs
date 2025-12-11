using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using darts_hub.model;
using Newtonsoft.Json;

namespace darts_hub.control
{
    /// <summary>
    /// Manages export and import of application configurations from apps-downloadable.json
    /// </summary>
    public class ConfigExportManager
    {
        private static readonly string AppsConfigPath = Path.Combine(Helper.GetAppBasePath(), "apps-downloadable.json");
        private static readonly string ExportsDirectory = Path.Combine(Helper.GetAppBasePath(), "exports");
        private static readonly string BackupsDirectory = Path.Combine(Helper.GetAppBasePath(), "backups", "config-backups");
        
        // Use Newtonsoft.Json for compatibility with existing serialization
        private static readonly JsonSerializerSettings NewtonsoftSettings = new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Creates a backup of the apps-downloadable.json before any modification
        /// </summary>
        private static async Task<string> CreateBackup(string operation)
        {
            try
            {
                Directory.CreateDirectory(BackupsDirectory);
                
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"apps-config_{operation}_{timestamp}.json";
                var backupPath = Path.Combine(BackupsDirectory, backupFileName);
                
                if (File.Exists(AppsConfigPath))
                {
                    File.Copy(AppsConfigPath, backupPath, true);
                    return backupPath;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create backup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads the current apps-downloadable.json configuration
        /// </summary>
        private static async Task<List<AppDownloadable>> LoadAppsConfig()
        {
            try
            {
                if (!File.Exists(AppsConfigPath))
                {
                    throw new FileNotFoundException("apps-downloadable.json not found");
                }
                
                var json = await File.ReadAllTextAsync(AppsConfigPath);
                var apps = JsonConvert.DeserializeObject<List<AppDownloadable>>(json, NewtonsoftSettings);
                
                return apps ?? new List<AppDownloadable>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load apps configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves the apps-downloadable.json configuration
        /// </summary>
        private static async Task SaveAppsConfig(List<AppDownloadable> apps)
        {
            try
            {
                var json = JsonConvert.SerializeObject(apps, NewtonsoftSettings);
                await File.WriteAllTextAsync(AppsConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save apps configuration: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Filters out sensitive credentials (U, P, B) from darts-caller configuration
        /// Returns a new list with filtered data
        /// </summary>
        private static List<AppDownloadable> FilterCredentialsFromCaller(List<AppDownloadable> apps)
        {
            // Serialize to JSON for manipulation
            var appsJson = JsonConvert.SerializeObject(apps, NewtonsoftSettings);
            var appsDict = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(appsJson);
            
            // Find darts-caller
            var callerAppDict = appsDict.FirstOrDefault(a =>
            {
                var nameKey = a.Keys.FirstOrDefault(k => k.Equals("Name", StringComparison.OrdinalIgnoreCase));
                return nameKey != null && string.Equals(a[nameKey]?.ToString(), "darts-caller", StringComparison.OrdinalIgnoreCase);
            });
            
            if (callerAppDict != null)
            {
                // Get configuration
                var configKey = callerAppDict.Keys.FirstOrDefault(k => k.Equals("Configuration", StringComparison.OrdinalIgnoreCase));
                if (configKey != null && callerAppDict[configKey] != null)
                {
                    var configJson = JsonConvert.SerializeObject(callerAppDict[configKey]);
                    var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
                    
                    // Get arguments
                    var argsKey = configDict.Keys.FirstOrDefault(k => k.Equals("Arguments", StringComparison.OrdinalIgnoreCase));
                    if (argsKey != null && configDict[argsKey] != null)
                    {
                        var argsJson = JsonConvert.SerializeObject(configDict[argsKey]);
                        var argsList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(argsJson);
                        
                        // Filter out U, P, B
                        var filteredArgs = argsList.Where(arg =>
                        {
                            var nameKey = arg.Keys.FirstOrDefault(k => k.Equals("Name", StringComparison.OrdinalIgnoreCase));
                            if (nameKey == null) return true;
                            
                            var argName = arg[nameKey]?.ToString();
                            return !string.Equals(argName, "U", StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(argName, "P", StringComparison.OrdinalIgnoreCase) &&
                                   !string.Equals(argName, "B", StringComparison.OrdinalIgnoreCase);
                        }).ToList();
                        
                        // Update arguments
                        configDict[argsKey] = filteredArgs;
                        callerAppDict[configKey] = configDict;
                    }
                }
            }
            
            // Convert back to AppDownloadable list
            var filteredJson = JsonConvert.SerializeObject(appsDict, NewtonsoftSettings);
            return JsonConvert.DeserializeObject<List<AppDownloadable>>(filteredJson);
        }

        /// <summary>
        /// Converts AppDownloadable list to ParameterData format (NameHuman + Value only)
        /// Only includes parameters that have non-empty values
        /// </summary>
        private static Dictionary<string, List<ExportParameter>> ConvertToParameterData(
            List<AppDownloadable> apps, 
            bool excludeCredentials = false)
        {
            var parameterData = new Dictionary<string, List<ExportParameter>>();
            
            foreach (var app in apps)
            {
                if (app.Configuration?.Arguments == null || !app.Configuration.Arguments.Any())
                {
                    continue; // Skip apps without arguments
                }
                
                var exportParams = new List<ExportParameter>();
                
                foreach (var arg in app.Configuration.Arguments)
                {
                    // Skip credentials if requested
                    if (excludeCredentials && 
                        string.Equals(app.Name, "darts-caller", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(arg.Name, "U", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(arg.Name, "P", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(arg.Name, "B", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip this credential
                        }
                    }
                    
                    // Only export parameters with non-empty values
                    if (!string.IsNullOrWhiteSpace(arg.Value))
                    {
                        exportParams.Add(new ExportParameter
                        {
                            Name = arg.Name,
                            NameHuman = arg.NameHuman,
                            Value = arg.Value
                        });
                    }
                }
                
                // Only add extension if it has parameters with values
                if (exportParams.Any())
                {
                    parameterData[app.Name] = exportParams;
                }
            }
            
            return parameterData;
        }

        #region Export Methods

        /// <summary>
        /// Exports the complete configuration (all extensions)
        /// Only exports NameHuman and Value for each parameter (like parameter export)
        /// </summary>
        /// <param name="customName">Optional custom name for the export file</param>
        /// <param name="description">Optional description</param>
        /// <param name="excludeCredentials">If true, excludes U, P, B parameters from darts-caller</param>
        public static async Task<string> ExportFull(string customName = null, string description = null, bool excludeCredentials = false)
        {
            try
            {
                // Create backup before export
                await CreateBackup("export_full");
                
                // Load all apps
                var apps = await LoadAppsConfig();
                
                // Convert to parameter data format (NameHuman + Value only)
                var parameterData = ConvertToParameterData(apps, excludeCredentials);
                
                // Create metadata with parameter data
                var metadata = new ExportMetadata
                {
                    Type = ExportType.Full,
                    Timestamp = DateTime.Now,
                    AppVersion = Updater.version,
                    Description = description ?? "Full configuration export (NameHuman + Value only)",
                    ExtensionNames = parameterData.Keys.ToList(),
                    ParameterData = parameterData,
                    Data = new List<AppDownloadable>() // Empty - we use ParameterData instead
                };
                
                // Generate filename
                var fileName = GenerateExportFileName(ExportType.Full, customName);
                var exportPath = await SaveExport(metadata, fileName);
                
                return exportPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Full export failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports specific extensions by name
        /// Only exports NameHuman and Value for each parameter (like parameter export)
        /// </summary>
        /// <param name="extensionNames">List of extension names to export</param>
        /// <param name="customName">Optional custom name for the export file</param>
        /// <param name="description">Optional description</param>
        /// <param name="excludeCredentials">If true, excludes U, P, B parameters from darts-caller</param>
        public static async Task<string> ExportExtensions(
            List<string> extensionNames, 
            string customName = null, 
            string description = null,
            bool excludeCredentials = false)
        {
            try
            {
                if (extensionNames == null || !extensionNames.Any())
                {
                    throw new ArgumentException("At least one extension name must be provided");
                }
                
                // Create backup before export
                await CreateBackup("export_extensions");
                
                // Load all apps
                var apps = await LoadAppsConfig();
                
                // Filter to only requested extensions (case-insensitive)
                var exportApps = new List<AppDownloadable>();
                foreach (var requestedName in extensionNames)
                {
                    var app = apps.FirstOrDefault(a => 
                        string.Equals(a.Name, requestedName, StringComparison.OrdinalIgnoreCase));
                    
                    if (app == null)
                    {
                        throw new Exception($"Extension not found: {requestedName}");
                    }
                    
                    exportApps.Add(app);
                }
                
                // Convert to parameter data format (NameHuman + Value only)
                var parameterData = ConvertToParameterData(exportApps, excludeCredentials);
                
                // Create metadata with parameter data
                var metadata = new ExportMetadata
                {
                    Type = ExportType.Extensions,
                    Timestamp = DateTime.Now,
                    AppVersion = Updater.version,
                    Description = description ?? $"Export of {exportApps.Count} extension(s) (NameHuman + Value only)",
                    ExtensionNames = parameterData.Keys.ToList(),
                    ParameterData = parameterData,
                    Data = new List<AppDownloadable>() // Empty - we use ParameterData instead
                };
                
                // Generate filename
                var fileName = GenerateExportFileName(ExportType.Extensions, customName, extensionNames);
                var exportPath = await SaveExport(metadata, fileName);
                
                return exportPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Extension export failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exports specific parameters from specific extensions
        /// Only exports NameHuman and Value, and only if Value is not null/empty
        /// </summary>
        public static async Task<string> ExportParameters(
            Dictionary<string, List<string>> extensionParameters, 
            string customName = null, 
            string description = null)
        {
            try
            {
                if (extensionParameters == null || !extensionParameters.Any())
                {
                    throw new ArgumentException("At least one extension with parameters must be provided");
                }
                
                // Create backup before export
                await CreateBackup("export_parameters");
                
                // Load all apps properly as AppDownloadable objects
                var apps = await LoadAppsConfig();
                
                var parameterData = new Dictionary<string, List<ExportParameter>>();
                var exportedExtensionNames = new List<string>();
                
                // Process each extension
                foreach (var kvp in extensionParameters)
                {
                    var extensionName = kvp.Key;
                    var parameterNames = kvp.Value;
                    
                    // Find the extension (case-insensitive)
                    var app = apps.FirstOrDefault(a => 
                        string.Equals(a.Name, extensionName, StringComparison.OrdinalIgnoreCase));
                    
                    if (app == null)
                    {
                        throw new Exception($"Extension not found: {extensionName}");
                    }
                    
                    if (app.Configuration?.Arguments == null || !app.Configuration.Arguments.Any())
                    {
                        throw new Exception($"Extension {extensionName} has no parameters");
                    }
                    
                    // Filter arguments by parameter names (case-insensitive)
                    // Only export parameters that have a Value (not null/empty)
                    var exportParams = new List<ExportParameter>();
                    
                    foreach (var paramName in parameterNames)
                    {
                        var arg = app.Configuration.Arguments.FirstOrDefault(a =>
                            string.Equals(a.Name, paramName, StringComparison.OrdinalIgnoreCase));
                        
                        if (arg != null)
                        {
                            // Only export if Value is not null or empty
                            if (!string.IsNullOrWhiteSpace(arg.Value))
                            {
                                exportParams.Add(new ExportParameter
                                {
                                    Name = arg.Name,
                                    NameHuman = arg.NameHuman,
                                    Value = arg.Value
                                });
                            }
                        }
                    }
                    
                    if (!exportParams.Any())
                    {
                        throw new Exception($"No parameters with values found in {extensionName} matching: {string.Join(", ", parameterNames)}");
                    }
                    
                    parameterData[app.Name] = exportParams;
                    exportedExtensionNames.Add(app.Name);
                }
                
                // Create metadata with simplified parameter data
                var metadata = new ExportMetadata
                {
                    Type = ExportType.Parameters,
                    Timestamp = DateTime.Now,
                    AppVersion = Updater.version,
                    Description = description ?? "Export of specific parameters (NameHuman + Value only)",
                    ExtensionNames = exportedExtensionNames,
                    ParameterNames = extensionParameters,
                    ParameterData = parameterData,
                    Data = new List<AppDownloadable>() // Empty for parameter exports
                };
                
                // Generate filename
                var fileName = GenerateExportFileName(ExportType.Parameters, customName);
                var exportPath = await SaveExport(metadata, fileName);
                
                return exportPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Parameter export failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Import Methods

        /// <summary>
        /// Imports a previously exported configuration
        /// </summary>
        public static async Task<ImportResult> Import(string exportFilePath, ImportMode mode = ImportMode.Merge, bool createBackup = true)
        {
            try
            {
                if (!File.Exists(exportFilePath))
                {
                    throw new FileNotFoundException($"Export file not found: {exportFilePath}");
                }
                
                // Create backup before import
                string backupPath = null;
                if (createBackup)
                {
                    backupPath = await CreateBackup("import");
                }
                
                // Load export metadata
                var exportJson = await File.ReadAllTextAsync(exportFilePath);
                var metadata = JsonConvert.DeserializeObject<ExportMetadata>(exportJson);
                
                if (metadata == null || metadata.Data == null)
                {
                    throw new Exception("Invalid export file format");
                }
                
                // Load current configuration as JSON for manipulation
                var currentJson = await File.ReadAllTextAsync(AppsConfigPath);
                var currentAppsJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(currentJson);
                
                // Perform import based on type and mode
                var result = PerformImport(currentAppsJson, metadata, mode);
                
                // Save updated configuration
                var updatedJson = JsonConvert.SerializeObject(currentAppsJson, NewtonsoftSettings);
                await File.WriteAllTextAsync(AppsConfigPath, updatedJson);
                
                result.BackupPath = backupPath;
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Import failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Performs the actual import logic using JSON manipulation
        /// Now all export types use ParameterData format (NameHuman + Value only)
        /// </summary>
        private static ImportResult PerformImport(
            List<Dictionary<string, object>> currentAppsJson, 
            ExportMetadata metadata, 
            ImportMode mode)
        {
            var result = new ImportResult
            {
                ExportType = metadata.Type,
                ImportMode = mode,
                Timestamp = DateTime.Now
            };
            
            // Check if this is a new-format export (uses ParameterData)
            bool usesParameterData = metadata.ParameterData != null && metadata.ParameterData.Any();
            
            // All new exports use ParameterData format
            if (usesParameterData)
            {
                result = ImportParameters(currentAppsJson, metadata, mode);
            }
            else
            {
                // Legacy support for old exports that used Data field
                switch (metadata.Type)
                {
                    case ExportType.Full:
                        var fullImportJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(metadata.Data));
                        result = ImportFullLegacy(currentAppsJson, fullImportJson, mode);
                        break;
                        
                    case ExportType.Extensions:
                        var extImportJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(metadata.Data));
                        result = ImportExtensionsLegacy(currentAppsJson, extImportJson, mode);
                        break;
                        
                    case ExportType.Parameters:
                        result = ImportParameters(currentAppsJson, metadata, mode);
                        break;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Imports full configuration (LEGACY - for old export format)
        /// </summary>
        private static ImportResult ImportFullLegacy(
            List<Dictionary<string, object>> currentAppsJson, 
            List<Dictionary<string, object>> importAppsJson, 
            ImportMode mode)
        {
            var result = new ImportResult
            {
                ExportType = ExportType.Full,
                ImportMode = mode,
                Timestamp = DateTime.Now
            };
            
            if (mode == ImportMode.Replace)
            {
                // Replace everything
                currentAppsJson.Clear();
                currentAppsJson.AddRange(importAppsJson);
                result.AddedExtensions = importAppsJson.Count;
                result.Message = $"Replaced all configurations with {importAppsJson.Count} extensions";
            }
            else // Merge
            {
                foreach (var importApp in importAppsJson)
                {
                    var importName = importApp.ContainsKey("name") ? importApp["name"].ToString() : null;
                    if (string.IsNullOrEmpty(importName)) continue;
                    
                    var existingApp = currentAppsJson.FirstOrDefault(a => 
                        a.ContainsKey("name") && 
                        string.Equals(a["name"].ToString(), importName, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingApp != null)
                    {
                        // Update existing
                        var index = currentAppsJson.IndexOf(existingApp);
                        currentAppsJson[index] = importApp;
                        result.UpdatedExtensions++;
                    }
                    else
                    {
                        // Add new
                        currentAppsJson.Add(importApp);
                        result.AddedExtensions++;
                    }
                }
                
                result.Message = $"Merged configuration: {result.AddedExtensions} added, {result.UpdatedExtensions} updated";
            }
            
            return result;
        }

        /// <summary>
        /// Imports specific extensions
        /// </summary>
        private static ImportResult ImportExtensionsLegacy(
            List<Dictionary<string, object>> currentAppsJson, 
            List<Dictionary<string, object>> importAppsJson, 
            ImportMode mode)
        {
            var result = new ImportResult
            {
                ExportType = ExportType.Extensions,
                ImportMode = mode,
                Timestamp = DateTime.Now
            };
            
            foreach (var importApp in importAppsJson)
            {
                var importName = importApp.ContainsKey("name") ? importApp["name"].ToString() : null;
                if (string.IsNullOrEmpty(importName)) continue;
                
                var existingApp = currentAppsJson.FirstOrDefault(a => 
                    a.ContainsKey("name") && 
                    string.Equals(a["name"].ToString(), importName, StringComparison.OrdinalIgnoreCase));
                
                if (existingApp != null)
                {
                    if (mode == ImportMode.Replace)
                    {
                        // Replace entire extension
                        var index = currentAppsJson.IndexOf(existingApp);
                        currentAppsJson[index] = importApp;
                        result.UpdatedExtensions++;
                    }
                    else // Merge
                    {
                        // Merge configurations
                        MergeExtensionJson(existingApp, importApp);
                        result.UpdatedExtensions++;
                    }
                }
                else
                {
                    // Add new extension
                    currentAppsJson.Add(importApp);
                    result.AddedExtensions++;
                }
            }
            
            result.Message = $"Imported extensions: {result.AddedExtensions} added, {result.UpdatedExtensions} updated";
            return result;
        }

        /// <summary>
        /// Imports specific parameters
        /// Only updates parameters where the Value differs from the current configuration
        /// Matches parameters by NameHuman
        /// </summary>
        private static ImportResult ImportParameters(
            List<Dictionary<string, object>> currentAppsJson, 
            ExportMetadata metadata,
            ImportMode mode)
        {
            var result = new ImportResult
            {
                ExportType = ExportType.Parameters,
                ImportMode = mode,
                Timestamp = DateTime.Now
            };
            
            // Check if we have the new ParameterData format
            if (metadata.ParameterData == null || !metadata.ParameterData.Any())
            {
                result.Errors.Add("Export file does not contain parameter data");
                return result;
            }
            
            foreach (var kvp in metadata.ParameterData)
            {
                var extensionName = kvp.Key;
                var importParams = kvp.Value;
                
                // Find the extension in current config
                var existingAppJson = currentAppsJson.FirstOrDefault(a =>
                {
                    var nameKey = a.Keys.FirstOrDefault(k => k.Equals("Name", StringComparison.OrdinalIgnoreCase));
                    return nameKey != null && string.Equals(a[nameKey]?.ToString(), extensionName, StringComparison.OrdinalIgnoreCase);
                });
                
                if (existingAppJson == null)
                {
                    result.Warnings.Add($"Extension '{extensionName}' not found, skipping parameter import");
                    continue;
                }
                
                // Get configuration
                var configKey = existingAppJson.Keys.FirstOrDefault(k => k.Equals("Configuration", StringComparison.OrdinalIgnoreCase));
                if (configKey == null || existingAppJson[configKey] == null)
                {
                    result.Warnings.Add($"Extension '{extensionName}' has no configuration, skipping");
                    continue;
                }
                
                var configJson = JsonConvert.SerializeObject(existingAppJson[configKey]);
                var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
                
                var argsKey = configDict.Keys.FirstOrDefault(k => k.Equals("Arguments", StringComparison.OrdinalIgnoreCase));
                if (argsKey == null || configDict[argsKey] == null)
                {
                    result.Warnings.Add($"Extension '{extensionName}' has no arguments, skipping");
                    continue;
                }
                
                var argsJson = JsonConvert.SerializeObject(configDict[argsKey]);
                var existingArgs = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(argsJson);
                
                int updatedInExtension = 0;
                
                // Process each import parameter
                foreach (var importParam in importParams)
                {
                    // Find matching argument by Name (not NameHuman, as Name is the unique identifier)
                    var existingArg = existingArgs.FirstOrDefault(a =>
                    {
                        var nameKey = a.Keys.FirstOrDefault(k => k.Equals("Name", StringComparison.OrdinalIgnoreCase));
                        return nameKey != null && string.Equals(a[nameKey]?.ToString(), importParam.Name, StringComparison.OrdinalIgnoreCase);
                    });
                    
                    if (existingArg == null)
                    {
                        result.Warnings.Add($"Parameter '{importParam.Name}' ('{importParam.NameHuman}') not found in {extensionName}, skipping");
                        continue;
                    }
                    
                    // Get current value
                    var valueKey = existingArg.Keys.FirstOrDefault(k => k.Equals("Value", StringComparison.OrdinalIgnoreCase));
                    var currentValue = valueKey != null ? existingArg[valueKey]?.ToString() : null;
                    
                    // Only update if value is different
                    if (currentValue != importParam.Value)
                    {
                        if (valueKey != null)
                        {
                            existingArg[valueKey] = importParam.Value;
                            updatedInExtension++;
                            result.UpdatedParameters++;
                        }
                    }
                }
                
                if (updatedInExtension > 0)
                {
                    // Save updated arguments back
                    configDict[argsKey] = existingArgs;
                    existingAppJson[configKey] = configDict;
                    result.UpdatedExtensions++;
                }
            }
            
            if (result.UpdatedParameters > 0)
            {
                result.Message = $"Updated {result.UpdatedParameters} parameter(s) across {result.UpdatedExtensions} extension(s)";
            }
            else
            {
                result.Message = "No parameter values were different - nothing to update";
            }
            
            return result;
        }

        /// <summary>
        /// Merges extension JSON objects
        /// </summary>
        private static void MergeExtensionJson(Dictionary<string, object> target, Dictionary<string, object> source)
        {
            // Update basic properties
            var simpleProps = new[] { "downloadUrl", "helpUrl", "changelogUrl", "descriptionShort", "chmod" };
            foreach (var prop in simpleProps)
            {
                if (source.ContainsKey(prop))
                {
                    target[prop] = source[prop];
                }
            }
            
            // Merge configuration
            if (source.ContainsKey("configuration") && source["configuration"] != null)
            {
                if (!target.ContainsKey("configuration") || target["configuration"] == null)
                {
                    target["configuration"] = source["configuration"];
                }
                else
                {
                    var targetConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(target["configuration"]));
                    var sourceConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(source["configuration"]));
                    
                    // Update config properties
                    if (sourceConfig.ContainsKey("prefix"))
                        targetConfig["prefix"] = sourceConfig["prefix"];
                    if (sourceConfig.ContainsKey("delimitter"))
                        targetConfig["delimitter"] = sourceConfig["delimitter"];
                    
                    // Merge arguments
                    if (sourceConfig.ContainsKey("arguments") && sourceConfig["arguments"] != null)
                    {
                        var sourceArgs = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                            JsonConvert.SerializeObject(sourceConfig["arguments"]));
                        
                        if (!targetConfig.ContainsKey("arguments") || targetConfig["arguments"] == null)
                        {
                            targetConfig["arguments"] = sourceArgs;
                        }
                        else
                        {
                            var targetArgs = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                                JsonConvert.SerializeObject(targetConfig["arguments"]));
                            
                            foreach (var sourceArg in sourceArgs)
                            {
                                var sourceArgName = sourceArg.ContainsKey("name") ? sourceArg["name"].ToString() : null;
                                if (string.IsNullOrEmpty(sourceArgName)) continue;
                                
                                var targetArg = targetArgs.FirstOrDefault(a => 
                                    a.ContainsKey("name") && 
                                    string.Equals(a["name"].ToString(), sourceArgName, StringComparison.OrdinalIgnoreCase));
                                
                                if (targetArg != null)
                                {
                                    var index = targetArgs.IndexOf(targetArg);
                                    targetArgs[index] = sourceArg;
                                }
                                else
                                {
                                    targetArgs.Add(sourceArg);
                                }
                            }
                            
                            targetConfig["arguments"] = targetArgs;
                        }
                    }
                    
                    target["configuration"] = targetConfig;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a filename for the export
        /// </summary>
        private static string GenerateExportFileName(ExportType type, string customName = null, List<string> extensionNames = null)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var typeName = type.ToString().ToLower();
            
            if (!string.IsNullOrWhiteSpace(customName))
            {
                // Sanitize custom name
                var invalidChars = Path.GetInvalidFileNameChars();
                var safeName = string.Join("_", customName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
                return $"export_{typeName}_{safeName}_{timestamp}.json";
            }
            
            if (extensionNames != null && extensionNames.Any() && extensionNames.Count <= 3)
            {
                var names = string.Join("-", extensionNames.Take(3));
                return $"export_{typeName}_{names}_{timestamp}.json";
            }
            
            return $"export_{typeName}_{timestamp}.json";
        }

        /// <summary>
        /// Saves the export to file
        /// </summary>
        private static async Task<string> SaveExport(ExportMetadata metadata, string fileName)
        {
            try
            {
                Directory.CreateDirectory(ExportsDirectory);
                
                var exportPath = Path.Combine(ExportsDirectory, fileName);
                var json = JsonConvert.SerializeObject(metadata, NewtonsoftSettings);
                
                await File.WriteAllTextAsync(exportPath, json);
                
                return exportPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save export: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lists all available extensions in the current configuration
        /// </summary>
        public static async Task<List<string>> ListAvailableExtensions()
        {
            var apps = await LoadAppsConfig();
            return apps.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Lists all parameters for a specific extension
        /// </summary>
        public static async Task<List<string>> ListExtensionParameters(string extensionName)
        {
            var apps = await LoadAppsConfig();
            var app = apps.FirstOrDefault(a => 
                string.Equals(a.Name, extensionName, StringComparison.OrdinalIgnoreCase));
            
            if (app?.Configuration?.Arguments == null)
            {
                return new List<string>();
            }
            
            return app.Configuration.Arguments.Select(a => a.Name).ToList();
        }

        /// <summary>
        /// Lists all export files
        /// </summary>
        public static List<FileInfo> ListExports()
        {
            if (!Directory.Exists(ExportsDirectory))
            {
                return new List<FileInfo>();
            }
            
            var dir = new DirectoryInfo(ExportsDirectory);
            return dir.GetFiles("*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.CreationTime)
                .ToList();
        }

        /// <summary>
        /// Gets metadata from an export file without importing
        /// </summary>
        public static async Task<ExportMetadata> GetExportInfo(string exportFilePath)
        {
            try
            {
                if (!File.Exists(exportFilePath))
                {
                    throw new FileNotFoundException($"Export file not found: {exportFilePath}");
                }
                
                var json = await File.ReadAllTextAsync(exportFilePath);
                var metadata = JsonConvert.DeserializeObject<ExportMetadata>(json);
                
                // Clear data to save memory when just reading info
                if (metadata != null)
                {
                    metadata.Data = new List<AppDownloadable>();
                }
                
                return metadata;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to read export info: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lists all configuration backups
        /// </summary>
        public static List<FileInfo> ListBackups()
        {
            if (!Directory.Exists(BackupsDirectory))
            {
                return new List<FileInfo>();
            }
            
            var dir = new DirectoryInfo(BackupsDirectory);
            return dir.GetFiles("*.json", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.CreationTime)
                .ToList();
        }

        #endregion
    }

    /// <summary>
    /// Import mode determines how conflicts are resolved
    /// </summary>
    public enum ImportMode
    {
        /// <summary>
        /// Merge with existing configuration (updates existing, adds new)
        /// </summary>
        Merge,
        
        /// <summary>
        /// Replace existing configuration completely
        /// </summary>
        Replace
    }

    /// <summary>
    /// Result of an import operation
    /// </summary>
    public class ImportResult
    {
        public ExportType ExportType { get; set; }
        public ImportMode ImportMode { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string BackupPath { get; set; }
        
        public int AddedExtensions { get; set; }
        public int UpdatedExtensions { get; set; }
        public int AddedParameters { get; set; }
        public int UpdatedParameters { get; set; }
        
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        
        public bool Success => !Errors.Any();
    }
}
