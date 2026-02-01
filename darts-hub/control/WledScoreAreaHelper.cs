using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using darts_hub.model;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace darts_hub.control
{
    /// <summary>
    /// Helper class for WLED Score Area Effects with range selection
    /// </summary>
    public static class WledScoreAreaHelper
    {
        /// <summary>
        /// Checks if a parameter value is a color effect
        /// </summary>
        private static bool IsColorEffect(string value)
        {
            if (string.IsNullOrEmpty(value)) 
            {
                System.Diagnostics.Debug.WriteLine($"IsColorEffect('{value}') = false (null/empty)");
                return false;
            }
            
            // Check if it's a simple color effect (just a color name)
            if (NewSettingsContentProvider.ColorEffects.Contains(value))
            {
                System.Diagnostics.Debug.WriteLine($"IsColorEffect('{value}') = true (direct match)");
                return true;
            }
            
            // Check if it's a color effect with solid prefix (solid|colorname format)
            if (value.StartsWith("solid|", StringComparison.OrdinalIgnoreCase))
            {
                var colorName = value.Substring(6); // Remove "solid|" prefix
                bool isColor = NewSettingsContentProvider.ColorEffects.Contains(colorName);
                System.Diagnostics.Debug.WriteLine($"IsColorEffect('{value}') = {isColor} (solid| prefix, colorName='{colorName}')");
                return isColor;
            }
            
            System.Diagnostics.Debug.WriteLine($"IsColorEffect('{value}') = false (no match)");
            return false;
        }

        /// <summary>
        /// Checks if a parameter value is a WLED effect
        /// </summary>
        private static bool IsWledEffect(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            var parts = value.Split('|');
            if (parts.Length == 0) return false;
            
            var effectName = parts[0];
            
            // Check if the effect name is in the fallback categories (known WLED effects)
            var allFallbackEffects = WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
            if (allFallbackEffects.Contains(effectName))
            {
                System.Diagnostics.Debug.WriteLine($"IsWledEffect: '{effectName}' found in fallback effects");
                return true;
            }
            
            // Check if it has the new format with prefixed parameters (s{value}, i{value}, p{value}, d{value})
            if (parts.Length > 1)
            {
                bool hasNewFormatParams = false;
                for (int i = 1; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (!string.IsNullOrEmpty(part) && 
                        (part.StartsWith("s") || part.StartsWith("i") || part.StartsWith("p") || part.StartsWith("d")))
                    {
                        hasNewFormatParams = true;
                        break;
                    }
                }
                
                if (hasNewFormatParams)
                {
                    System.Diagnostics.Debug.WriteLine($"IsWledEffect: '{effectName}' has new format parameters");
                    return true;
                }
            }
            
            // Check if it has the old format with 4 parts (effect|palette|speed|intensity)
            if (parts.Length == 4)
            {
                // Try to parse speed and intensity as numbers
                if (int.TryParse(parts[2], out _) && int.TryParse(parts[3], out _))
                {
                    System.Diagnostics.Debug.WriteLine($"IsWledEffect: '{effectName}' has old 4-part format");
                    return true;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"IsWledEffect: '{value}' not recognized as WLED effect");
            return false;
        }

        /// <summary>
        /// Creates a score area effect parameter control with range dropdowns and effect selection
        /// </summary>
        public static Control CreateScoreAreaEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Input mode selector (always visible at top)
            var modeSelector = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select input mode...",
                Margin = new Thickness(0, 0, 0, 5)
            };

            var manualItem = new ComboBoxItem { Content = "🖊️ Manual Input", Tag = "manual", Foreground = Brushes.White };
            var effectsItem = new ComboBoxItem { Content = "✨ WLED Effects", Tag = "effects", Foreground = Brushes.White };
            var presetsItem = new ComboBoxItem { Content = "🎨 Presets", Tag = "presets", Foreground = Brushes.White };
            var colorsItem = new ComboBoxItem { Content = "🌈 Color Effects", Tag = "colors", Foreground = Brushes.White };

            modeSelector.Items.Add(manualItem);
            modeSelector.Items.Add(effectsItem);
            modeSelector.Items.Add(presetsItem);
            modeSelector.Items.Add(colorsItem);

            // Range selection panel (only visible for automatic modes)
            var rangePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 5),
                IsVisible = false // Hidden initially and for manual mode
            };

            var rangeLabel = new TextBlock
            {
                Text = "Score Range:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80
            };

            var fromDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "From",
                Width = 80
            };

            var toLabel = new TextBlock
            {
                Text = "to",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };

            var toDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "To",
                Width = 80
            };

            // Populate range dropdowns
            for (int i = 1; i <= 179; i++)
            {
                fromDropdown.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
            }

            for (int i = 2; i <= 180; i++)
            {
                toDropdown.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
            }

            // Container for the effect input control
            var effectContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0),
                IsVisible = true // Always visible initially
            };

            // Parse current value if exists (format: "from-to effect" or manual format)
            int? selectedFrom = null;
            int? selectedTo = null;
            string? effectPart = null;
            bool isManualMode = false;

            System.Diagnostics.Debug.WriteLine($"=== SCORE AREA PARSING START ===");
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: '{param.Name}'");

            if (!string.IsNullOrEmpty(param.Value))
            {
                var mainParts = param.Value.Split(' ', 2);
                System.Diagnostics.Debug.WriteLine($"Main parts count: {mainParts.Length}");
                for (int i = 0; i < mainParts.Length; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"Main part {i}: '{mainParts[i]}'");
                }

                if (mainParts.Length >= 2)
                {
                    var rangeParts = mainParts[0].Split('-');
                    System.Diagnostics.Debug.WriteLine($"Range parts count: {rangeParts.Length}");
                    for (int i = 0; i < rangeParts.Length; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"Range part {i}: '{rangeParts[i]}'");
                    }

                    if (rangeParts.Length == 2 && 
                        int.TryParse(rangeParts[0], out var from) && 
                        int.TryParse(rangeParts[1], out var to))
                    {
                        // This is a range-based value (automatic mode)
                        selectedFrom = from;
                        selectedTo = to;
                        effectPart = mainParts[1];
                        
                        System.Diagnostics.Debug.WriteLine($"AUTOMATIC MODE DETECTED:");
                        System.Diagnostics.Debug.WriteLine($"  From: {selectedFrom}");
                        System.Diagnostics.Debug.WriteLine($"  To: {selectedTo}");
                        System.Diagnostics.Debug.WriteLine($"  Effect Part: '{effectPart}'");
                        
                        // Set range dropdowns
                        foreach (ComboBoxItem item in fromDropdown.Items)
                        {
                            if (item.Tag is int value && value == from)
                            {
                                fromDropdown.SelectedItem = item;
                                System.Diagnostics.Debug.WriteLine($"  Selected FROM dropdown: {value}");
                                break;
                            }
                        }
                        
                        foreach (ComboBoxItem item in toDropdown.Items)
                        {
                            if (item.Tag is int value && value == to)
                            {
                                toDropdown.SelectedItem = item;
                                System.Diagnostics.Debug.WriteLine($"  Selected TO dropdown: {value}");
                                break;
                            }
                        }
                        
                        // Set mode based on effect type - prioritize color effects first
                        System.Diagnostics.Debug.WriteLine($"  Determining mode for effect: '{effectPart}'");
                        
                        if (IsColorEffect(effectPart))
                        {
                            modeSelector.SelectedItem = colorsItem;
                            System.Diagnostics.Debug.WriteLine($"  MODE: COLORS (detected color effect)");
                        }
                        else if (IsPresetParameter(effectPart))
                        {
                            modeSelector.SelectedItem = presetsItem;
                            System.Diagnostics.Debug.WriteLine($"  MODE: PRESETS (detected preset parameter)");
                        }
                        else if (IsWledEffect(effectPart))
                        {
                            modeSelector.SelectedItem = effectsItem;
                            System.Diagnostics.Debug.WriteLine($"  MODE: EFFECTS (detected WLED effect parameter)");
                        }
                        else
                        {
                            modeSelector.SelectedItem = manualItem;
                            isManualMode = true;
                            System.Diagnostics.Debug.WriteLine($"  MODE: MANUAL (fallback)");
                        }
                    }
                    else
                    {
                        // This is manual format - entire value goes to manual input
                        isManualMode = true;
                        modeSelector.SelectedItem = manualItem;
                        System.Diagnostics.Debug.WriteLine($"MANUAL MODE: Invalid range format");
                    }
                }
                else
                {
                    // Single value - manual mode
                    isManualMode = true;
                    modeSelector.SelectedItem = manualItem;
                    System.Diagnostics.Debug.WriteLine($"MANUAL MODE: Single value");
                }
            }
            else
            {
                // Default to manual mode for empty values
                modeSelector.SelectedItem = manualItem;
                isManualMode = true;
                System.Diagnostics.Debug.WriteLine($"MANUAL MODE: Empty value");
            }

            // Show/hide panels based on current mode
            rangePanel.IsVisible = !isManualMode;
            
            System.Diagnostics.Debug.WriteLine($"=== UI VISIBILITY SETUP ===");
            System.Diagnostics.Debug.WriteLine($"Manual Mode: {isManualMode}");
            System.Diagnostics.Debug.WriteLine($"Range Panel Visible: {rangePanel.IsVisible}");
            
            // For automatic modes, validate range immediately
            if (!isManualMode)
            {
                var fromItem = fromDropdown.SelectedItem as ComboBoxItem;
                var toItem = toDropdown.SelectedItem as ComboBoxItem;
                
                bool isValidRange = fromItem?.Tag is int fromValue && 
                                   toItem?.Tag is int toValue && 
                                   fromValue < toValue;
                
                effectContainer.IsVisible = isValidRange;
                
                System.Diagnostics.Debug.WriteLine($"Range Validation:");
                System.Diagnostics.Debug.WriteLine($"  From Item: {fromItem?.Tag}");
                System.Diagnostics.Debug.WriteLine($"  To Item: {toItem?.Tag}");
                System.Diagnostics.Debug.WriteLine($"  Is Valid Range: {isValidRange}");
                System.Diagnostics.Debug.WriteLine($"  Effect Container Visible: {effectContainer.IsVisible}");
            }
            else
            {
                effectContainer.IsVisible = true; // Always visible for manual mode
                System.Diagnostics.Debug.WriteLine($"Manual mode - Effect Container Visible: {effectContainer.IsVisible}");
            }

            // Store effect part for range-based modes, or use full value for manual mode
            if (isManualMode)
            {
                effectPart = param.Value; // Use entire value for manual mode
            }

            // Flag to prevent recursive updates
            bool isUpdating = false;

            // Function to update parameter value
            void UpdateParameterValue()
            {
                if (isUpdating) return;

                var currentMode = (modeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                
                if (currentMode == "manual")
                {
                    // For manual mode, the effectPart IS the complete parameter value
                    isUpdating = true;
                    try
                    {
                        param.Value = effectPart;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                }
                else
                {
                    // For automatic modes, combine range with effect
                    var fromItem = fromDropdown.SelectedItem as ComboBoxItem;
                    var toItem = toDropdown.SelectedItem as ComboBoxItem;
                    
                    if (fromItem?.Tag is int fromValue && 
                        toItem?.Tag is int toValue &&
                        fromValue < toValue &&
                        !string.IsNullOrEmpty(effectPart))
                    {
                        isUpdating = true;
                        try
                        {
                            param.Value = $"{fromValue}-{toValue} {effectPart}";
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        }
                        finally
                        {
                            isUpdating = false;
                        }
                    }
                    else if (fromItem?.Tag is int && toItem?.Tag is int)
                    {
                        // Clear parameter if range is valid but effect is empty
                        isUpdating = true;
                        try
                        {
                            param.Value = null;
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        }
                        finally
                        {
                            isUpdating = false;
                        }
                    }
                }
            }

            // Function to validate and update range selection (only for automatic modes)
            void ValidateRangeSelection()
            {
                var currentMode = (modeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                
                if (currentMode == "manual")
                {
                    // For manual mode, always allow input
                    effectContainer.IsVisible = true;
                    return;
                }

                var fromItem = fromDropdown.SelectedItem as ComboBoxItem;
                var toItem = toDropdown.SelectedItem as ComboBoxItem;
                
                bool isValidRange = fromItem?.Tag is int fromValue && 
                                   toItem?.Tag is int toValue && 
                                   fromValue < toValue;
                
                effectContainer.IsVisible = isValidRange;
                
                if (!isValidRange && !isUpdating)
                {
                    // Clear parameter value if range is invalid in automatic modes
                    isUpdating = true;
                    try
                    {
                        param.Value = null;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                }
            }

            // Range dropdown event handlers
            fromDropdown.SelectionChanged += (s, e) =>
            {
                // Update "to" dropdown minimum based on "from" selection
                if (fromDropdown.SelectedItem is ComboBoxItem fromItem && fromItem.Tag is int fromValue)
                {
                    toDropdown.Items.Clear();
                    for (int i = fromValue + 1; i <= 180; i++)
                    {
                        toDropdown.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i, Foreground = Brushes.White });
                    }
                    
                    // Reset "to" selection if it's now invalid
                    if (toDropdown.SelectedItem is ComboBoxItem toItem && toItem.Tag is int toValue && toValue <= fromValue)
                    {
                        toDropdown.SelectedItem = null;
                    }
                }
                
                ValidateRangeSelection();
                UpdateParameterValue();
            };

            toDropdown.SelectionChanged += (s, e) =>
            {
                ValidateRangeSelection();
                UpdateParameterValue();
            };

            // Mode selector event handler
            modeSelector.SelectionChanged += async (s, e) =>
            {
                if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                {
                    var mode = selectedItem.Tag?.ToString();
                    
                    // Show/hide range panel based on mode
                    rangePanel.IsVisible = mode != "manual";
                    
                    // Show loading indicator
                    effectContainer.Child = new TextBlock 
                    { 
                        Text = "Loading...", 
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    
                    // For manual mode, use the full parameter value; for others, create temporary parameter
                    var tempParam = new Argument("temp", Argument.TypeString, false);
                    
                    if (mode == "manual")
                    {
                        tempParam.Value = param.Value; // Use full value for manual mode
                        effectContainer.IsVisible = true; // Always show for manual mode
                    }
                    else
                    {
                        tempParam.Value = effectPart; // Use just effect part for automatic modes
                        ValidateRangeSelection(); // Check if container should be visible
                    }
                    
                    Control newControl = mode switch
                    {
                        "manual" => CreateManualEffectInput(tempParam, () => { 
                            effectPart = tempParam.Value; 
                            param.Value = tempParam.Value; // Direct assignment for manual mode
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        }),
                        "effects" => await CreateWledEffectsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app),
                        "presets" => await CreateWledPresetsDropdownWithState(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app, ExtractPresetFromEffectPart(effectPart), selectedTo ?? 0, selectedFrom ?? 0),
                        "colors" => CreateColorEffectsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app),
                        _ => CreateManualEffectInput(tempParam, () => { 
                            effectPart = tempParam.Value; 
                            param.Value = tempParam.Value;
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        })
                    };
                    
                    effectContainer.Child = newControl;
                }
            };

            // Initialize effect container for existing data
            System.Diagnostics.Debug.WriteLine($"=== INITIALIZATION START ===");
            System.Diagnostics.Debug.WriteLine($"Mode Selector Selected Item: {modeSelector.SelectedItem}");
            
            if (modeSelector.SelectedItem != null)
            {
                var currentMode = (modeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                System.Diagnostics.Debug.WriteLine($"Current Mode: '{currentMode}'");
                System.Diagnostics.Debug.WriteLine($"Is Manual Mode: {isManualMode}");
                System.Diagnostics.Debug.WriteLine($"Effect Part: '{effectPart}'");
                System.Diagnostics.Debug.WriteLine($"Effect Container Visible: {effectContainer.IsVisible}");
                
                if (isManualMode)
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Manual mode");
                    // Default manual mode
                    var tempParam = new Argument("temp", Argument.TypeString, false) { Value = param.Value };
                    effectContainer.Child = CreateManualEffectInput(tempParam, () => { 
                        effectPart = tempParam.Value; 
                        param.Value = tempParam.Value;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    });
                    System.Diagnostics.Debug.WriteLine($"INITIALIZED: Manual text input created");
                }
                else if (currentMode == "colors" && !string.IsNullOrEmpty(effectPart))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Colors mode");
                    
                    // CRITICAL FIX: For colors mode, always ensure container visibility during initialization
                    if (!effectContainer.IsVisible)
                    {
                        System.Diagnostics.Debug.WriteLine($"FIXING: Effect container was hidden by range validation - forcing visibility for colors initialization");
                        effectContainer.IsVisible = true;
                    }
                    
                    // For colors, create immediately (synchronous)
                    var tempParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                    effectContainer.Child = CreateColorEffectsDropdown(tempParam, () => { 
                        effectPart = tempParam.Value; 
                        UpdateParameterValue(); 
                    }, app);
                    System.Diagnostics.Debug.WriteLine($"INITIALIZED: Colors dropdown created successfully");
                }
                else if (currentMode == "presets" && !string.IsNullOrEmpty(effectPart))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Preset mode with effect part");
                    
                    // CRITICAL FIX: For presets mode, always ensure container visibility during initialization
                    if (!effectContainer.IsVisible)
                    {
                        System.Diagnostics.Debug.WriteLine($"FIXING: Effect container was hidden by range validation - forcing visibility for presets initialization");
                        effectContainer.IsVisible = true;
                    }
                    
                    // Show loading indicator initially
                    effectContainer.Child = new TextBlock 
                    { 
                        Text = "Loading presets...", 
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    System.Diagnostics.Debug.WriteLine($"LOADING: Showing 'Loading presets...' indicator");
                    
                    // For presets, initialize on UI thread with async population
                    System.Diagnostics.Debug.WriteLine($"UI THREAD: Starting preset initialization");
                    
                    var tempParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                    var extractedPreset = ExtractPresetFromEffectPart(effectPart);
                    
                    System.Diagnostics.Debug.WriteLine($"UI THREAD: ExtractPresetFromEffectPart('{effectPart}') = '{extractedPreset}'");
                    
                    // Create preset control on UI thread asynchronously
                    _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Starting async preset creation");
                            
                            // Create the preset control on UI thread
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Creating preset control");
                            var presetControl = await CreateWledPresetsDropdownWithState(
                                tempParam, 
                                () => { effectPart = tempParam.Value; UpdateParameterValue(); }, 
                                app, 
                                extractedPreset, 
                                selectedTo ?? 0, 
                                selectedFrom ?? 0
                            );
                            
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Preset control created successfully");
                            
                            // Set the control on UI thread
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Setting preset control to container");
                            // CRITICAL FIX: Re-ensure visibility before setting the control, in case something changed it in the meantime
                            effectContainer.IsVisible = true;
                            effectContainer.Child = presetControl;
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Preset control set to container");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD ERROR: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"UI THREAD STACK: {ex.StackTrace}");
                            
                            // Fallback to manual input on error
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                var fallbackParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                                effectContainer.Child = CreateManualEffectInput(fallbackParam, () => { 
                                    effectPart = fallbackParam.Value; 
                                    UpdateParameterValue();
                                });
                                System.Diagnostics.Debug.WriteLine($"UI THREAD: Fallback manual input created due to error");
                            });
                        }
                    });
                }
                else if (currentMode == "effects" && !string.IsNullOrEmpty(effectPart))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Effects mode");
                    
                    // CRITICAL FIX: For effects mode, always ensure container visibility during initialization
                    // The range validation might have hidden it, but we need to show it for effects initialization
                    if (!effectContainer.IsVisible)
                    {
                        System.Diagnostics.Debug.WriteLine($"FIXING: Effect container was hidden by range validation - forcing visibility for effects initialization");
                        effectContainer.IsVisible = true;
                    }
                    
                    // IMMEDIATE SYNCHRONOUS CREATION as fallback if async fails
                    System.Diagnostics.Debug.WriteLine($"IMMEDIATE: Creating synchronous effects control as initial content");
                    var immediateTempParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                    
                    // Create a simple immediate control that shows the parsed values
                    var immediatePanel = new StackPanel { Spacing = 5 };
                    immediatePanel.Children.Add(new TextBlock 
                    { 
                        Text = $"WLED Effect: {effectPart}", 
                        Foreground = Brushes.White,
                        FontSize = 12
                    });
                    immediatePanel.Children.Add(new TextBlock 
                    { 
                        Text = "Loading full WLED controls...", 
                        Foreground = Brushes.LightGray,
                        FontSize = 10
                    });
                    effectContainer.Child = immediatePanel;
                    System.Diagnostics.Debug.WriteLine($"IMMEDIATE: Set immediate panel showing effect info");
                    
                    // CRITICAL FIX: Call CreateWledEffectsDropdown on UI thread, not in background
                    System.Diagnostics.Debug.WriteLine($"UI THREAD: Starting async creation on UI thread");
                    _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Creating WLED effects control with effectPart='{effectPart}'");
                            var tempParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Created tempParam with value='{tempParam.Value}'");
                            
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Calling CreateWledEffectsDropdown");
                            var effectControl = await CreateWledEffectsDropdown(
                                tempParam, 
                                () => { 
                                    System.Diagnostics.Debug.WriteLine($"CALLBACK: effectPart updated from '{effectPart}' to '{tempParam.Value}'");
                                    effectPart = tempParam.Value; 
                                    UpdateParameterValue(); 
                                }, 
                                app
                            );
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: CreateWledEffectsDropdown completed successfully");
                            
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Setting effect control to container");
                            // CRITICAL FIX: Re-ensure visibility before setting the control, in case something changed it in the meantime
                            effectContainer.IsVisible = true;
                            effectContainer.Child = effectControl;
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: WLED Effects control set to container successfully");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD ERROR in effects creation: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"UI THREAD STACK: {ex.StackTrace}");
                            

                            // Fallback to manual input on error
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Creating fallback manual input due to error");
                            var fallbackParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                            effectContainer.Child = CreateManualEffectInput(fallbackParam, () => { 
                                effectPart = fallbackParam.Value; 
                                UpdateParameterValue();
                            });
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Fallback manual input created due to effects error");
                        }
                    });
                    System.Diagnostics.Debug.WriteLine($"UI THREAD: Async task scheduled for effects creation");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"INITIALIZING: No mode selected - default fallback");
                // Default fallback
                var tempParam = new Argument("temp", Argument.TypeString, false) { Value = param.Value };
                effectContainer.Child = CreateManualEffectInput(tempParam, () => { 
                    effectPart = tempParam.Value; 
                    param.Value = tempParam.Value;
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                });
            }

            System.Diagnostics.Debug.WriteLine($"=== INITIALIZATION COMPLETE ===");

            // Build the UI
            rangePanel.Children.Add(rangeLabel);
            rangePanel.Children.Add(fromDropdown);
            rangePanel.Children.Add(toLabel);
            rangePanel.Children.Add(toDropdown);

            mainPanel.Children.Add(modeSelector);
            mainPanel.Children.Add(rangePanel);
            mainPanel.Children.Add(effectContainer);

            return mainPanel;
        }

        /// <summary>
        /// Checks if a parameter is a score area effect parameter
        /// </summary>
        public static bool IsScoreAreaEffectParameter(Argument param)
        {
            // Check by argument name pattern (A1, A2, A3, etc.)
            if (param.Name.Length >= 2 && param.Name.StartsWith("A") && 
                int.TryParse(param.Name.Substring(1), out var areaNumber) && 
                areaNumber >= 1 && areaNumber <= 12)
            {
                return true;
            }

            // Check by human name containing "score_area"
            if (param.NameHuman != null && 
                param.NameHuman.Contains("score_area", StringComparison.OrdinalIgnoreCase) &&
                param.NameHuman.Contains("effects", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool IsPresetParameter(string value)
        {
            if (string.IsNullOrEmpty(value)) 
            {
                System.Diagnostics.Debug.WriteLine($"IsPresetParameter('{value}') = false (null/empty)");
                return false;
            }
            
            // Check for ps|1, ps|2, etc. format (with or without duration)
            var parts = value.Split('|');
            
            bool isPreset = parts.Length >= 2 && 
                           parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase) && 
                           int.TryParse(parts[1], out _);
            
            System.Diagnostics.Debug.WriteLine($"IsPresetParameter('{value}') = {isPreset}");
            System.Diagnostics.Debug.WriteLine($"  Parts: [{string.Join(", ", parts)}]");
            System.Diagnostics.Debug.WriteLine($"  Parts count: {parts.Length}");
            if (parts.Length >= 1) System.Diagnostics.Debug.WriteLine($"  First part: '{parts[0]}'");
            if (parts.Length >= 2) System.Diagnostics.Debug.WriteLine($"  Second part: '{parts[1]}' (is int: {int.TryParse(parts[1], out _)})");
            
            return isPreset;
        }

        private static string? ExtractPresetFromEffectPart(string? effectPart)
        {
            if (string.IsNullOrEmpty(effectPart)) return null;
            
            // Check if effectPart is a preset format (ps|1, ps|1|5, etc.);
            var parts = effectPart.Split('|');
            if (parts.Length >= 2 && 
                parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase) && 
                int.TryParse(parts[1], out _))
            {
                // Return just the preset part (ps|1)
                return $"ps|{parts[1]}";
            }
            
            return null;
        }

        private static Control CreateManualEffectInput(Argument param, Action? saveCallback = null)
        {
            var textBox = new TextBox
            {
                Text = param.Value ?? string.Empty,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = "Enter effect manually..."
            };

            var warningText = new TextBlock
            {
                Text = "⚠️ This argument is enabled but empty. It can cause issues when the extension starts. Clear it with the eraser if you do not need it.",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                IsVisible = false
            };

            void UpdateWarning()
            {
                var isEmpty = string.IsNullOrWhiteSpace(textBox.Text);
                warningText.IsVisible = isEmpty;
                textBox.BorderBrush = isEmpty
                    ? new SolidColorBrush(Color.FromRgb(220, 53, 69))
                    : new SolidColorBrush(Color.FromRgb(100, 100, 100));
                textBox.BorderThickness = isEmpty ? new Thickness(2) : new Thickness(1);
            }

            UpdateWarning();

            textBox.TextChanged += (s, e) =>
            {
                param.Value = textBox.Text;
                param.IsValueChanged = true;
                saveCallback?.Invoke();
                UpdateWarning();
            };

            var container = new StackPanel { Spacing = 4 };
            container.Children.Add(textBox);
            container.Children.Add(warningText);
            return container;
        }

        private static async Task<Control> CreateWledEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            // Create a main panel to hold all effect controls
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Create effect selection panel
            var effectPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var effectDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Loading WLED effects...",
                MinWidth = 180,
                IsTextSearchEnabled = true, // Enable text search for better keyboard navigation
                MaxDropDownHeight = 300 // Limit dropdown height for better usability
            };

            // Add tooltip with keyboard navigation help
            ToolTip.SetTip(effectDropdown, "Select a WLED effect for score area\nType letters to jump to effects (e.g. 'f' for fire)\nUse arrow keys to navigate");

            var refreshButton = new Button
            {
                Content = "🔄",
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            var testButton = new Button
            {
                Content = "▶️",
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            var stopButton = new Button
            {
                Content = "■",
                Background = new SolidColorBrush(Color.FromRgb(150, 0, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            ToolTip.SetTip(refreshButton, "Refresh effects from WLED controller");
            ToolTip.SetTip(testButton, "Test selected effect on WLED controller");
            ToolTip.SetTip(stopButton, "Stop effects on WLED controller");

            // Create palette selection panel
            var palettePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var paletteLabel = new TextBlock
            {
                Text = "Palette:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 50
            };

            var paletteDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Loading palettes...",
                MinWidth = 150
            };

            // Create speed and intensity controls
            var speedPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var speedLabel = new TextBlock
            {
                Text = "Speed:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 50
            };

            var speedSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            };

            var speedValue = new TextBlock
            {
                Text = "128",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 30
            };

            var intensityPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var intensityLabel = new TextBlock
            {
                Text = "Intensity:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 50
            };

            var intensitySlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                Width = 120,
                VerticalAlignment = VerticalAlignment.Center
            };

            var intensityValue = new TextBlock
            {
                Text = "128",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 30
            };

            // Parse current value to extract effect, palette, speed, intensity
            // Expected format: {effect-name/ID}|s{speed}|i{intensity}|p{palette-ID} (new format)
            // or {effect-name}|{palette}|{speed}|{intensity} (old format for backward compatibility)
            string? selectedEffect = null;
            string? selectedPalette = null;
            int selectedSpeed = 128;
            int selectedIntensity = 128;

            System.Diagnostics.Debug.WriteLine($"=== WLED EFFECTS PARSING START ===");
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");

            if (!string.IsNullOrEmpty(param.Value))
            {
                var parts = param.Value.Split('|');
                System.Diagnostics.Debug.WriteLine($"Split into {parts.Length} parts: [{string.Join(", ", parts)}]");
                
                if (parts.Length > 0) 
                {
                    selectedEffect = parts[0];
                    System.Diagnostics.Debug.WriteLine($"Selected Effect: '{selectedEffect}'");
                }
                
                // First try new format with prefixes
                bool foundNewFormat = false;
                for (int i = 1; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;
                    
                    if (part.StartsWith("s") && int.TryParse(part.Substring(1), out var speed))
                    {
                        selectedSpeed = Math.Max(1, Math.Min(255, speed));
                        foundNewFormat = true;
                        System.Diagnostics.Debug.WriteLine($"Found Speed (new format): {selectedSpeed}");
                    }
                    else if (part.StartsWith("i") && int.TryParse(part.Substring(1), out var intensity))
                    {
                        selectedIntensity = Math.Max(1, Math.Min(255, intensity));
                        foundNewFormat = true;
                        System.Diagnostics.Debug.WriteLine($"Found Intensity (new format): {selectedIntensity}");
                    }
                    else if (part.StartsWith("p"))
                    {
                        var paletteValue = part.Substring(1);
                        selectedPalette = paletteValue;
                        foundNewFormat = true;
                        System.Diagnostics.Debug.WriteLine($"Found Palette (new format): '{selectedPalette}'");
                    }
                }
                
                // If no new format found, try old format for backward compatibility
                if (!foundNewFormat && parts.Length > 1)
                {
                    System.Diagnostics.Debug.WriteLine($"Trying old format parsing...");
                    if (parts.Length > 1) 
                    {
                        selectedPalette = parts[1];
                        System.Diagnostics.Debug.WriteLine($"Found Palette (old format): '{selectedPalette}'");
                    }
                    if (parts.Length > 2 && int.TryParse(parts[2], out var oldSpeed)) 
                    {
                        selectedSpeed = Math.Max(1, Math.Min(255, oldSpeed));
                        System.Diagnostics.Debug.WriteLine($"Found Speed (old format): {selectedSpeed}");
                    }
                    if (parts.Length > 3 && int.TryParse(parts[3], out var oldIntensity)) 
                    {
                        selectedIntensity = Math.Max(1, Math.Min(255, oldIntensity));
                        System.Diagnostics.Debug.WriteLine($"Found Intensity (old format): {selectedIntensity}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Final parsed values:");
                System.Diagnostics.Debug.WriteLine($"  Effect: '{selectedEffect}'");
                System.Diagnostics.Debug.WriteLine($"  Palette: '{selectedPalette}'");
                System.Diagnostics.Debug.WriteLine($"  Speed: {selectedSpeed}");
                System.Diagnostics.Debug.WriteLine($"  Intensity: {selectedIntensity}");
                System.Diagnostics.Debug.WriteLine($"  Found New Format: {foundNewFormat}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Parameter value is empty or null");
            }

            // Set initial values
            System.Diagnostics.Debug.WriteLine($"=== SETTING INITIAL SLIDER VALUES ===");
            System.Diagnostics.Debug.WriteLine($"Setting Speed to: {selectedSpeed}");
            System.Diagnostics.Debug.WriteLine($"Setting Intensity to: {selectedIntensity}");
            
            speedSlider.Value = selectedSpeed;
            speedValue.Text = selectedSpeed.ToString();
            intensitySlider.Value = selectedIntensity;
            intensityValue.Text = selectedIntensity.ToString();
            
            System.Diagnostics.Debug.WriteLine($"Sliders initialized:");
            System.Diagnostics.Debug.WriteLine($"  Speed Slider Value: {speedSlider.Value}");
            System.Diagnostics.Debug.WriteLine($"  Speed Text Value: {speedValue.Text}");
            System.Diagnostics.Debug.WriteLine($"  Intensity Slider Value: {intensitySlider.Value}");
            System.Diagnostics.Debug.WriteLine($"  Intensity Text Value: {intensityValue.Text}");

            // Flag to prevent recursive updates
            bool isUpdating = false;
            bool isInitializing = true;

            // Function to update parameter value
            void UpdateParameterValue()
            {
                if (isUpdating || isInitializing) return;

                var effect = (effectDropdown.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var palette = (paletteDropdown.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                var speed = (int)Math.Round(speedSlider.Value);
                var intensity = (int)Math.Round(intensitySlider.Value);

                if (!string.IsNullOrEmpty(effect))
                {
                    isUpdating = true;
                    try
                    {
                        var parts = new List<string> { effect };
                        
                        // Add speed component with prefix
                        parts.Add($"s{speed}");
                        
                        // Add intensity component with prefix
                        parts.Add($"i{intensity}");
                        
                        // Add palette component if selected and not empty
                        if (!string.IsNullOrEmpty(palette))
                        {
                            // For palette, try to get palette index if it's a name
                            if (int.TryParse(palette, out _))
                            {
                                // Already an index
                                parts.Add($"p{palette}");
                            }
                            else
                            {
                                // It's a name, try to find index
                                // For simplicity, just use the name for now
                                parts.Add($"p{palette}");
                            }
                        }
                        
                        param.Value = string.Join("|", parts);
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                }
            }

            // Function to populate effects
            async Task PopulateEffects()
            {
                System.Diagnostics.Debug.WriteLine($"=== POPULATING EFFECTS START ===");
                System.Diagnostics.Debug.WriteLine($"Looking for effect to select: '{selectedEffect}'");
                
                effectDropdown.PlaceholderText = "Loading WLED effects...";
                effectDropdown.Items.Clear();
                
                ComboBoxItem? effectToSelect = null;
                
                if (app != null)
                {
                    var (effects, source, isLive) = await WledApi.GetEffectsWithFallbackAsync(app);
                    
                    System.Diagnostics.Debug.WriteLine($"Retrieved {effects.Count} effects from {source} (isLive: {isLive})");
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"─── Live from {source} ───" : "─── Fallback Effects ───";
                    
                    var dynamicHeader = new ComboBoxItem
                    {
                        Content = headerText,
                        Foreground = new SolidColorBrush(headerColor),
                        IsEnabled = false,
                        FontWeight = FontWeight.Bold
                    };
                    effectDropdown.Items.Add(dynamicHeader);

                    // Add effects
                    foreach (var effect in effects)
                    {
                        var effectItem = new ComboBoxItem
                        {
                            Content = effect,
                            Tag = effect,
                            Foreground = Brushes.White
                        };
                        effectDropdown.Items.Add(effectItem);
                        
                        // Pre-select if this matches current value
                        if (selectedEffect == effect)
                        {
                            effectToSelect = effectItem;
                            System.Diagnostics.Debug.WriteLine($"MATCH FOUND: Will select effect '{effect}'");
                        }
                    }

                    effectDropdown.PlaceholderText = isLive ? 
                        "Select WLED effect (live data)..." : 
                        "Select WLED effect (fallback data)...";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No app provided, using fallback effects");
                    
                    // Just use fallback if no app provided
                    var fallbackEffects = WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Retrieved {fallbackEffects.Count} fallback effects");
                    
                    foreach (var effect in fallbackEffects)
                    {
                        var effectItem = new ComboBoxItem
                        {
                            Content = effect,
                            Tag = effect,
                            Foreground = Brushes.White
                        };
                        effectDropdown.Items.Add(effectItem);
                        
                        if (selectedEffect == effect)
                        {
                            effectToSelect = effectItem;
                            System.Diagnostics.Debug.WriteLine($"FALLBACK MATCH FOUND: Will select effect '{effect}'");
                        }
                    }
                    effectDropdown.PlaceholderText = "Select WLED effect...";
                }
                
                if (effectToSelect != null)
                {
                    effectDropdown.SelectedItem = effectToSelect;
                    System.Diagnostics.Debug.WriteLine($"EFFECT SELECTED: '{selectedEffect}' was set as selected item");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NO EFFECT MATCH: '{selectedEffect}' was not found in available effects");
                    
                    // Debug: List all available effects
                    var allEffects = effectDropdown.Items.OfType<ComboBoxItem>()
                        .Where(item => item.IsEnabled && item.Tag != null)
                        .Select(item => item.Tag?.ToString())
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"Available effects: [{string.Join(", ", allEffects)}]");
                }
                
                System.Diagnostics.Debug.WriteLine($"=== POPULATING EFFECTS COMPLETE ===");
            }

            // Function to populate palettes
            async Task PopulatePalettes()
            {
                System.Diagnostics.Debug.WriteLine($"=== POPULATING PALETTES START ===");
                System.Diagnostics.Debug.WriteLine($"Looking for palette to select: '{selectedPalette}'");
                
                paletteDropdown.PlaceholderText = "Loading palettes...";
                paletteDropdown.Items.Clear();
                
                ComboBoxItem? paletteToSelect = null;
                
                // Add "None" option
                var noneItem = new ComboBoxItem
                {
                    Content = "None",
                    Tag = "",
                    Foreground = Brushes.White
                };
                paletteDropdown.Items.Add(noneItem);
                
                if (string.IsNullOrEmpty(selectedPalette))
                {
                    paletteToSelect = noneItem;
                    System.Diagnostics.Debug.WriteLine($"NONE SELECTED: Empty palette, will select 'None'");
                }
                
                if (app != null)
                {
                    var (palettes, source, isLive) = await WledApi.GetPalettesWithFallbackAsync(app);
                    
                    System.Diagnostics.Debug.WriteLine($"Retrieved {palettes.Count} palettes from {source} (isLive: {isLive})");
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"─── Live from {source} ───" : "─── Fallback Palettes ───";
                    
                    var dynamicHeader = new ComboBoxItem
                    {
                        Content = headerText,
                        Foreground = new SolidColorBrush(headerColor),
                        IsEnabled = false,
                        FontWeight = FontWeight.Bold
                    };
                    paletteDropdown.Items.Add(dynamicHeader);

                    // Add palettes with their index as value (for p{palette-ID} format)
                    for (int i = 0; i < palettes.Count; i++)
                    {
                        var palette = palettes[i];
                        var paletteItem = new ComboBoxItem
                        {
                            Content = palette,
                            Tag = i.ToString(), // Use index as the palette ID
                            Foreground = Brushes.White
                        };
                        paletteDropdown.Items.Add(paletteItem);
                        
                        // Pre-select if this matches current value (by index or name)
                        if (selectedPalette == i.ToString() || selectedPalette == palette)
                        {
                            paletteToSelect = paletteItem;
                            System.Diagnostics.Debug.WriteLine($"PALETTE MATCH FOUND: Will select palette '{palette}' (index: {i}, selectedPalette: '{selectedPalette}')");
                        }
                    }

                    paletteDropdown.PlaceholderText = isLive ? 
                        "Select palette (live data)..." : 
                        "Select palette (fallback data)...";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No app provided, using fallback palettes");
                    
                    // Just use fallback if no app provided
                    for (int i = 0; i < WledApi.FallbackPalettes.Count; i++)
                    {
                        var palette = WledApi.FallbackPalettes[i];
                        var paletteItem = new ComboBoxItem
                        {
                            Content = palette,
                            Tag = i.ToString(), // Use index as the palette ID
                            Foreground = Brushes.White
                        };
                        paletteDropdown.Items.Add(paletteItem);
                        
                        if (selectedPalette == i.ToString() || selectedPalette == palette)
                        {
                            paletteToSelect = paletteItem;
                            System.Diagnostics.Debug.WriteLine($"FALLBACK PALETTE MATCH: Will select palette '{palette}' (index: {i})");
                        }
                    }
                    paletteDropdown.PlaceholderText = "Select palette...";
                }
                
                if (paletteToSelect != null)
                {
                    paletteDropdown.SelectedItem = paletteToSelect;
                    System.Diagnostics.Debug.WriteLine($"PALETTE SELECTED: '{paletteToSelect.Content}' was set as selected item");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"NO PALETTE MATCH: '{selectedPalette}' was not found in available palettes");
                    
                    // Debug: List all available palettes
                    var allPalettes = paletteDropdown.Items.OfType<ComboBoxItem>()
                        .Where(item => item.IsEnabled && item.Tag != null)
                        .Select(item => $"{item.Content} (tag: {item.Tag})")
                        .ToList();
                    System.Diagnostics.Debug.WriteLine($"Available palettes: [{string.Join(", ", allPalettes)}]");
                }
                
                System.Diagnostics.Debug.WriteLine($"=== POPULATING PALETTES COMPLETE ===");
            }

            // Initial population
            System.Diagnostics.Debug.WriteLine($"=== STARTING INITIAL POPULATION ===");
            await PopulateEffects();
            await PopulatePalettes();
            
            // Re-set slider values after population (in case they got reset)
            System.Diagnostics.Debug.WriteLine($"=== RE-SETTING SLIDER VALUES AFTER POPULATION ===");
            speedSlider.Value = selectedSpeed;
            speedValue.Text = selectedSpeed.ToString();
            intensitySlider.Value = selectedIntensity;
            intensityValue.Text = selectedIntensity.ToString();
            
            System.Diagnostics.Debug.WriteLine($"Final slider values:");
            System.Diagnostics.Debug.WriteLine($"  Speed: {speedSlider.Value} (text: {speedValue.Text})");
            System.Diagnostics.Debug.WriteLine($"  Intensity: {intensitySlider.Value} (text: {intensityValue.Text})");
            
            // Allow updates after initial population
            System.Diagnostics.Debug.WriteLine($"=== INITIAL POPULATION COMPLETE - ENABLING UPDATES ===");
            isInitializing = false;

            // Event handlers
            refreshButton.Click += async (s, e) =>
            {
                refreshButton.IsEnabled = false;
                refreshButton.Content = "⏳";
                try
                {
                    isInitializing = true; // Prevent updates during refresh
                    await PopulateEffects();
                    await PopulatePalettes();
                    isInitializing = false;
                }
                finally
                {
                    refreshButton.Content = "🔄";
                    refreshButton.IsEnabled = true;
                }
            };

            testButton.Click += async (s, e) =>
            {
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Test button clicked but app is null");
                    return;
                }

                if (effectDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string effect && 
                    !string.IsNullOrEmpty(effect))
                {
                    System.Diagnostics.Debug.WriteLine($"Testing WLED effect: {effect}");
                    
                    // Disable button to prevent multiple clicks
                    testButton.IsEnabled = false;
                    var originalContent = testButton.Content;
                    testButton.Content = "⏳";
                    
                    try
                    {
                        var palette = (paletteDropdown.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                        var speed = (int)Math.Round(speedSlider.Value);
                        var intensity = (int)Math.Round(intensitySlider.Value);
                        
                        // For testing, convert palette ID back to name if needed
                        string? paletteName = null;
                        if (!string.IsNullOrEmpty(palette) && int.TryParse(palette, out var paletteIndex))
                        {
                            var (palettes, _, _) = await WledApi.GetPalettesWithFallbackAsync(app);
                            if (paletteIndex < palettes.Count)
                            {
                                paletteName = palettes[paletteIndex];
                            }
                        }
                        else if (!string.IsNullOrEmpty(palette))
                        {
                            // It's already a name
                            paletteName = palette;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Sending test effect: {effect}, palette: {paletteName}, speed: {speed}, intensity: {intensity}");
                        
                        var success = await WledApi.TestEffectAsync(app, effect, 
                            paletteName, speed, intensity);
                            
                        if (success)
                        {
                            testButton.Content = "✅";
                            System.Diagnostics.Debug.WriteLine("Effect test successful");
                        }
                        else
                        {
                            testButton.Content = "❌";
                            System.Diagnostics.Debug.WriteLine("Effect test failed");
                        }
                        
                        // Reset button after delay
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        testButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine($"Error testing effect: {ex.Message}");
                        await Task.Delay(1500);
                    }
                    finally
                    {
                        testButton.Content = originalContent;
                        testButton.IsEnabled = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Test button clicked but no effect selected or effect is empty");
                }
            };

            stopButton.Click += async (s, e) =>
            {
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Stop button clicked but app is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Stopping WLED effects");
                
                // Disable button to prevent multiple clicks
                stopButton.IsEnabled = false;
                var originalContent = stopButton.Content;
                stopButton.Content = "⏳";
                
                try
                {
                    var success = await WledApi.StopEffectsAsync(app);
                    if (success)
                    {
                        stopButton.Content = "✅";
                        System.Diagnostics.Debug.WriteLine("Effects stopped successfully");
                    }
                    else
                    {
                        stopButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine("Failed to stop effects");
                    }
                    
                    // Reset button after delay
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    stopButton.Content = "❌";
                    System.Diagnostics.Debug.WriteLine($"Error stopping effects: {ex.Message}");
                    await Task.Delay(1500);
                }
                finally
                {
                    stopButton.Content = originalContent;
                    stopButton.IsEnabled = true;
                }
            };

            effectDropdown.SelectionChanged += (s, e) =>
            {
                if (!isUpdating && !isInitializing && effectDropdown.SelectedItem is ComboBoxItem { Tag: string })
                {
                    UpdateParameterValue();
                }
            };

            paletteDropdown.SelectionChanged += (s, e) =>
            {
                if (!isUpdating && !isInitializing)
                {
                    UpdateParameterValue();
                }
            };

            speedSlider.ValueChanged += (s, e) =>
            {
                var value = (int)Math.Round(speedSlider.Value);
                speedValue.Text = value.ToString();
                if (!isUpdating && !isInitializing)
                {
                    UpdateParameterValue();
                }
            };

            intensitySlider.ValueChanged += (s, e) =>
            {
                var value = (int)Math.Round(intensitySlider.Value);
                intensityValue.Text = value.ToString();
                if (!isUpdating && !isInitializing)
                {
                    UpdateParameterValue();
                }
            };

            // Build the UI
            effectPanel.Children.Add(effectDropdown);
            effectPanel.Children.Add(refreshButton);
            effectPanel.Children.Add(testButton);
            effectPanel.Children.Add(stopButton);

            palettePanel.Children.Add(paletteLabel);
            palettePanel.Children.Add(paletteDropdown);

            speedPanel.Children.Add(speedLabel);
            speedPanel.Children.Add(speedSlider);
            speedPanel.Children.Add(speedValue);

            intensityPanel.Children.Add(intensityLabel);
            intensityPanel.Children.Add(intensitySlider);
            intensityPanel.Children.Add(intensityValue);

            mainPanel.Children.Add(effectPanel);
            mainPanel.Children.Add(palettePanel);
            mainPanel.Children.Add(speedPanel);
            mainPanel.Children.Add(intensityPanel);

            return mainPanel;
        }

        private static async Task<Control> CreateWledPresetsDropdownWithState(Argument param, Action? saveCallback = null, AppBase? app = null, string? initialSelectedPreset = null, int initialToRange = 0, int initialFromRange = 0)
        {
            // Create a main panel to hold preset selection and duration
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Create a panel to hold dropdown and refresh button
            var presetPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var presetDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Loading WLED presets...",
                MinWidth = 180,
                IsTextSearchEnabled = true, // Enable text search for better keyboard navigation
                MaxDropDownHeight = 300 // Limit dropdown height for better usability
            };

            // Add tooltip with keyboard navigation help
            ToolTip.SetTip(presetDropdown, "Select a preset for score area\nType letters to jump to presets (e.g. '1' for ps|1)\nUse arrow keys to navigate");

            var refreshButton = new Button
            {
                Content = "🔄",
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            var testButton = new Button
            {
                Content = "▶️",
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            var stopButton = new Button
            {
                Content = "■",
                Background = new SolidColorBrush(Color.FromRgb(150, 0, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            ToolTip.SetTip(refreshButton, "Refresh presets from WLED controller");
            ToolTip.SetTip(testButton, "Test selected preset on WLED controller");
            ToolTip.SetTip(stopButton, "Stop effects on WLED controller");

            // Duration selection panel
            var durationPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var durationLabel = new TextBlock
            {
                Text = "Duration:",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 60
            };

            var durationUpDown = new NumericUpDown
            {
                Value = 0, // Default 0 seconds
                Minimum = 0m,
                Maximum = 60m,
                Increment = 1m,
                FormatString = "F0", // Show whole numbers
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Width = 120,
                MinWidth = 120
            };

            var secondsLabel = new TextBlock
            {
                Text = "sec",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Parse current value to determine what should be selected
            // Try to parse format: "ps|1|duration" or just "ps|1"
            string? targetPreset = initialSelectedPreset; // Use initialSelectedPreset first
            decimal targetDuration = 0m;
            
            if (!string.IsNullOrEmpty(param.Value))
            {
                var parts = param.Value.Split('|');
                if (parts.Length >= 2 && parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase))
                {
                    // Only override targetPreset if initialSelectedPreset wasn't provided
                    if (string.IsNullOrEmpty(targetPreset))
                    {
                        targetPreset = $"ps|{parts[1]}"; // Reconstruct ps|number format
                    }
                    
                    if (parts.Length > 2 && decimal.TryParse(parts[2], System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var parsedDuration))
                    {
                        targetDuration = Math.Max(0m, Math.Min(60m, parsedDuration));
                    }
                }
            }
            
            // Set the duration immediately to preserve state
            durationUpDown.Value = targetDuration;

            // Flag to prevent recursive updates
            bool isUpdating = false;
            bool isInitializing = true; // Flag to prevent updates during initialization

            // Function to update parameter value
            void UpdateParameterValue()
            {
                if (isUpdating || isInitializing) return;
                
                if (presetDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string preset &&
                    durationUpDown.Value.HasValue)
                {
                    isUpdating = true;
                    try
                    {
                        var durationValue = Math.Round(durationUpDown.Value.Value, 0);
                        
                        if (durationValue == 0)
                        {
                            param.Value = preset;
                        }
                        else
                        {
                            param.Value = $"{preset}|{durationValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                        }
                        
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                }
            }

            // Function to populate presets
            async Task PopulatePresets()
            {
                presetDropdown.PlaceholderText = "Loading WLED presets...";
                presetDropdown.Items.Clear();
                
                ComboBoxItem? itemToSelect = null; // Track which item should be selected
              
                if (app != null)
                {
                    var (presets, source, isLive) = await WledApi.GetPresetsWithFallbackAsync(app);
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"─── Live from {source} ───" : "─── Fallback Presets ───";
                    
                    var dynamicHeader = new ComboBoxItem
                    {
                        Content = headerText,
                        Foreground = new SolidColorBrush(headerColor),
                        IsEnabled = false,
                        FontWeight = FontWeight.Bold
                    };
                    presetDropdown.Items.Add(dynamicHeader);

                    // Add presets using ps|1, ps|2, etc. format
                    foreach (var preset in presets.OrderBy(p => p.Key))
                    {
                        var presetDisplayName = isLive ? 
                            $"Preset {preset.Key} - {preset.Value}" : 
                            preset.Value;
                        var presetValue = $"ps|{preset.Key}";
                        
                        var presetItem = new ComboBoxItem
                        {
                            Content = presetDisplayName,
                            Tag = presetValue,
                            Foreground = Brushes.White
                        };
                        presetDropdown.Items.Add(presetItem);
                        
                        // Check if this should be selected
                        if (targetPreset == presetValue)
                        {
                            itemToSelect = presetItem;
                            System.Diagnostics.Debug.WriteLine($"MATCH FOUND: Will select preset '{presetValue}'");
                        }
                    }

                    presetDropdown.PlaceholderText = isLive ? 
                        "Select preset (live data)..." : 
                        "Select preset (fallback data)...";
                }
                else
                {
                    for (int i = 1; i <= WledApi.FallbackPresets.Count; i++)
                    {
                        var preset = WledApi.FallbackPresets[i - 1];
                        var presetValue = $"ps|{i}";
                        
                        var presetItem = new ComboBoxItem
                        {
                            Content = preset,
                            Tag = presetValue,
                            Foreground = Brushes.White
                        };
                        presetDropdown.Items.Add(presetItem);
                        
                        // Check if this should be selected
                        if (targetPreset == presetValue)
                        {
                            itemToSelect = presetItem;
                            System.Diagnostics.Debug.WriteLine($"FALLBACK MATCH FOUND: Will select preset '{presetValue}'");
                        }
                    }
                    presetDropdown.PlaceholderText = "Select preset...";
                }
                
                // Set selection AFTER all items have been added
                if (itemToSelect != null)
                {
                    presetDropdown.SelectedItem = itemToSelect;
                }
                
                // Allow updates after initial population and selection
                isInitializing = false;
            }

            // Initial population
            await PopulatePresets();

            // Event handlers
            refreshButton.Click += async (s, e) =>
            {
                refreshButton.IsEnabled = false;
                refreshButton.Content = "⏳";
                try
                {
                    isInitializing = true; // Prevent updates during refresh
                    await PopulatePresets();
                }
                finally
                {
                    refreshButton.Content = "🔄";
                    refreshButton.IsEnabled = true;
                }
            };

            // Event handlers - FIXED: Properly capture references and handle async operations
            testButton.Click += async (s, e) =>
            {
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Test button clicked but app is null");
                    return;
                }

                if (presetDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string presetTag && 
                    !string.IsNullOrEmpty(presetTag))
                {
                    // Extract preset ID from "ps|X" format
                    var parts = presetTag.Split('|');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var presetId))
                    {
                        System.Diagnostics.Debug.WriteLine($"Testing WLED preset: {presetId}");
                        
                        // Disable button to prevent multiple clicks
                        testButton.IsEnabled = false;
                        var originalContent = testButton.Content;
                        testButton.Content = "⏳";
                        
                        try
                        {
                            var success = await WledApi.TestPresetAsync(app, presetId);
                            
                            if (success)
                            {
                                testButton.Content = "✅";
                                System.Diagnostics.Debug.WriteLine("Preset test successful");
                            }
                            else
                            {
                                testButton.Content = "❌";
                                System.Diagnostics.Debug.WriteLine("Preset test failed");
                            }
                            
                            // Reset button after delay
                            await Task.Delay(1500);
                        }
                        catch (Exception ex)
                        {
                            testButton.Content = "❌";
                            System.Diagnostics.Debug.WriteLine($"Error testing preset: {ex.Message}");
                            await Task.Delay(1500);
                        }
                        finally
                        {
                            testButton.Content = originalContent;
                            testButton.IsEnabled = true;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid preset tag format: {presetTag}/ Parts: {string.Join(", ", parts)}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Test button clicked but no preset selected");
                }
            };

            stopButton.Click += async (s, e) =>
            {
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Stop button clicked but app is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Stopping WLED effects");
                
                // Disable button to prevent multiple clicks
                stopButton.IsEnabled = false;
                var originalContent = stopButton.Content;
                stopButton.Content = "⏳";
                
                try
                {
                    var success = await WledApi.StopEffectsAsync(app);
                    
                    if (success)
                    {
                        stopButton.Content = "✅";
                        System.Diagnostics.Debug.WriteLine("Effects stopped successfully");
                    }
                    else
                    {
                        stopButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine("Failed to stop effects");
                    }
                    
                    // Reset button after delay
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    stopButton.Content = "❌";
                    System.Diagnostics.Debug.WriteLine($"Error stopping effects: {ex.Message}");
                    await Task.Delay(1500);
                }
                finally
                {
                    stopButton.Content = originalContent;
                    stopButton.IsEnabled = true;
                }
            };

            presetDropdown.SelectionChanged += (s, e) =>
            {
                if (!isUpdating && !isInitializing && presetDropdown.SelectedItem is ComboBoxItem { Tag: string })
                {
                    UpdateParameterValue();
                }
            };

            durationUpDown.ValueChanged += (s, e) =>
            {
                if (!isUpdating && !isInitializing && durationUpDown.Value.HasValue)
                {
                    UpdateParameterValue();
                }
            };

            // Build the UI
            presetPanel.Children.Add(presetDropdown);
            presetPanel.Children.Add(refreshButton);
            presetPanel.Children.Add(testButton);
            presetPanel.Children.Add(stopButton);

            durationPanel.Children.Add(durationLabel);
            durationPanel.Children.Add(durationUpDown);
            durationPanel.Children.Add(secondsLabel);

            mainPanel.Children.Add(presetPanel);
            mainPanel.Children.Add(durationPanel);

            return mainPanel;
        }

        private static Control CreateColorEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var colorDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select color effect...",
                MinWidth = 200,
                MaxWidth = 280, // Set consistent maximum width
                Width = 240, // Set fixed width for consistency
                IsTextSearchEnabled = true, // Enable text search for better keyboard navigation
                MaxDropDownHeight = 300 // Limit dropdown height for better usability
            };

            // Add tooltip with keyboard navigation help
            ToolTip.SetTip(colorDropdown, "Select a color effect for score area\nType letters to jump to colors (e.g. 'r' for red)\nUse arrow keys to navigate");

            var testButton = new Button
            {
                Content = "▶️",
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            var stopButton = new Button
            {
                Content = "■",
                Background = new SolidColorBrush(Color.FromRgb(150, 0, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Width = 30,
                Height = 30,
                VerticalAlignment = VerticalAlignment.Top
            };

            ToolTip.SetTip(testButton, "Test selected color on WLED controller");
            ToolTip.SetTip(stopButton, "Stop effects on WLED controller");

            // Determine the current color value (remove "solid|" prefix if present)
            string currentColorValue = param.Value ?? "";
            System.Diagnostics.Debug.WriteLine($"=== SCORE AREA COLOR DROPDOWN INIT ===");
            System.Diagnostics.Debug.WriteLine($"App: {app?.Name ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: {param.Name}");
            System.Diagnostics.Debug.WriteLine($"Original Parameter Value: '{param.Value}'");
            
            // Check for parameter-specific default color first
            string defaultColorForParam = NewSettingsContentProvider.DEFAULT_WLED_SCORE_AREA_COLOR; // fallback
            if (NewSettingsContentProvider.ParameterColorDefaults.TryGetValue(param.Name, out var specificDefault))
            {
                defaultColorForParam = specificDefault;
                System.Diagnostics.Debug.WriteLine($"Found specific default color for score area '{param.Name}': '{defaultColorForParam}'");
            }
            
            // Handle default placeholder values - set default color if no real value is configured
            if (string.IsNullOrEmpty(currentColorValue) || currentColorValue == "change to activate")
            {
                currentColorValue = defaultColorForParam; // Use parameter-specific default
                System.Diagnostics.Debug.WriteLine($"Using parameter-specific default color for score area: '{currentColorValue}'");
            }
            else if (currentColorValue.StartsWith("solid|", StringComparison.OrdinalIgnoreCase))
            {
                currentColorValue = currentColorValue.Substring(6);
                System.Diagnostics.Debug.WriteLine($"Removed 'solid|' prefix, new value: '{currentColorValue}'");
            }

            System.Diagnostics.Debug.WriteLine($"Final Current Color Value: '{currentColorValue}'");

            // Flag to prevent updates during initialization
            bool isInitializing = true;

            // Sort colors alphabetically for better navigation
            var sortedColors = NewSettingsContentProvider.ColorEffects.OrderBy(c => c).ToList();

            // Add keyboard navigation enhancement
            var searchBuffer = "";
            var lastSearchTime = DateTime.MinValue;
            
            // Enhanced keyboard handler for better search functionality
            colorDropdown.KeyDown += (s, e) =>
            {
                var currentTime = DateTime.Now;
                var key = e.Key.ToString();
                
                // Reset search buffer if more than 1 second has passed
                if ((currentTime - lastSearchTime).TotalMilliseconds > 1000)
                {
                    searchBuffer = "";
                }
                lastSearchTime = currentTime;
                
                // Only handle letter/number keys for search
                if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
                {
                    searchBuffer += key.ToLower();
                    
                    // Find first color that starts with search buffer
                    var matchingColor = sortedColors.FirstOrDefault(color => 
                        color.StartsWith(searchBuffer, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingColor != null)
                    {
                        var matchingItem = colorDropdown.Items.OfType<ComboBoxItem>()
                            .FirstOrDefault(item => item.Tag?.ToString() == matchingColor);
                        if (matchingItem != null)
                        {
                            colorDropdown.SelectedItem = matchingItem;
                            System.Diagnostics.Debug.WriteLine($"Score area keyboard search: '{searchBuffer}' -> selected '{matchingColor}'");
                        }
                    }
                    
                    e.Handled = true; // Prevent default behavior
                }
            };

            // Function to save the current selected color to the parameter
            Action saveCurrentSelection = () =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect)
                {
                    // CRITICAL FIX: Always store color effects with "solid|" prefix for score areas
                    param.Value = $"solid|{colorEffect}";
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                    
                    System.Diagnostics.Debug.WriteLine($"Updated score area color effect parameter with solid prefix: {param.Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No color selected or invalid selection for score area");
                }
            };

            // Populate color effects
            ComboBoxItem? selectedColorItem = null;
            System.Diagnostics.Debug.WriteLine($"Available colors count: {sortedColors.Count}");
            
            foreach (var colorEffect in sortedColors)
            {
                var colorItem = new ComboBoxItem
                {
                    Content = colorEffect,
                    Tag = colorEffect,
                    Foreground = Brushes.White
                };
                colorDropdown.Items.Add(colorItem);
                
                // Pre-select if this matches current value (compare just the color name)
                if (string.Equals(currentColorValue, colorEffect, StringComparison.OrdinalIgnoreCase))
                {
                    selectedColorItem = colorItem;
                    System.Diagnostics.Debug.WriteLine($"MATCH FOUND: Will select color '{colorEffect}' (case-insensitive match)");
                }
            }

            // Set selected item after all items are added
            if (selectedColorItem != null)
            {
                colorDropdown.SelectedItem = selectedColorItem;
                System.Diagnostics.Debug.WriteLine($"SELECTED COLOR: '{selectedColorItem.Content}' was set as selected item");
            }
            else if (!string.IsNullOrEmpty(currentColorValue))
            {
                System.Diagnostics.Debug.WriteLine($"NO COLOR MATCH: '{currentColorValue}' was not found in available colors");
                // Try a partial match as fallback
                var partialMatch = sortedColors
                    .FirstOrDefault(color => color.Contains(currentColorValue, StringComparison.OrdinalIgnoreCase) ||
                                           currentColorValue.Contains(color, StringComparison.OrdinalIgnoreCase));
                if (partialMatch != null)
                {
                    var partialItem = colorDropdown.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(item => item.Tag?.ToString() == partialMatch);
                    if (partialItem != null)
                    {
                        colorDropdown.SelectedItem = partialItem;
                        System.Diagnostics.Debug.WriteLine($"PARTIAL MATCH FOUND: Selected '{partialMatch}' for value '{currentColorValue}'");
                    }
                }
                else
                {
                    // If no match found at all, select the parameter-specific default color
                    var defaultItem = colorDropdown.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(item => item.Tag?.ToString() == defaultColorForParam);
                    if (defaultItem != null)
                    {
                        colorDropdown.SelectedItem = defaultItem;
                        System.Diagnostics.Debug.WriteLine($"NO MATCH: Selected parameter-specific default score area color '{defaultColorForParam}'");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Current color value is empty - selecting parameter-specific default color '{defaultColorForParam}'");
                // Select parameter-specific default color for empty values
                var defaultItem = colorDropdown.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(item => item.Tag?.ToString() == defaultColorForParam);
                if (defaultItem != null)
                {
                    colorDropdown.SelectedItem = defaultItem;
                    System.Diagnostics.Debug.WriteLine($"EMPTY VALUE: Selected parameter-specific default score area color '{defaultColorForParam}'");
                }
            }

            // CRITICAL FIX: Save the initial selection if it exists and the parameter is empty
            if (colorDropdown.SelectedItem != null && string.IsNullOrEmpty(param.Value))
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL FIX: Saving initial score area selection because parameter was empty");
                saveCurrentSelection();
            }

            // Allow updates after initialization
            isInitializing = false;

            // Event handlers - FIXED: Properly capture references and handle async operations
            testButton.Click += async (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"=== SCORE AREA COLOR TEST BUTTON CLICKED ===");
                
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Test button clicked but app is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"App available: {app.Name}");

                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect && 
                    !string.IsNullOrEmpty(colorEffect))
                {
                    System.Diagnostics.Debug.WriteLine($"Testing WLED score area color: {colorEffect}");
                    
                    // Disable button to prevent multiple clicks
                    testButton.IsEnabled = false;
                    var originalContent = testButton.Content;
                    testButton.Content = "⏳";
                    
                    try
                    {
                        // For score area effects, we test the color directly
                        // The WledApi.TestColorAsync method expects just the color name without "solid|" prefix
                        System.Diagnostics.Debug.WriteLine($"Calling WledApi.TestColorAsync with app='{app.Name}', color='{colorEffect}'");
                        
                        var success = await WledApi.TestColorAsync(app, colorEffect);
                        
                        if (success)
                        {
                            testButton.Content = "✅";
                            System.Diagnostics.Debug.WriteLine("Score area color test successful");
                        }
                        else
                        {
                            testButton.Content = "❌";
                            System.Diagnostics.Debug.WriteLine("Score area color test failed");
                        }
                        
                        // Reset button after delay
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        testButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine($"Error testing score area color: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        await Task.Delay(1500);
                    }
                    finally
                    {
                        testButton.Content = originalContent;
                        testButton.IsEnabled = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Test button clicked but no color selected or color is empty");
                }
            };

            stopButton.Click += async (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"=== SCORE AREA COLOR STOP BUTTON CLICKED ===");
                
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Stop button clicked but app is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"App available: {app.Name}");
                System.Diagnostics.Debug.WriteLine("Stopping WLED effects");
                
                // Disable button to prevent multiple clicks
                stopButton.IsEnabled = false;
                var originalContent = stopButton.Content;
                stopButton.Content = "⏳";
                
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Calling WledApi.StopEffectsAsync with app='{app.Name}'");
                    
                    var success = await WledApi.StopEffectsAsync(app);
                    if (success)
                    {
                        stopButton.Content = "✅";
                        System.Diagnostics.Debug.WriteLine("Score area effects stopped successfully");
                    }
                    else
                    {
                        stopButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine("Failed to stop score area effects");
                    }
                    
                    // Reset button after delay
                    await Task.Delay(1500);
                }
                catch (Exception ex)
                {
                    stopButton.Content = "❌";
                    System.Diagnostics.Debug.WriteLine($"Error stopping score area effects: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    await Task.Delay(1500);
                }
                finally
                {
                    stopButton.Content = originalContent;
                    stopButton.IsEnabled = true;
                }
            };

            colorDropdown.SelectionChanged += (s, e) =>
            {
                if (!isInitializing)
                {
                    saveCurrentSelection();
                }
            };

            panel.Children.Add(colorDropdown);
            panel.Children.Add(testButton);
            panel.Children.Add(stopButton);
            
            System.Diagnostics.Debug.WriteLine($"=== SCORE AREA COLOR DROPDOWN COMPLETE ===");
            
            return panel;
        }
    }
}