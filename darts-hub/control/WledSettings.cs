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
        /// Checks if a parameter value is in WLED effect format
        /// </summary>
        /// <param name="value">The parameter value to check</param>
        /// <returns>True if the value is a WLED effect parameter</returns>
        public static bool IsWledEffectParameter(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
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

            // Set default selection based on current value
            if (!string.IsNullOrEmpty(param.Value))
            {
                if (IsPresetParameter(param.Value))
                {
                    modeSelector.SelectedItem = presetsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: PRESETS (detected preset parameter)");
                }
                else if (IsWledEffectParameter(param.Value))
                {
                    modeSelector.SelectedItem = effectsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: EFFECTS (detected WLED effect parameter)");
                }
                else if (NewSettingsContentProvider.ColorEffects.Contains(param.Value))
                {
                    modeSelector.SelectedItem = colorsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: COLORS");
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
                    
                    Control newControl = mode switch
                    {
                        "manual" => CreateManualEffectInput(param, saveCallback),
                        "effects" => await CreateWledEffectsDropdown(param, saveCallback, app),
                        "presets" => await CreateWledPresetsDropdown(param, saveCallback, app),
                        "colors" => CreateColorEffectsDropdown(param, saveCallback, app),
                        _ => CreateManualEffectInput(param, saveCallback)
                    };
                    
                    inputContainer.Child = newControl;
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
                            System.Diagnostics.Debug.WriteLine($"BACKGROUND ERROR: {ex.Message}");
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
                MinWidth = 180
            };

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
                
                if (app != null)
                {
                    var (effects, source, isLive) = await WledApi.GetEffectsWithFallbackAsync(app);
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"--- Live from {source} ---" : "--- Fallback Effects ---";
                    
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
                        "Select WLED effect (live data)..." : 
                        "Select WLED effect (fallback data)...";
                }
                else
                {
                    // Just use fallback if no app provided
                    var fallbackEffects = WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).ToList();
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
                        }
                    }
                    effectDropdown.PlaceholderText = "Select WLED effect...";
                }
                
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
                
                if (app != null)
                {
                    var (palettes, source, isLive) = await WledApi.GetPalettesWithFallbackAsync(app);
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"--- Live from {source} ---" : "--- Fallback Palettes ---";
                    
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
                        "Select palette (live data)..." : 
                        "Select palette (fallback data)...";
                }
                else
                {
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
                        }
                    }
                    paletteDropdown.PlaceholderText = "Select palette...";
                }
                
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

            // Event handlers
            refreshButton.Click += async (s, e) =>
            {
                refreshButton.IsEnabled = false;
                refreshButton.Content = "🔄";
                try
                {
                    isInitializing = true;
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
                if (effectDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string effect && 
                    app != null)
                {
                    testButton.IsEnabled = false;
                    testButton.Content = "▶️";
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
                        
                        var success = await WledApi.TestEffectAsync(app, effect, 
                            paletteName, speed, intensity);
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
            };

            stopButton.Click += async (s, e) =>
            {
                if (app != null)
                {
                    stopButton.IsEnabled = false;
                    stopButton.Content = "■";
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
                MinWidth = 180
            };

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
                
                if (app != null)
                {
                    var (presets, source, isLive) = await WledApi.GetPresetsWithFallbackAsync(app);
                    
                    // Add info header
                    var headerColor = isLive ? Color.FromRgb(100, 255, 100) : Color.FromRgb(120, 120, 120);
                    var headerText = isLive ? $"--- Live from {source} ---" : "--- Fallback Presets ---";
                    
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
                        "Select preset (live data)..." : 
                        "Select preset (fallback data)...";
                }
                else
                {
                    // Just use fallback if no app provided - create ps|1, ps|2, etc.
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
                        
                        if (selectedPreset == presetValue)
                        {
                            itemToSelect = presetItem;
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
                refreshButton.Content = "🔄";
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
                        testButton.Content = "▶️";
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
                    stopButton.Content = "■";
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

            // Simple dropdown without duration for color effects
            var colorDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select color effect...",
                MinWidth = 200
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

            ToolTip.SetTip(testButton, "Test selected color on WLED controller");
            ToolTip.SetTip(stopButton, "Stop effects on WLED controller");

            // Populate color effects
            foreach (var colorEffect in NewSettingsContentProvider.ColorEffects)
            {
                var colorItem = new ComboBoxItem
                {
                    Content = colorEffect,
                    Tag = colorEffect,
                    Foreground = Brushes.White
                };
                colorDropdown.Items.Add(colorItem);
                
                // Pre-select if this matches current value
                if (param.Value == colorEffect)
                {
                    colorDropdown.SelectedItem = colorItem;
                }
            }

            // Event handlers
            testButton.Click += async (s, e) =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect && 
                    app != null)
                {
                    testButton.IsEnabled = false;
                    testButton.Content = "▶️";
                    try
                    {
                        var success = await WledApi.TestColorAsync(app, colorEffect);
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
            };

            stopButton.Click += async (s, e) =>
            {
                if (app != null)
                {
                    stopButton.IsEnabled = false;
                    stopButton.Content = "■";
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

            colorDropdown.SelectionChanged += (s, e) =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect)
                {
                    param.Value = $"solid|{colorEffect}"; // Just the color effect name, no duration
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                }
            };

            panel.Children.Add(colorDropdown);
            panel.Children.Add(testButton);
            panel.Children.Add(stopButton);
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