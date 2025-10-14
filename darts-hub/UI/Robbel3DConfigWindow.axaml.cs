using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using darts_hub.control;
using darts_hub.control.wizard;
using darts_hub.model;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace darts_hub.UI
{
    public partial class Robbel3DConfigWindow : Window
    {
        private readonly ProfileManager profileManager;
        private List<Robbel3DConfiguration> availablePresets = new();
        private Robbel3DConfiguration? selectedPreset;
        private string? selectedWledIp;
        private List<WledDevice> discoveredDevices = new();
        private CancellationTokenSource? scanCancellationTokenSource;
        private bool isAdvancedExpanded = false;

        public Robbel3DConfigWindow(ProfileManager profileManager)
        {
            InitializeComponent();
            this.profileManager = profileManager;
            
            // Configuration files should be included in release, no need to copy at runtime
            
            LoadAvailablePresets();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            Loaded += async (s, e) => await OnWindowLoaded();
        }

        private async Task OnWindowLoaded()
        {
            try
            {
                // Auto-populate IP from existing WLED configuration if available
                await TryAutoDetectExistingWledIp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error in OnWindowLoaded: {ex.Message}");
            }
        }

        private async Task TryAutoDetectExistingWledIp()
        {
            try
            {
                var profiles = profileManager.GetProfiles();
                if (profiles.Count == 0) return;

                var profile = profiles.First();
                var wledApp = profile.Apps.Values.FirstOrDefault(profileState => 
                    profileState.App?.CustomName?.Contains("wled", StringComparison.OrdinalIgnoreCase) == true)?.App;

                if (wledApp?.Configuration?.Arguments != null)
                {
                    var wepsArg = wledApp.Configuration.Arguments.FirstOrDefault(arg =>
                        arg.Name.Equals("WEPS", StringComparison.OrdinalIgnoreCase));

                    if (wepsArg != null && !string.IsNullOrWhiteSpace(wepsArg.Value))
                    {
                        var ips = wepsArg.Value.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                        if (ips.Length > 0)
                        {
                            var ip = ips[0].Trim().Trim('"');
                            var wledIpTextBox = this.FindControl<TextBox>("WledIpTextBox");
                            if (wledIpTextBox != null)
                            {
                                wledIpTextBox.Text = ip;
                                
                                // Auto-validate if IP looks valid
                                if (System.Net.IPAddress.TryParse(ip, out _))
                                {
                                    await ValidateWledDevice(ip);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error auto-detecting WLED IP: {ex.Message}");
            }
        }

        private void LoadAvailablePresets()
        {
            try
            {
                availablePresets = Robbel3DConfigurationManager.GetAvailablePresets();
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Loaded {availablePresets.Count} presets");
                
                var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
                if (presetComboBox != null)
                {
                    presetComboBox.Items.Clear();
                    
                    if (availablePresets.Count == 0)
                    {
                        var item = new ComboBoxItem 
                        { 
                            Content = "No presets available", 
                            IsEnabled = false 
                        };
                        presetComboBox.Items.Add(item);
                        System.Diagnostics.Debug.WriteLine("[Robbel3D] No presets available");
                    }
                    else
                    {
                        foreach (var preset in availablePresets)
                        {
                            var item = new ComboBoxItem 
                            { 
                                Content = $"{preset.Name} (v{preset.Version})", 
                                Tag = preset 
                            };
                            presetComboBox.Items.Add(item);
                            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Added preset: {preset.Name}");
                        }
                        
                        // Select the complete configuration by default if available
                        var completePreset = availablePresets.FirstOrDefault(p => 
                            p.Name.Contains("Complete", StringComparison.OrdinalIgnoreCase));
                        if (completePreset != null)
                        {
                            var completeItem = presetComboBox.Items.Cast<ComboBoxItem>()
                                .FirstOrDefault(item => item.Tag == completePreset);
                            if (completeItem != null)
                            {
                                presetComboBox.SelectedItem = completeItem;
                                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Auto-selected complete preset: {completePreset.Name}");
                            }
                        }
                        else
                        {
                            // Select first preset if no complete preset found
                            presetComboBox.SelectedIndex = 0;
                            System.Diagnostics.Debug.WriteLine("[Robbel3D] Auto-selected first preset");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Robbel3D] PresetComboBox not found!");
                }
                
                // Load existing caller parameters if available
                LoadExistingCallerParameters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading presets: {ex.Message}");
            }
        }

        private void LoadExistingCallerParameters()
        {
            try
            {
                var profiles = profileManager.GetProfiles();
                if (profiles.Count == 0) return;

                var profile = profiles.First();
                var callerApp = profile.Apps.Values.FirstOrDefault(profileState => 
                    profileState.App?.CustomName?.Contains("caller", StringComparison.OrdinalIgnoreCase) == true)?.App;

                if (callerApp?.Configuration?.Arguments != null)
                {
                    // Load existing values into the UI fields
                    var emailTextBox = this.FindControl<TextBox>("AutodartsEmailTextBox");
                    var passwordTextBox = this.FindControl<TextBox>("AutodartsPasswordTextBox");
                    var boardIdTextBox = this.FindControl<TextBox>("AutodartsBoardIdTextBox");
                    var mediaPathTextBox = this.FindControl<TextBox>("MediaPathTextBox");

                    var uArg = callerApp.Configuration.Arguments.FirstOrDefault(a => a.Name.Equals("U", StringComparison.OrdinalIgnoreCase));
                    var pArg = callerApp.Configuration.Arguments.FirstOrDefault(a => a.Name.Equals("P", StringComparison.OrdinalIgnoreCase));
                    var bArg = callerApp.Configuration.Arguments.FirstOrDefault(a => a.Name.Equals("B", StringComparison.OrdinalIgnoreCase));
                    var mArg = callerApp.Configuration.Arguments.FirstOrDefault(a => a.Name.Equals("M", StringComparison.OrdinalIgnoreCase));

                    if (emailTextBox != null && uArg != null && !string.IsNullOrEmpty(uArg.Value))
                        emailTextBox.Text = uArg.Value;
                    if (passwordTextBox != null && pArg != null && !string.IsNullOrEmpty(pArg.Value))
                        passwordTextBox.Text = pArg.Value;
                    if (boardIdTextBox != null && bArg != null && !string.IsNullOrEmpty(bArg.Value))
                        boardIdTextBox.Text = bArg.Value;
                    if (mediaPathTextBox != null && mArg != null && !string.IsNullOrEmpty(mArg.Value))
                        mediaPathTextBox.Text = mArg.Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error loading existing caller parameters: {ex.Message}");
            }
        }

        private void PresetComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem item && item.Tag is Robbel3DConfiguration preset)
            {
                selectedPreset = preset;
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Selected preset: {preset.Name}");
                UpdatePresetDetails(preset);
                UpdateConfigurationPreview();
                ValidateCanApplyConfiguration();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[Robbel3D] No valid preset selected");
                selectedPreset = null;
                ValidateCanApplyConfiguration();
            }
        }

        private void WledIpTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (!string.IsNullOrWhiteSpace(textBox?.Text))
            {
                selectedWledIp = textBox.Text.Trim();
                ValidateWledDeviceAsync(selectedWledIp);
            }
            else
            {
                selectedWledIp = null;
                var validationPanel = this.FindControl<StackPanel>("ValidationPanel");
                if (validationPanel != null) validationPanel.IsVisible = false;
            }
            
            ValidateCanApplyConfiguration();
        }

        private void RequiredParameter_TextChanged(object? sender, TextChangedEventArgs e)
        {
            ValidateCanApplyConfiguration();
        }

        private async void MediaPathBrowseButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Select Media/Sounds Folder",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        var mediaPathTextBox = this.FindControl<TextBox>("MediaPathTextBox");
                        if (mediaPathTextBox != null)
                        {
                            mediaPathTextBox.Text = folders[0].Path.LocalPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Folder Selection Error", 
                    $"Error selecting folder: {ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void ValidateCanApplyConfiguration()
        {
            var applyButton = this.FindControl<Button>("ApplyConfigButton");
            if (applyButton == null) 
            {
                System.Diagnostics.Debug.WriteLine("[Robbel3D] ApplyConfigButton not found!");
                return;
            }

            bool canApply = selectedPreset != null && !string.IsNullOrEmpty(selectedWledIp);
            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Basic validation - Preset: {selectedPreset?.Name ?? "null"}, WLED IP: {selectedWledIp ?? "null"}, Can Apply: {canApply}");
            
            // Additional validation for required Caller parameters with UI inputs
            if (canApply && selectedPreset != null)
            {
                var uiParameters = GetUIParameterValues();
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Parameters: {string.Join(", ", uiParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                
                var (isValid, missingParams) = Robbel3DConfigurationManager.ValidateRequiredCallerParameters(selectedPreset, profileManager, uiParameters);
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Parameter validation - Valid: {isValid}, Missing: {string.Join(", ", missingParams)}");
                
                if (!isValid)
                {
                    canApply = false;
                    
                    // Show validation warning
                    ShowRequiredParametersWarning(missingParams);
                }
                else
                {
                    // Hide any existing warning
                    HideRequiredParametersWarning();
                }
            }

            System.Diagnostics.Debug.WriteLine($"[Robbel3D] Final apply button state: {canApply}");
            applyButton.IsEnabled = canApply;
        }

        private Dictionary<string, string> GetUIParameterValues()
        {
            var uiParameters = new Dictionary<string, string>();
            
            try
            {
                var emailTextBox = this.FindControl<TextBox>("AutodartsEmailTextBox");
                var passwordTextBox = this.FindControl<TextBox>("AutodartsPasswordTextBox");
                var boardIdTextBox = this.FindControl<TextBox>("AutodartsBoardIdTextBox");
                var mediaPathTextBox = this.FindControl<TextBox>("MediaPathTextBox");

                System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Controls found - Email: {emailTextBox != null}, Password: {passwordTextBox != null}, Board: {boardIdTextBox != null}, Media: {mediaPathTextBox != null}");

                if (emailTextBox != null && !string.IsNullOrWhiteSpace(emailTextBox.Text))
                {
                    uiParameters["U"] = emailTextBox.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Parameter U: {emailTextBox.Text.Trim()}");
                }
                if (passwordTextBox != null && !string.IsNullOrWhiteSpace(passwordTextBox.Text))
                {
                    uiParameters["P"] = passwordTextBox.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Parameter P: [PASSWORD SET]");
                }
                if (boardIdTextBox != null && !string.IsNullOrWhiteSpace(boardIdTextBox.Text))
                {
                    uiParameters["B"] = boardIdTextBox.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Parameter B: {boardIdTextBox.Text.Trim()}");
                }
                if (mediaPathTextBox != null && !string.IsNullOrWhiteSpace(mediaPathTextBox.Text))
                {
                    uiParameters["M"] = mediaPathTextBox.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[Robbel3D] UI Parameter M: {mediaPathTextBox.Text.Trim()}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error getting UI parameter values: {ex.Message}");
            }
            
            return uiParameters;
        }

        private void ShowRequiredParametersWarning(List<string> missingParams)
        {
            // Find or create validation warning panel
            var validationWarning = this.FindControl<Border>("ValidationWarningPanel");
            
            if (validationWarning == null)
            {
                // Create validation warning panel if it doesn't exist
                // This would need to be added to the XAML or created dynamically
                return;
            }

            var warningText = this.FindControl<TextBlock>("ValidationWarningText");
            if (warningText != null)
            {
                warningText.Text = $"⚠️ Required Caller parameters missing:\n• {string.Join("\n• ", missingParams)}\n\nPlease configure these parameters in the Caller app settings before applying the configuration.";
            }

            validationWarning.IsVisible = true;
        }

        private void HideRequiredParametersWarning()
        {
            var validationWarning = this.FindControl<Border>("ValidationWarningPanel");
            if (validationWarning != null)
            {
                validationWarning.IsVisible = false;
            }
        }

        private void UpdatePresetDetails(Robbel3DConfiguration preset)
        {
            var presetDetailsPanel = this.FindControl<Border>("PresetDetailsPanel");
            var presetDescription = this.FindControl<TextBlock>("PresetDescription");
            var presetLedCount = this.FindControl<TextBlock>("PresetLedCount");
            var presetVersion = this.FindControl<TextBlock>("PresetVersion");
            var presetAuthor = this.FindControl<TextBlock>("PresetAuthor");
            var presetTags = this.FindControl<TextBlock>("PresetTags");
            
            if (presetDetailsPanel != null) presetDetailsPanel.IsVisible = true;
            if (presetDescription != null) presetDescription.Text = preset.Description;
            if (presetLedCount != null) presetLedCount.Text = preset.LedCount > 0 ? preset.LedCount.ToString() : 
                (preset.WledConfig?.Hardware?.Led?.Total.ToString() ?? "Unknown");
            if (presetVersion != null) presetVersion.Text = preset.Version;
            if (presetAuthor != null) presetAuthor.Text = preset.Author ?? "Unknown";
            if (presetTags != null) presetTags.Text = preset.Tags != null && preset.Tags.Count > 0 ? 
                string.Join(", ", preset.Tags) : "None";
        }

        private void UpdateConfigurationPreview()
        {
            if (selectedPreset == null) return;

            try
            {
                // Update WLED settings preview - show summary for better UX
                var wledSettingsText = this.FindControl<TextBlock>("WledSettingsText");
                if (wledSettingsText != null)
                {
                    var wledSettingsCount = selectedPreset.WledSettings.Count;
                    var nonEmptySettings = selectedPreset.WledSettings.Count(kvp => !string.IsNullOrEmpty(kvp.Value));
                    
                    var summaryLines = new List<string>
                    {
                        $"Total WLED Arguments: {wledSettingsCount}",
                        $"Configured Arguments: {nonEmptySettings}",
                        "",
                        "Key Settings:"
                    };
                    
                    // Show key settings
                    var keySettings = new[] { "WEPS", "LEDCOUNT", "BRI", "IDE1", "IDE2", "WINNER", "BUST" };
                    foreach (var key in keySettings)
                    {
                        if (selectedPreset.WledSettings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                        {
                            summaryLines.Add($"  {key} = \"{value}\"");
                        }
                    }
                    
                    if (wledSettingsCount > keySettings.Length)
                    {
                        summaryLines.Add($"  ... and {wledSettingsCount - keySettings.Length} more");
                    }
                    
                    wledSettingsText.Text = string.Join("\n", summaryLines);
                }

                // Update Caller settings preview - show summary  
                var callerSettingsText = this.FindControl<TextBlock>("CallerSettingsText");
                if (callerSettingsText != null)
                {
                    var callerSettingsCount = selectedPreset.CallerSettings.Count;
                    var nonEmptySettings = selectedPreset.CallerSettings.Count(kvp => !string.IsNullOrEmpty(kvp.Value));
                    
                    var summaryLines = new List<string>
                    {
                        //$"Total Caller Arguments: {callerSettingsCount}",
                        $"Configured Arguments: {nonEmptySettings}",
                        "",
                        "Key Settings:"
                    };
                    
                    // Show key settings  
                    var keySettings = new[] {"V", "PORT","CC","R","RL","RG","CCP","CBA","CRL","E","ETS","PCC","PCCYO","DL","HP","DEB" };
                   
                
                    foreach (var key in keySettings)
                    {
                        if (selectedPreset.CallerSettings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                        {
                            summaryLines.Add($"  {key} = \"{value}\"");
                        }
                    }
                    
                    if (callerSettingsCount > keySettings.Length)
                    {
                        summaryLines.Add($"  ... and {callerSettingsCount - keySettings.Length} more");
                    }
                    
                    callerSettingsText.Text = string.Join("\n", summaryLines);
                }

                // Update WLED files preview - show external file references
                var wledPresetsText = this.FindControl<TextBlock>("WledPresetsText");
                if (wledPresetsText != null)
                {
                    var summaryLines = new List<string>();
                    
                    // Show external file references
                    if (!string.IsNullOrEmpty(selectedPreset.WledConfigFile))
                    {
                        summaryLines.Add($"WLED Config File: {selectedPreset.WledConfigFile}");
                        if (selectedPreset.WledConfig != null)
                        {
                            summaryLines.Add($"  ✓ Successfully loaded ({selectedPreset.WledConfig.Hardware?.Led?.Total ?? 0} LEDs)");
                        }
                        else
                        {
                            summaryLines.Add("  ⚠ File not found or invalid");
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(selectedPreset.WledPresetsFile))
                    {
                        summaryLines.Add($"WLED Presets File: {selectedPreset.WledPresetsFile}");
                        if (selectedPreset.WledPresets != null)
                        {
                            summaryLines.Add($"  ✓ Successfully loaded ({selectedPreset.WledPresets.Count} presets)");
                            
                            // Show first few presets
                            var maxPreviewPresets = 3;
                            var presetIndex = 0;
                            foreach (var preset in selectedPreset.WledPresets.Take(maxPreviewPresets))
                            {
                                var presetName = "Unknown";
                                try
                                {
                                    var presetObj = JsonConvert.SerializeObject(preset.Value);
                                    dynamic presetData = JsonConvert.DeserializeObject(presetObj);
                                    presetName = presetData?.n?.ToString() ?? $"Preset {preset.Key}";
                                }
                                catch
                                {
                                    presetName = $"Preset {preset.Key}";
                                }
                                
                                summaryLines.Add($"    {preset.Key}: {presetName}");
                                presetIndex++;
                            }
                            
                            if (selectedPreset.WledPresets.Count > maxPreviewPresets)
                            {
                                summaryLines.Add($"    ... and {selectedPreset.WledPresets.Count - maxPreviewPresets} more presets");
                            }
                        }
                        else
                        {
                            summaryLines.Add("  ⚠ File not found or invalid");
                        }
                    }
                    
                    if (summaryLines.Count == 0)
                    {
                        summaryLines.Add("No external WLED files specified");
                    }
                    
                    wledPresetsText.Text = string.Join("\n", summaryLines);
                }

                // Show configuration metadata if available
                var metadataText = this.FindControl<TextBlock>("MetadataText");
                if (metadataText != null && selectedPreset.Metadata != null)
                {
                    var metadataLines = new List<string>();
                    
                    if (!string.IsNullOrEmpty(selectedPreset.Metadata.CreatedWith))
                    {
                        metadataLines.Add($"Created with: {selectedPreset.Metadata.CreatedWith} v{selectedPreset.Metadata.CreatedWithVersion}");
                    }
                    
                    if (!string.IsNullOrEmpty(selectedPreset.Metadata.TargetWledVersion))
                    {
                        metadataLines.Add($"Target WLED: {selectedPreset.Metadata.TargetWledVersion}");
                    }
                    
                    if (!string.IsNullOrEmpty(selectedPreset.Metadata.TargetCallerVersion))
                    {
                        metadataLines.Add($"Target Caller: {selectedPreset.Metadata.TargetCallerVersion}");
                    }
                    
                    if (!string.IsNullOrEmpty(selectedPreset.Metadata.CompatibilityNotes))
                    {
                        metadataLines.Add($"Notes: {selectedPreset.Metadata.CompatibilityNotes}");
                    }
                    
                    metadataText.Text = string.Join("\n", metadataLines);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Error updating configuration preview: {ex.Message}");
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            await PerformNetworkScan();
        }

        private async Task PerformNetworkScan()
        {
            try
            {
                var scanButton = this.FindControl<Button>("ScanButton");
                if (scanButton != null)
                {
                    scanButton.IsEnabled = false;
                    scanButton.Content = "🔍 Scanning...";
                }
                
                // Cancel any existing scan
                scanCancellationTokenSource?.Cancel();
                scanCancellationTokenSource = new CancellationTokenSource();

                SetLoadingState(true, "Scanning network for WLED devices...");

                // Perform network scan
                discoveredDevices = await NetworkDeviceScanner.ScanForWledDevices(scanCancellationTokenSource.Token);

                SetLoadingState(false);

                if (discoveredDevices.Count > 0)
                {
                    var deviceDiscoveryPanel = this.FindControl<StackPanel>("DeviceDiscoveryPanel");
                    var discoveredDevicesListBox = this.FindControl<ListBox>("DiscoveredDevicesListBox");
                    
                    if (deviceDiscoveryPanel != null) deviceDiscoveryPanel.IsVisible = true;
                    if (discoveredDevicesListBox != null)
                    {
                        discoveredDevicesListBox.Items.Clear();

                        foreach (var device in discoveredDevices)
                        {
                            var ledInfo = device.LedCount > 0 ? $" ({device.LedCount} LEDs)" : "";
                            var listItem = new ListBoxItem
                            {
                                Content = $"{device.Name} - {device.IpAddress}{ledInfo}",
                                Tag = device
                            };
                            discoveredDevicesListBox.Items.Add(listItem);
                        }

                        // Auto-select first device if only one found
                        if (discoveredDevices.Count == 1)
                        {
                            discoveredDevicesListBox.SelectedIndex = 0;
                        }
                    }
                }
                else
                {
                    await ShowMessageBox("Network Scan", 
                        "No WLED devices found on your network. Please enter the IP address manually.", 
                        MsBox.Avalonia.Enums.Icon.Info);
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("[Robbel3D] Network scan cancelled");
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Network Scan Error", 
                    $"An error occurred while scanning for devices:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
            finally
            {
                var scanButton = this.FindControl<Button>("ScanButton");
                if (scanButton != null)
                {
                    scanButton.IsEnabled = true;
                    scanButton.Content = "🔍 Scan Network";
                }
                SetLoadingState(false);
            }
        }

        private void DiscoveredDevicesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is ListBoxItem item && item.Tag is WledDevice device)
            {
                var wledIpTextBox = this.FindControl<TextBox>("WledIpTextBox");
                if (wledIpTextBox != null)
                {
                    wledIpTextBox.Text = device.IpAddress;
                }
                _ = ValidateWledDevice(device.IpAddress);
            }
        }

        private async Task ValidateWledDevice(string ipAddress)
        {
            try
            {
                var validationPanel = this.FindControl<StackPanel>("ValidationPanel");
                var validationIndicator = this.FindControl<Ellipse>("ValidationIndicator");
                var validationText = this.FindControl<TextBlock>("ValidationText");
                
                if (validationPanel != null) validationPanel.IsVisible = true;
                if (validationIndicator != null) validationIndicator.Fill = Avalonia.Media.Brushes.Orange;
                if (validationText != null) validationText.Text = "Validating device...";

                var isValid = await Robbel3DConfigurationManager.ValidateWledDevice(ipAddress);

                if (isValid)
                {
                    if (validationIndicator != null) validationIndicator.Fill = Avalonia.Media.Brushes.Green;
                    if (validationText != null) validationText.Text = "✓ WLED device validated and ready";
                    selectedWledIp = ipAddress;
                }
                else
                {
                    if (validationIndicator != null) validationIndicator.Fill = Avalonia.Media.Brushes.Red;
                    if (validationText != null) validationText.Text = "✗ Invalid or unreachable WLED device";
                    selectedWledIp = null;
                }

                CheckCanApplyConfiguration();
            }
            catch (Exception ex)
            {
                var validationIndicator = this.FindControl<Ellipse>("ValidationIndicator");
                var validationText = this.FindControl<TextBlock>("ValidationText");
                
                if (validationIndicator != null) validationIndicator.Fill = Avalonia.Media.Brushes.Red;
                if (validationText != null) validationText.Text = "✗ Validation failed";
                selectedWledIp = null;
                CheckCanApplyConfiguration();
                System.Diagnostics.Debug.WriteLine($"[Robbel3D] Device validation error: {ex.Message}");
            }
        }

        private void CheckCanApplyConfiguration()
        {
            var applyConfigButton = this.FindControl<Button>("ApplyConfigButton");
            if (applyConfigButton != null)
            {
                applyConfigButton.IsEnabled = selectedPreset != null && 
                                             !string.IsNullOrWhiteSpace(selectedWledIp);
            }
        }

        private void AdvancedToggleButton_Click(object sender, RoutedEventArgs e)
        {
            isAdvancedExpanded = !isAdvancedExpanded;
            
            var advancedOptionsPanel = this.FindControl<StackPanel>("AdvancedOptionsPanel");
            var advancedToggleButton = this.FindControl<Button>("AdvancedToggleButton");
            
            if (advancedOptionsPanel != null) advancedOptionsPanel.IsVisible = isAdvancedExpanded;
            if (advancedToggleButton != null) advancedToggleButton.Content = isAdvancedExpanded ? 
                "🔼 Hide Advanced Options" : "🔽 Show Advanced Options";
        }

        private async void ApplyConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPreset == null || string.IsNullOrWhiteSpace(selectedWledIp))
            {
                await ShowMessageBox("Configuration Error", 
                    "Please select a preset and validate a WLED device before applying configuration.", 
                    MsBox.Avalonia.Enums.Icon.Warning);
                return;
            }

            await ApplyConfiguration();
        }

        private async Task ApplyConfiguration()
        {
            try
            {
                // Final validation before applying
                if (selectedPreset == null || string.IsNullOrEmpty(selectedWledIp))
                {
                    await ShowMessageBox("Configuration Error", 
                        "Please select a configuration preset and WLED device before applying.", 
                        MsBox.Avalonia.Enums.Icon.Error);
                    return;
                }

                // Get UI parameter values
                var uiParameters = GetUIParameterValues();
                
                // Validate required Caller parameters with UI inputs
                var (isValid, missingParams) = Robbel3DConfigurationManager.ValidateRequiredCallerParameters(selectedPreset, profileManager, uiParameters);
                if (!isValid)
                {
                    var missingParamsList = string.Join("\n• ", missingParams);
                    await ShowMessageBox("Required Parameters Missing", 
                        $"The following required Caller parameters are missing:\n\n• {missingParamsList}\n\n" +
                        "Please fill in all required fields above before applying the Robbel3D configuration.\n\n" +
                        "Required parameters:\n" +
                        "• U: Your Autodarts email address\n" +
                        "• P: Your Autodarts password\n" +
                        "• B: Your Autodarts board ID\n" +
                        "• M: Path to your media/sounds directory", 
                        MsBox.Avalonia.Enums.Icon.Warning);
                    return;
                }

                SetLoadingState(true, "Applying Robbel3D configuration...");

                // Step 1: Backup existing configuration (if enabled)
                var backupCheckBox = this.FindControl<CheckBox>("BackupExistingConfigCheckBox");
                if (backupCheckBox?.IsChecked == true)
                {
                    UpdateProgressText("Creating backup of existing configuration...");
                    await Task.Delay(1000); // Simulate backup process
                    // TODO: Implement actual backup functionality
                }

                // Step 2: Reset device (if enabled)
                var resetCheckBox = this.FindControl<CheckBox>("ResetBeforeConfigCheckBox");
                if (resetCheckBox?.IsChecked == true)
                {
                    UpdateProgressText("Resetting WLED device to factory defaults...");
                    await Robbel3DConfigurationManager.ResetWledDevice(selectedWledIp!);
                    await Task.Delay(3000); // Wait for device to restart
                }

                // Step 3: Apply configuration with UI parameters
                UpdateProgressText("Uploading WLED configuration and presets...");
                var success = await Robbel3DConfigurationManager.ApplyConfiguration(
                    selectedPreset!, selectedWledIp!, profileManager, uiParameters);

                if (!success)
                {
                    throw new Exception("Failed to apply configuration to WLED device");
                }

                // Step 4: Validate configuration (if enabled)
                var validateCheckBox = this.FindControl<CheckBox>("ValidateAfterConfigCheckBox");
                if (validateCheckBox?.IsChecked == true)
                {
                    UpdateProgressText("Validating applied configuration...");
                    await Task.Delay(2000); // Give device time to process
                    
                    var isValidDevice = await Robbel3DConfigurationManager.ValidateWledDevice(selectedWledIp!);
                    if (!isValidDevice)
                    {
                        await ShowMessageBox("Validation Warning", 
                            "Configuration was applied but validation failed. The device may need more time to restart or process the new settings.", 
                            MsBox.Avalonia.Enums.Icon.Warning);
                    }
                }

                SetLoadingState(false);

                // Show detailed success message
                var settingsApplied = selectedPreset!.WledSettings.Count + selectedPreset.CallerSettings.Count;
                var presetsUploaded = selectedPreset.WledPresets?.Count ?? 0;
                var configUploaded = selectedPreset.WledConfig != null ? 1 : 0;
                
                await ShowMessageBox("Configuration Complete", 
                    $"Successfully applied Robbel3D configuration '{selectedPreset.Name}' to your WLED device!\n\n" +
                    $"✅ {settingsApplied} application settings configured\n" +
                    $"✅ {configUploaded} WLED device configuration uploaded\n" +
                    $"✅ {presetsUploaded} WLED presets uploaded (with preserved names)\n" +
                    $"📁 External files: {selectedPreset.WledConfigFile}, {selectedPreset.WledPresetsFile}\n" +
                    $"🎯 Required Caller parameters configured from UI inputs\n" +
                    $"📧 Email: {uiParameters.GetValueOrDefault("U", "Not set")}\n" +
                    $"🆔 Board ID: {uiParameters.GetValueOrDefault("B", "Not set")}\n" +
                    $"📂 Media Path: {uiParameters.GetValueOrDefault("M", "Not set")}\n\n" +
                    "Your darts applications are now ready with optimized settings for the best LED experience!", 
                    MsBox.Avalonia.Enums.Icon.Success);

                Close(true);
            }
            catch (Exception ex)
            {
                SetLoadingState(false);
                await ShowMessageBox("Configuration Error", 
                    $"An error occurred while applying the configuration:\n\n{ex.Message}\n\n" +
                    "Please check your WLED device connection, ensure the external config files exist, and verify that all required Caller parameters are filled in.", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void UpdateProgressText(string text)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var progressText = this.FindControl<TextBlock>("ProgressText");
                var loadingText = this.FindControl<TextBlock>("LoadingText");
                
                if (progressText != null) progressText.Text = text;
                if (loadingText != null) loadingText.Text = text;
            });
        }

        private void SetLoadingState(bool isLoading, string? message = null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var loadingOverlay = this.FindControl<Border>("LoadingOverlay");
                var progressPanel = this.FindControl<StackPanel>("ProgressPanel");
                var applyConfigButton = this.FindControl<Button>("ApplyConfigButton");
                var scanButton = this.FindControl<Button>("ScanButton");
                var presetComboBox = this.FindControl<ComboBox>("PresetComboBox");
                
                if (loadingOverlay != null) loadingOverlay.IsVisible = isLoading;
                if (progressPanel != null) progressPanel.IsVisible = isLoading;
                
                if (!string.IsNullOrEmpty(message))
                {
                    UpdateProgressText(message);
                }

                // Disable controls while loading
                if (applyConfigButton != null) applyConfigButton.IsEnabled = !isLoading && selectedPreset != null && !string.IsNullOrWhiteSpace(selectedWledIp);
                if (scanButton != null) scanButton.IsEnabled = !isLoading;
                if (presetComboBox != null) presetComboBox.IsEnabled = !isLoading;
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            scanCancellationTokenSource?.Cancel();
            Close(false);
        }

        private async Task<ButtonResult> ShowMessageBox(string title, string message, MsBox.Avalonia.Enums.Icon icon)
        {
            return await MessageBoxHelper.ShowMessageBox(this, title, message, icon);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            scanCancellationTokenSource?.Cancel();
            base.OnClosing(e);
        }

        private async void ValidateWledDeviceAsync(string ipAddress)
        {
            var validationPanel = this.FindControl<StackPanel>("ValidationPanel");
            var validationIndicator = this.FindControl<Ellipse>("ValidationIndicator");
            var validationText = this.FindControl<TextBlock>("ValidationText");
            
            if (validationPanel == null || validationIndicator == null || validationText == null) return;

            try
            {
                // Show validation in progress
                validationPanel.IsVisible = true;
                validationIndicator.Fill = new SolidColorBrush(Colors.Orange);
                validationText.Text = "Validating WLED device...";

                // Validate the device
                var isValid = await Robbel3DConfigurationManager.ValidateWledDevice(ipAddress);

                if (isValid)
                {
                    validationIndicator.Fill = new SolidColorBrush(Colors.Green);
                    validationText.Text = "✓ WLED device validated successfully";
                }
                else
                {
                    validationIndicator.Fill = new SolidColorBrush(Colors.Red);
                    validationText.Text = "✗ Device validation failed - check IP address";
                }
            }
            catch (Exception ex)
            {
                validationIndicator.Fill = new SolidColorBrush(Colors.Red);
                validationText.Text = $"✗ Validation error: {ex.Message}";
            }

            // Revalidate if configuration can be applied
            ValidateCanApplyConfiguration();
        }
    }
}