using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using darts_hub.model;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace darts_hub.control
{
    /// <summary>
    /// Helper class for WLED Score Area Effects with range selection
    /// </summary>
    public static class WledScoreAreaHelper
    {
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
                IsVisible = false // Hidden until mode is selected
            };

            // Parse current value if exists (format: "from-to effect" or manual format)
            int? selectedFrom = null;
            int? selectedTo = null;
            string? effectPart = null;
            bool isManualMode = false;

            if (!string.IsNullOrEmpty(param.Value))
            {
                var mainParts = param.Value.Split(' ', 2);
                if (mainParts.Length >= 2)
                {
                    var rangeParts = mainParts[0].Split('-');
                    if (rangeParts.Length == 2 && 
                        int.TryParse(rangeParts[0], out var from) && 
                        int.TryParse(rangeParts[1], out var to))
                    {
                        // This is a range-based value (automatic mode)
                        selectedFrom = from;
                        selectedTo = to;
                        effectPart = mainParts[1];
                        
                        // Set range dropdowns
                        foreach (ComboBoxItem item in fromDropdown.Items)
                        {
                            if (item.Tag is int value && value == from)
                            {
                                fromDropdown.SelectedItem = item;
                                break;
                            }
                        }
                        
                        foreach (ComboBoxItem item in toDropdown.Items)
                        {
                            if (item.Tag is int value && value == to)
                            {
                                toDropdown.SelectedItem = item;
                                break;
                            }
                        }
                        
                        // Set mode based on effect type
                        if (IsPresetParameter(effectPart))
                        {
                            modeSelector.SelectedItem = presetsItem;
                        }
                        else if (WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).Contains(effectPart))
                        {
                            modeSelector.SelectedItem = effectsItem;
                        }
                        else if (NewSettingsContentProvider.ColorEffects.Contains(effectPart))
                        {
                            modeSelector.SelectedItem = colorsItem;
                        }
                        else
                        {
                            modeSelector.SelectedItem = manualItem;
                            isManualMode = true;
                        }
                    }
                    else
                    {
                        // This is manual format - entire value goes to manual input
                        isManualMode = true;
                        modeSelector.SelectedItem = manualItem;
                    }
                }
                else
                {
                    // Single value - manual mode
                    isManualMode = true;
                    modeSelector.SelectedItem = manualItem;
                }
            }
            else
            {
                // Default to manual mode for empty values
                modeSelector.SelectedItem = manualItem;
                isManualMode = true;
            }

            // Show/hide panels based on current mode
            rangePanel.IsVisible = !isManualMode;
            effectContainer.IsVisible = true; // Always show initially

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
                        
                        System.Diagnostics.Debug.WriteLine($"Updated manual score area effect parameter: {param.Value}");
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
                            
                            System.Diagnostics.Debug.WriteLine($"Updated automatic score area effect parameter: {param.Value}");
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
                        "presets" => await CreateWledPresetsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app),
                        "colors" => CreateColorEffectsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }),
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

            // Initialize effect container immediately for manual mode and when existing data exists
            if (modeSelector.SelectedItem != null)
            {
                var currentMode = (modeSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                
                if (currentMode == "manual")
                {
                    // For manual mode, immediately create the text input
                    var tempParam = new Argument("temp", Argument.TypeString, false) { Value = param.Value };
                    var manualControl = CreateManualEffectInput(tempParam, () => { 
                        effectPart = tempParam.Value; 
                        param.Value = tempParam.Value;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    });
                    effectContainer.Child = manualControl;
                }
                else if (effectContainer.IsVisible)
                {
                    // For automatic modes, use async initialization
                    Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                        {
                            var mode = selectedItem.Tag?.ToString();
                            var tempParam = new Argument("temp", Argument.TypeString, false) { Value = effectPart };
                            
                            Control newControl = mode switch
                            {
                                "effects" => await CreateWledEffectsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app),
                                "presets" => await CreateWledPresetsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }, app),
                                "colors" => CreateColorEffectsDropdown(tempParam, () => { effectPart = tempParam.Value; UpdateParameterValue(); }),
                                _ => CreateManualEffectInput(tempParam, () => { 
                                    effectPart = tempParam.Value; 
                                    param.Value = tempParam.Value;
                                    param.IsValueChanged = true;
                                    saveCallback?.Invoke();
                                })
                            };
                            
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                effectContainer.Child = newControl;
                            });
                        }
                    });
                }
            }

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
            if (string.IsNullOrEmpty(value)) return false;
            
            // Check for ps|1, ps|2, etc. format (with or without duration)
            var parts = value.Split('|');
            
            return parts.Length >= 2 && 
                   parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase) && 
                   int.TryParse(parts[1], out _);
        }

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

        private static async Task<Control> CreateWledEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            // Create a panel to hold dropdown and refresh button (no duration for effects)
            var panel = new StackPanel
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
                MinWidth = 200
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

            ToolTip.SetTip(refreshButton, "Refresh effects from WLED controller");

            // Parse current value (no duration parsing for effects)
            string? selectedEffect = param.Value;

            // Function to populate effects
            async Task PopulateEffects()
            {
                effectDropdown.PlaceholderText = "Loading WLED effects...";
                effectDropdown.Items.Clear();
                
                if (app != null)
                {
                    var (effects, source, isLive) = await WledApi.GetEffectsWithFallbackAsync(app);
                    
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
                            effectDropdown.SelectedItem = effectItem;
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
                            effectDropdown.SelectedItem = effectItem;
                        }
                    }
                    effectDropdown.PlaceholderText = "Select WLED effect...";
                }
            }

            // Initial population
            await PopulateEffects();

            // Event handlers
            refreshButton.Click += async (s, e) =>
            {
                refreshButton.IsEnabled = false;
                refreshButton.Content = "⏳";
                try
                {
                    await PopulateEffects();
                }
                finally
                {
                    refreshButton.Content = "🔄";
                    refreshButton.IsEnabled = true;
                }
            };

            effectDropdown.SelectionChanged += (s, e) =>
            {
                if (effectDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string effect)
                {
                    param.Value = effect; // Just the effect name, no duration
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                }
            };

            panel.Children.Add(effectDropdown);
            panel.Children.Add(refreshButton);
            return panel;
        }

        private static async Task<Control> CreateWledPresetsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
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

            ToolTip.SetTip(refreshButton, "Refresh presets from WLED controller");

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

            // Parse current value if it contains duration info
            string? selectedPreset = null;
            decimal selectedDuration = 0m;
            
            if (!string.IsNullOrEmpty(param.Value))
            {
                // Try to parse format: "ps|1|duration" or just "ps|1"
                var parts = param.Value.Split('|');
                if (parts.Length >= 2 && parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase))
                {
                    selectedPreset = $"ps|{parts[1]}"; // Reconstruct ps|number format
                    if (parts.Length > 2 && decimal.TryParse(parts[2], System.Globalization.NumberStyles.Float, 
                        System.Globalization.CultureInfo.InvariantCulture, out var parsedDuration))
                    {
                        selectedDuration = Math.Max(0m, Math.Min(60m, parsedDuration));
                    }
                }
                durationUpDown.Value = selectedDuration;
            }

            // Flag to prevent recursive updates
            bool isUpdating = false;

            // Function to update parameter value
            void UpdateParameterValue()
            {
                if (isUpdating) return;
                
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
                        
                        if (selectedPreset == presetValue)
                        {
                            presetDropdown.SelectedItem = presetItem;
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
                        
                        if (selectedPreset == presetValue)
                        {
                            presetDropdown.SelectedItem = presetItem;
                        }
                    }
                    presetDropdown.PlaceholderText = "Select preset...";
                }
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
                    await PopulatePresets();
                }
                finally
                {
                    refreshButton.Content = "🔄";
                    refreshButton.IsEnabled = true;
                }
            };

            presetDropdown.SelectionChanged += (s, e) =>
            {
                if (!isUpdating && presetDropdown.SelectedItem is ComboBoxItem { Tag: string })
                {
                    UpdateParameterValue();
                }
            };

            durationUpDown.ValueChanged += (s, e) =>
            {
                if (!isUpdating && durationUpDown.Value.HasValue)
                {
                    UpdateParameterValue();
                }
            };

            // Build the UI
            presetPanel.Children.Add(presetDropdown);
            presetPanel.Children.Add(refreshButton);

            durationPanel.Children.Add(durationLabel);
            durationPanel.Children.Add(durationUpDown);
            durationPanel.Children.Add(secondsLabel);

            mainPanel.Children.Add(presetPanel);
            mainPanel.Children.Add(durationPanel);

            return mainPanel;
        }

        private static Control CreateColorEffectsDropdown(Argument param, Action? saveCallback = null)
        {
            var colorDropdown = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select color effect..."
            };

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
                
                if (param.Value == colorEffect)
                {
                    colorDropdown.SelectedItem = colorItem;
                }
            }

            colorDropdown.SelectionChanged += (s, e) =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect)
                {
                    param.Value = colorEffect;
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                }
            };

            return colorDropdown;
        }
    }
}