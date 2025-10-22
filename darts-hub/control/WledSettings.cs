using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// WLED-specific settings and UI components for the new settings content provider
    /// </summary>
    public static class WledSettings
    {
        /// <summary>
        /// Checks if a parameter value is in preset format (ps|X)
        /// </summary>
        public static bool IsPresetParameter(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            // Check for ps|1, ps|2, etc. format (with or without duration: ps|1|d5)
            var parts = value.Split('|');
            
            return parts.Length >= 2 && 
                   parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase) && 
                   int.TryParse(parts[1], out _);
        }

        /// <summary>
        /// Checks if a parameter value is a color effect
        /// </summary>
        public static bool IsColorEffect(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            // Check if it's a simple color effect (just a color name)
            if (NewSettingsContentProvider.ColorEffects.Contains(value))
            {
                return true;
            }
            
            // Check if it's a color effect with solid prefix (solid|colorname format)
            if (value.StartsWith("solid|", StringComparison.OrdinalIgnoreCase))
            {
                var colorName = value.Substring(6); // Remove "solid|" prefix
                bool isColorEffect = NewSettingsContentProvider.ColorEffects.Contains(colorName);
                System.Diagnostics.Debug.WriteLine($"IsColorEffect check: '{value}' -> colorName: '{colorName}' -> isColorEffect: {isColorEffect}");
                
                // Also check with case insensitive comparison for better matching
                if (!isColorEffect)
                {
                    isColorEffect = NewSettingsContentProvider.ColorEffects.Any(color => 
                        string.Equals(color, colorName, StringComparison.OrdinalIgnoreCase));
                    System.Diagnostics.Debug.WriteLine($"IsColorEffect case-insensitive check: '{colorName}' -> isColorEffect: {isColorEffect}");
                }
                
                return isColorEffect;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if a parameter value is in WLED effect format
        /// </summary>
        /// <param name="value">The parameter value to check</param>
        /// <returns>True if the value is a WLED effect parameter</returns>
        public static bool IsWledEffectParameter(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            // First check if it's a color effect (these should be handled by the color dropdown)
            if (IsColorEffect(value))
            {
                System.Diagnostics.Debug.WriteLine($"IsWledEffectParameter: '{value}' is a color effect, not a WLED effect");
                return false;
            }
            
            var parts = value.Split('|');
            if (parts.Length == 0) return false;
            
            var effectName = parts[0];
            
            // Check if the effect name is in the fallback categories (known WLED effects)
            var allFallbackEffects = WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
            if (allFallbackEffects.Contains(effectName))
            {
                System.Diagnostics.Debug.WriteLine($"IsWledEffectParameter: '{effectName}' found in fallback effects");
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
                    System.Diagnostics.Debug.WriteLine($"IsWledEffectParameter: '{effectName}' has new format parameters");
                    return true;
                }
            }
            
            // Check if it has the old format with 4 parts (effect|palette|speed|intensity)
            if (parts.Length == 4)
            {
                // Try to parse speed and intensity as numbers
                if (int.TryParse(parts[2], out _) && int.TryParse(parts[3], out _))
                {
                    System.Diagnostics.Debug.WriteLine($"IsWledEffectParameter: '{effectName}' has old 4-part format");
                    return true;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"IsWledEffectParameter: '{value}' not recognized as WLED effect");
            return false;
        }

        /// <summary>
        /// Creates an advanced effect parameter control with mode selection for WLED effects
        /// </summary>
        public static Control CreateAdvancedEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Input mode selector
            var modeSelector = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select input mode..."
            };

            var manualItem = new ComboBoxItem { Content = "🖊️ Manual Input", Tag = "manual", Foreground = Brushes.White };
            var effectsItem = new ComboBoxItem { Content = "✨ WLED Effects", Tag = "effects", Foreground = Brushes.White };
            var presetsItem = new ComboBoxItem { Content = "🎨 Presets", Tag = "presets", Foreground = Brushes.White };
            var colorsItem = new ComboBoxItem { Content = "🌈 Color Effects", Tag = "colors", Foreground = Brushes.White };

            modeSelector.Items.Add(manualItem);
            modeSelector.Items.Add(effectsItem);
            modeSelector.Items.Add(presetsItem);
            modeSelector.Items.Add(colorsItem);

            // Container for the input control
            var inputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0)
            };

            // Analyze current value to determine mode and content
            string? currentEffectValue = param.Value;
            bool isManualMode = true;
            
            System.Diagnostics.Debug.WriteLine($"=== ADVANCED EFFECT PARAMETER PARSING START ===");
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: '{param.Name}'");

            // CRITICAL FIX: Improve detection logic to prioritize color effects
            // Set default selection based on current value with better detection
            if (!string.IsNullOrEmpty(param.Value))
            {
                // Check for preset parameters first (ps|X format)
                if (IsPresetParameter(param.Value))
                {
                    modeSelector.SelectedItem = presetsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: PRESETS (detected preset parameter)");
                }
                // CRITICAL FIX: Check for color effects more thoroughly, including solid|color format
                else if (IsColorEffect(param.Value))
                {
                    modeSelector.SelectedItem = colorsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: COLORS (detected color effect)");
                }
                // ADDITIONAL CHECK: Explicitly check for solid|color pattern as backup
                else if (param.Value.StartsWith("solid|", StringComparison.OrdinalIgnoreCase))
                {
                    // Even if the color wasn't found in the list, if it has solid| prefix, treat as color
                    modeSelector.SelectedItem = colorsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: COLORS (detected solid| prefix - forcing color mode)");
                }
                // Check for WLED effect parameters (only if not a color effect)
                else if (IsWledEffectParameter(param.Value))
                {
                    modeSelector.SelectedItem = effectsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: EFFECTS (detected WLED effect parameter)");
                }
                else
                {
                    modeSelector.SelectedItem = manualItem;
                    isManualMode = true;
                    System.Diagnostics.Debug.WriteLine($"MODE: MANUAL (fallback)");
                }
            }
            else
            {
                modeSelector.SelectedItem = manualItem;
                isManualMode = true;
                System.Diagnostics.Debug.WriteLine($"MODE: MANUAL (empty value)");
            }

            System.Diagnostics.Debug.WriteLine($"Is Manual Mode: {isManualMode}");

            // Handle mode changes
            modeSelector.SelectionChanged += async (s, e) =>
            {
                if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                {
                    var mode = selectedItem.Tag?.ToString();
                    
                    // Show loading indicator
                    inputContainer.Child = new TextBlock 
                    { 
                        Text = "Loading...", 
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    
                    try
                    {
                        Control newControl = mode switch
                        {
                            "manual" => CreateManualEffectInput(param, saveCallback),
                            "effects" => await CreateWledEffectsDropdown(param, saveCallback, app),
                            "presets" => await CreateWledPresetsDropdown(param, saveCallback, app),
                            "colors" => CreateColorEffectsDropdown(param, saveCallback, app),
                            _ => CreateManualEffectInput(param, saveCallback)
                        };
                        
                        inputContainer.Child = newControl;
                        System.Diagnostics.Debug.WriteLine($"Successfully created control for mode: {mode}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating control for mode {mode}: {ex.Message}");
                        // Fallback to manual input on error
                        inputContainer.Child = CreateManualEffectInput(param, saveCallback);
                    }
                }
            };

            // Initialize with correct control based on detected mode
            System.Diagnostics.Debug.WriteLine($"=== INITIALIZATION START ===");
            System.Diagnostics.Debug.WriteLine($"Mode Selector Selected Item: {modeSelector.SelectedItem}");

            if (modeSelector.SelectedItem != null)
            {
                var currentMode = (modeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                System.Diagnostics.Debug.WriteLine($"Current Mode: '{currentMode}'");
                
                if (isManualMode)
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Manual mode");
                    // Create manual text input immediately
                    var currentInputControl = CreateManualEffectInput(param, saveCallback);
                    inputContainer.Child = currentInputControl;
                    System.Diagnostics.Debug.WriteLine($"INITIALIZED: Manual text input created");
                }
                else if (currentMode == "presets" && !string.IsNullOrEmpty(currentEffectValue))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Preset mode with value");
                    
                    // Show loading indicator initially
                    inputContainer.Child = new TextBlock 
                    { 
                        Text = "Loading presets...", 
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    
                    // Initialize presets asynchronously
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"BACKGROUND: Starting async preset creation");
                            
                            // Create the preset control on UI thread
                            var presetControl = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                            {
                                System.Diagnostics.Debug.WriteLine($"UI THREAD: Creating preset control");
                                return await CreateWledPresetsDropdown(param, saveCallback, app);
                            });
                            
                            System.Diagnostics.Debug.WriteLine($"BACKGROUND: Preset control created successfully");
                            
                            // Set the control on UI thread
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                inputContainer.Child = presetControl;
                                System.Diagnostics.Debug.WriteLine($"UI THREAD: Preset control set to container");
                            });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"_BACKGROUND ERROR: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"BACKGROUND STACK: {ex.StackTrace}");
                            
                            // Fallback to manual input on error
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                inputContainer.Child = CreateManualEffectInput(param, saveCallback);
                                System.Diagnostics.Debug.WriteLine($"UI THREAD: Fallback manual input created due to error");
                            });
                        }
                    });
                }
                else if (currentMode == "effects" && !string.IsNullOrEmpty(currentEffectValue))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Effects mode");
                    
                    // Show loading indicator initially
                    inputContainer.Child = new TextBlock 
                    { 
                        Text = "Loading effects...", 
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    
                    // Create effects control asynchronously on dispatcher
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Creating effects control asynchronously");
                            var effectControl = await CreateWledEffectsDropdown(param, saveCallback, app);
                            inputContainer.Child = effectControl;
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Effects control set successfully");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"UI THREAD ERROR: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"UI THREAD STACK: {ex.StackTrace}");
                            
                            // Fallback to manual input on error
                            inputContainer.Child = CreateManualEffectInput(param, saveCallback);
                            System.Diagnostics.Debug.WriteLine($"UI THREAD: Fallback manual input created due to error");
                        }
                    });
                }
                else if (currentMode == "colors" && !string.IsNullOrEmpty(currentEffectValue))
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Colors mode");
                    // For colors, create immediately (synchronous)
                    inputContainer.Child = CreateColorEffectsDropdown(param, saveCallback, app);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"INITIALIZING: Default fallback to manual");
                    System.Diagnostics.Debug.WriteLine($"  Reason: currentMode='{currentMode}', effectValue='{currentEffectValue}', isEmpty={string.IsNullOrEmpty(currentEffectValue)}");
                    
                    // Default to manual mode if no specific mode matched
                    inputContainer.Child = CreateManualEffectInput(param, saveCallback);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"INITIALIZING: No mode selected - default fallback");
                // Default fallback
                inputContainer.Child = CreateManualEffectInput(param, saveCallback);
            }

            System.Diagnostics.Debug.WriteLine($"=== INITIALIZATION COMPLETE ===");

            mainPanel.Children.Add(modeSelector);
            mainPanel.Children.Add(inputContainer);

            return mainPanel;
        }

        /// <summary>
        /// Creates a manual text input for effect parameters
        /// </summary>
        private static Control CreateManualEffectInput(Argument param, Action? saveCallback = null)
        {
            var textBox = new TextBox
            {
                Text = param.Value ?? "",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = "Enter effect manually..."
            };
            
            textBox.TextChanged += (s, e) =>
            {
                param.Value = textBox.Text;
                param.IsValueChanged = true;
                saveCallback?.Invoke();
            };
            
            return textBox;
        }

        /// <summary>
        /// Creates a comprehensive WLED effects dropdown with speed, intensity, palette and duration controls
        /// </summary>
        public static async Task<Control> CreateWledEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
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
                MinWidth = 220
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
                Minimum = 1,
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
                Minimum = 1,
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

            // Create duration control
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
                MinWidth = 50
            };

            var durationUpDown = new NumericUpDown
            {
                Value = 0, // Default 0 seconds (no duration limit)
                Minimum = 0m,
                Maximum = 300m, // Max 5 minutes
                Increment = 1m,
                FormatString = "F0",
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
                Text = "sec (0 = no limit)",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Parse current value to extract effect, palette, speed, intensity, duration
            // Expected format: {effect-name/ID}|s{speed}|i{intensity}|p{palette-ID}|d{duration}
            string? selectedEffect = null;
            string? selectedPalette = null;
            int selectedSpeed = 128;
            int selectedIntensity = 128;
            decimal selectedDuration = 0m;

            if (!string.IsNullOrEmpty(param.Value))
            {
                var parts = param.Value.Split('|');
                if (parts.Length > 0) 
                {
                    selectedEffect = parts[0];
                }
                
                // Parse each part looking for the format s{value}, i{value}, p{value}, d{value}
                for (int i = 1; i < parts.Length; i++)
                {
                    var part = parts[i];
                    if (string.IsNullOrEmpty(part)) continue;
                    
                    if (part.StartsWith("s") && int.TryParse(part.Substring(1), out var speed))
                    {
                        selectedSpeed = Math.Max(1, Math.Min(255, speed));
                    }
                    else if (part.StartsWith("i") && int.TryParse(part.Substring(1), out var intensity))
                    {
                        selectedIntensity = Math.Max(1, Math.Min(255, intensity));
                    }
                    else if (part.StartsWith("p"))
                    {
                        var paletteValue = part.Substring(1);
                        // Store palette value - could be name or ID
                        selectedPalette = paletteValue;
                    }
                    else if (part.StartsWith("d") && decimal.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float, 
                            System.Globalization.CultureInfo.InvariantCulture, out var duration))
                    {
                        selectedDuration = Math.Max(0m, Math.Min(300m, duration));
                    }
                    // Fallback: handle old format without prefixes for backward compatibility
                    else
                    {
                        // Try to handle old format: effect|palette|speed|intensity
                        if (i == 1 && !part.StartsWith("s") && !part.StartsWith("i") && !part.StartsWith("p") && !part.StartsWith("d"))
                        {
                            // This could be old format palette
                            selectedPalette = part;
                        }
                        else if (i == 2 && int.TryParse(part, out var oldSpeed))
                        {
                            selectedSpeed = Math.Max(1, Math.Min(255, oldSpeed));
                        }
                        else if (i == 3 && int.TryParse(part, out var oldIntensity))
                        {
                            selectedIntensity = Math.Max(1, Math.Min(255, oldIntensity));
                        }
                    }
                }
            }

            // Set initial values
            speedSlider.Value = selectedSpeed;
            speedValue.Text = selectedSpeed.ToString();
            intensitySlider.Value = selectedIntensity;
            intensityValue.Text = selectedIntensity.ToString();
            durationUpDown.Value = selectedDuration;

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
                var duration = (int)Math.Round(durationUpDown.Value ?? 0);

                if (!string.IsNullOrEmpty(effect))
                {
                    isUpdating = true;
                    try
                    {
                        var parts = new List<string> { effect };
                        
                        // Add speed component
                        parts.Add($"s{speed}");
                        
                        // Add intensity component
                        parts.Add($"i{intensity}");
                        
                        // Add palette component if selected and not empty
                        if (!string.IsNullOrEmpty(palette))
                        {
                            parts.Add($"p{palette}");
                        }
                        
                        // Add duration component if > 0
                        if (duration > 0)
                        {
                            parts.Add($"d{duration}");
                        }
                        
                        param.Value = string.Join("|", parts);
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                        
                        System.Diagnostics.Debug.WriteLine($"Updated WLED effect parameter: {param.Value}");
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
                effectDropdown.PlaceholderText = "Loading WLED effects...";
                effectDropdown.Items.Clear();
                
                ComboBoxItem? effectToSelect = null;
                
                var (effects, source, isLive) = await WledApi.GetEffectsWithFallbackAsync(app);
                
                // Add info header
                var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                var headerText = isLive ? $"--- {source} ---" : "--- Fallback Effects ---";
                
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
                    }
                }

                effectDropdown.PlaceholderText = isLive ? 
                    "Select WLED effect (local data)..." : 
                    "Select WLED effect (fallback data)...";
                
                if (effectToSelect != null)
                {
                    effectDropdown.SelectedItem = effectToSelect;
                }
            }

            // Function to populate palettes
            async Task PopulatePalettes()
            {
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
                }
                
                var (palettes, source, isLive) = await WledApi.GetPalettesWithFallbackAsync(app);
                
                // Add info header
                var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                var headerText = isLive ? $"--- {source} ---" : "--- Fallback Palettes ---";
                
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
                    }
                }

                paletteDropdown.PlaceholderText = isLive ? 
                    "Select palette (local data)..." : 
                    "Select palette (fallback data)...";
                
                if (paletteToSelect != null)
                {
                    paletteDropdown.SelectedItem = paletteToSelect;
                }
            }

            // Initial population
            await PopulateEffects();
            await PopulatePalettes();
            
            // Allow updates after initial population
            isInitializing = false;

            // Event handlers - FIXED: Properly capture references and handle async operations
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
                    System.Diagnostics.Debug.WriteLine("Test button clicked but no effect selected");
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

            durationUpDown.ValueChanged += (s, e) =>
            {
                if (!isUpdating && !isInitializing)
                {
                    UpdateParameterValue();
                }
            };

            // Build the UI
            effectPanel.Children.Add(effectDropdown);
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

            durationPanel.Children.Add(durationLabel);
            durationPanel.Children.Add(durationUpDown);
            durationPanel.Children.Add(secondsLabel);

            mainPanel.Children.Add(effectPanel);
            mainPanel.Children.Add(palettePanel);
            mainPanel.Children.Add(speedPanel);
            mainPanel.Children.Add(intensityPanel);
            mainPanel.Children.Add(durationPanel);

            return mainPanel;
        }

        /// <summary>
        /// Creates a WLED presets dropdown with duration control
        /// </summary>
        public static async Task<Control> CreateWledPresetsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            // Create a main panel to hold preset selection and duration
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Create a panel to hold dropdown and test buttons
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
                MinWidth = 220
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
                Maximum = 300m, // Max 5 minutes
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
                Text = "sec (0 = no limit)",
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Parse current value if it contains duration info
            // Expected format: ps|{preset-ID}|d{duration} or just ps|{preset-ID}
            string? selectedPreset = null;
            decimal selectedDuration = 0m; // Default to 0
            
            if (!string.IsNullOrEmpty(param.Value))
            {
                var parts = param.Value.Split('|');
                if (parts.Length >= 2 && parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase))
                {
                    selectedPreset = $"ps|{parts[1]}"; // Reconstruct ps|number format
                    
                    // Look for duration in remaining parts
                    for (int i = 2; i < parts.Length; i++)
                    {
                        var part = parts[i];
                        if (!string.IsNullOrEmpty(part) && part.StartsWith("d"))
                        {
                            if (decimal.TryParse(part.Substring(1), System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out var parsedDuration))
                            {
                                selectedDuration = Math.Max(0m, Math.Min(300m, parsedDuration));
                            }
                            break;
                        }
                    }
                }
                durationUpDown.Value = selectedDuration;
            }

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
                        
                        // Format: ps|{preset-ID}|d{duration} or just ps|{preset-ID}
                        if (durationValue == 0)
                        {
                            param.Value = preset;
                        }
                        else
                        {
                            param.Value = $"{preset}|d{durationValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                        }
                        
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                        
                        System.Diagnostics.Debug.WriteLine($"Updated preset parameter: {param.Value}");
                    }
                    finally
                    {
                        isUpdating = false;
                    }
                }
                else if (presetDropdown.SelectedItem is ComboBoxItem && durationUpDown.Value.HasValue)
                {
                    // If preset is selected but duration is invalid, clear the value
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

            // Function to populate presets
            async Task PopulatePresets()
            {
                presetDropdown.PlaceholderText = "Loading WLED presets...";
                presetDropdown.Items.Clear();
                
                ComboBoxItem? itemToSelect = null;
                
                var (presets, source, isLive) = await WledApi.GetPresetsWithFallbackAsync(app);
                
                // Add info header
                var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                var headerText = isLive ? $"--- {source} ---" : "--- Fallback Presets ---";
                
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
                    var presetValue = $"ps|{preset.Key}"; // Use ps|1, ps|2, etc.
                    
                    var presetItem = new ComboBoxItem
                    {
                        Content = presetDisplayName,
                        Tag = presetValue,
                        Foreground = Brushes.White
                    };
                    presetDropdown.Items.Add(presetItem);
                    
                    // Pre-select if this matches current value
                    if (selectedPreset == presetValue)
                    {
                        itemToSelect = presetItem;
                    }
                }
                presetDropdown.PlaceholderText = isLive ? 
                    "Select preset (local data)..." : 
                    "Select preset (fallback data)...";
                
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
            testButton.Click += async (s, e) =>
            {
                if (presetDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string presetTag && 
                    app != null)
                {
                    // Extract preset ID from "ps|X" format
                    var parts = presetTag.Split('|');
                    if (parts.Length >= 2 && int.TryParse(parts[1], out var presetId))
                    {
                        testButton.IsEnabled = false;
                        testButton.Content = "⏳";
                        try
                        {
                            var success = await WledApi.TestPresetAsync(app, presetId);
                            if (success)
                            {
                                testButton.Content = "▶️";
                                await Task.Delay(1000);
                            }
                            else
                            {
                                testButton.Content = "▶️";
                                await Task.Delay(1000);
                            }
                        }
                        finally
                        {
                            testButton.Content = "▶️";
                            testButton.IsEnabled = true;
                        }
                    }
                }
            };

            stopButton.Click += async (s, e) =>
            {
                if (app != null)
                {
                    stopButton.IsEnabled = false;
                    stopButton.Content = "⏳";
                    try
                    {
                        var success = await WledApi.StopEffectsAsync(app);
                        if (success)
                        {
                            stopButton.Content = "■";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            stopButton.Content = "■";
                            await Task.Delay(1000);
                        }
                    }
                    finally
                    {
                        stopButton.Content = "■";
                        stopButton.IsEnabled = true;
                    }
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
            presetPanel.Children.Add(testButton);
            presetPanel.Children.Add(stopButton);

            durationPanel.Children.Add(durationLabel);
            durationPanel.Children.Add(durationUpDown);
            durationPanel.Children.Add(secondsLabel);

            mainPanel.Children.Add(presetPanel);
            mainPanel.Children.Add(durationPanel);

            return mainPanel;
        }

        /// <summary>
        /// Creates a color effects dropdown for simple color selection
        /// </summary>
        public static Control CreateColorEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            // Enhanced dropdown with better keyboard navigation
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
            ToolTip.SetTip(colorDropdown, "Select a color effect\nType letters to jump to colors (e.g. 'r' for red)\nUse arrow keys to navigate");

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
            System.Diagnostics.Debug.WriteLine($"=== WLED COLOR DROPDOWN INIT ===");
            System.Diagnostics.Debug.WriteLine($"App: {app?.Name ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: {param.Name}");
            System.Diagnostics.Debug.WriteLine($"Original Parameter Value: '{param.Value}'");
            
            // Check for parameter-specific default color first
            string defaultColorForParam = NewSettingsContentProvider.DEFAULT_WLED_COLOR; // fallback
            if (NewSettingsContentProvider.ParameterColorDefaults.TryGetValue(param.Name, out var specificDefault))
            {
                defaultColorForParam = specificDefault;
                System.Diagnostics.Debug.WriteLine($"Found specific default color for '{param.Name}': '{defaultColorForParam}'");
            }
            
            // Handle default placeholder values - set default color if no real value is configured
            if (string.IsNullOrEmpty(currentColorValue) || currentColorValue == "change to activate")
            {
                currentColorValue = defaultColorForParam; // Use parameter-specific default
                System.Diagnostics.Debug.WriteLine($"Using parameter-specific default color: '{currentColorValue}'");
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
                            System.Diagnostics.Debug.WriteLine($"Keyboard search: '{searchBuffer}' -> selected '{matchingColor}'");
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
                    // CRITICAL FIX: Always store color effects with "solid|" prefix
                    param.Value = $"solid|{colorEffect}";
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                    
                    System.Diagnostics.Debug.WriteLine($"Updated color effect parameter with solid prefix: {param.Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: No color selected or invalid selection");
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

            // Set selected item after all items have been added
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
                        System.Diagnostics.Debug.WriteLine($"NO MATCH: Selected parameter-specific default color '{defaultColorForParam}'");
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
                    System.Diagnostics.Debug.WriteLine($"EMPTY VALUE: Selected parameter-specific default color '{defaultColorForParam}'");
                }
            }

            // CRITICAL FIX: Save the initial selection if it exists and the parameter is empty or doesn't have solid| prefix
            if (colorDropdown.SelectedItem != null && 
                (string.IsNullOrEmpty(param.Value) || !param.Value.StartsWith("solid|", StringComparison.OrdinalIgnoreCase)))
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL FIX: Saving initial selection because parameter was empty or missing solid| prefix");
                saveCurrentSelection();
            }

            // Allow updates after initialization
            isInitializing = false;

            // Event handlers - FIXED: Properly capture references and handle async operations
            testButton.Click += async (s, e) =>
            {
                // Ensure we have a valid app reference
                if (app == null)
                {
                    System.Diagnostics.Debug.WriteLine("Test button clicked but app is null");
                    return;
                }

                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect && 
                    !string.IsNullOrEmpty(colorEffect))
                {
                    System.Diagnostics.Debug.WriteLine($"Testing WLED color: {colorEffect}");
                    
                    // Disable button to prevent multiple clicks
                    testButton.IsEnabled = false;
                    var originalContent = testButton.Content;
                    testButton.Content = "⏳";
                    
                    try
                    {
                        var success = await WledApi.TestColorAsync(app, colorEffect);
                        
                        if (success)
                        {
                            testButton.Content = "✅";
                            System.Diagnostics.Debug.WriteLine("Color test successful");
                        }
                        else
                        {
                            testButton.Content = "❌";
                            System.Diagnostics.Debug.WriteLine("Color test failed");
                        }
                        
                        // Reset button after delay
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        testButton.Content = "❌";
                        System.Diagnostics.Debug.WriteLine($"Error testing color: {ex.Message}");
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
                    System.Diagnostics.Debug.WriteLine("Test button clicked but no color selected");
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
            
            System.Diagnostics.Debug.WriteLine($"=== WLED COLOR DROPDOWN COMPLETE ===");
            
            return panel;
        }

        /// <summary>
        /// Checks if a parameter is an effect parameter (not score area effect)
        /// </summary>
        public static bool IsEffectParameter(Argument param)
        {
            return param.Name.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                   param.Name.Contains("effects", StringComparison.OrdinalIgnoreCase) ||
                   (param.NameHuman != null && 
                    (param.NameHuman.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                     param.NameHuman.Contains("effects", StringComparison.OrdinalIgnoreCase)));
        }
    }
}