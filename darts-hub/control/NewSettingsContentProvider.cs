using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using darts_hub.model;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System;

namespace darts_hub.control
{
    /// <summary>
    /// New settings content mode for enhanced app configuration
    /// </summary>
    public class NewSettingsContentProvider
    {
        private static readonly List<string> ColorEffects = new List<string>
        {
            "Red Solid", "Green Solid", "Blue Solid", "White Solid", "Yellow Solid", "Orange Solid", "Pink Solid", "Purple Solid", "Cyan Solid", "Magenta Solid",
            "Red Fade", "Green Fade", "Blue Fade", "Rainbow Fade", "Warm White Fade", "Cool White Fade",
            "Red Pulse", "Green Pulse", "Blue Pulse", "White Pulse", "Rainbow Pulse",
            "Red Strobe", "Green Strobe", "Blue Strobe", "White Strobe", "Rainbow Strobe",
            "Team Color Red", "Team Color Blue", "Team Color Green", "Team Color Yellow",
            "Victory Gold", "Victory Rainbow", "Celebration Multi"
        };

        /// <summary>
        /// Creates the new settings content for an app
        /// </summary>
        /// <param name="app">The app to create settings for</param>
        /// <returns>A control containing the new settings UI</returns>
        public static async Task<Control> CreateNewSettingsContent(AppBase app, Action? saveCallback = null)
        {
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 700 // Limit width to fit properly in the new settings panel
            };

            // Store the save callback for later use
            if (saveCallback != null)
            {
                mainPanel.Tag = saveCallback;
            }

            // Header with app info
            var headerPanel = CreateHeaderPanel(app);
            mainPanel.Children.Add(headerPanel);

            // Status section
            var statusSection = CreateStatusSection(app);
            mainPanel.Children.Add(statusSection);

            // Quick actions section
            var actionsSection = CreateQuickActionsSection(app);
            mainPanel.Children.Add(actionsSection);

            // Configuration sections - replace the preview with actual configuration
            if (app.IsConfigurable() && app.Configuration != null)
            {
                // Configured parameters section
                var configuredSection = CreateConfiguredParametersSection(app, saveCallback);
                mainPanel.Children.Add(configuredSection);

                // Add parameter dropdown section
                var addParameterSection = CreateAddParameterSection(app, mainPanel, saveCallback);
                mainPanel.Children.Add(addParameterSection);
            }
            else
            {
                // Fallback for non-configurable apps
                var configSection = CreateConfigurationPreviewSection(app);
                mainPanel.Children.Add(configSection);
            }

            // Beta notice
            var betaNotice = CreateBetaNotice();
            mainPanel.Children.Add(betaNotice);

            return mainPanel;
        }

        private static Control CreateHeaderPanel(AppBase app)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650 // Ensure header fits within main panel width
            };

            var titleBlock = new TextBlock
            {
                Text = $"🎯 {app.CustomName} - New Settings Mode",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 650
            };

            var subtitleBlock = new TextBlock
            {
                Text = "Enhanced configuration interface (Beta)",
                FontSize = 14,
                FontStyle = FontStyle.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 650
            };

            headerPanel.Children.Add(titleBlock);
            headerPanel.Children.Add(subtitleBlock);

            return headerPanel;
        }

        private static Control CreateStatusSection(AppBase app)
        {
            var statusPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 0, 122, 204)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var statusTitle = new TextBlock
            {
                Text = "Application Status",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var runningStatus = new TextBlock
            {
                Text = app.AppRunningState ? "Running" : "Stopped",
                FontSize = 14,
                Foreground = app.AppRunningState ? 
                    new SolidColorBrush(Color.FromRgb(0, 255, 0)) : 
                    new SolidColorBrush(Color.FromRgb(255, 153, 153)),
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };

            var configStatus = new TextBlock
            {
                Text = app.IsConfigurable() ? "Configurable" : "Fixed Configuration",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };

            contentPanel.Children.Add(statusTitle);
            contentPanel.Children.Add(runningStatus);
            contentPanel.Children.Add(configStatus);

            statusPanel.Child = contentPanel;
            return statusPanel;
        }

        private static Control CreateQuickActionsSection(AppBase app)
        {
            var actionsPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 40, 167, 69)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var actionsTitle = new TextBlock
            {
                Text = "Quick Actions",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var startStopButton = new Button
            {
                Content = app.AppRunningState ? "⏹️ Stop" : "▶️ Start",
                Background = app.AppRunningState ? 
                    new SolidColorBrush(Color.FromRgb(220, 53, 69)) : 
                    new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 8),
                CornerRadius = new CornerRadius(5),
                FontWeight = FontWeight.Bold,
                MinWidth = 100
            };

            var restartButton = new Button
            {
                Content = "🔄 Restart",
                Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(15, 8),
                CornerRadius = new CornerRadius(5),
                FontWeight = FontWeight.Bold,
                IsEnabled = app.AppRunningState,
                MinWidth = 100
            };

            // Add event handlers for the buttons
            startStopButton.Click += (s, e) =>
            {
                try
                {
                    if (app.AppRunningState)
                    {
                        app.Close();
                    }
                    else
                    {
                        app.Run();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in start/stop button: {ex.Message}");
                }
            };

            restartButton.Click += (s, e) =>
            {
                try
                {
                    if (app.AppRunningState)
                    {
                        app.Close();
                        // Small delay to allow app to close
                        System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => app.Run());
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in restart button: {ex.Message}");
                }
            };

            buttonsPanel.Children.Add(startStopButton);
            buttonsPanel.Children.Add(restartButton);

            contentPanel.Children.Add(actionsTitle);
            contentPanel.Children.Add(buttonsPanel);

            actionsPanel.Child = contentPanel;
            return actionsPanel;
        }

        private static Control CreateConfiguredParametersSection(AppBase app, Action? saveCallback = null)
        {
            var mainPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            // Get configured and required parameters grouped by section
            var configuredParams = app.Configuration.Arguments
                .Where(arg => !arg.IsRuntimeArgument && (arg.Required || !string.IsNullOrEmpty(arg.Value)))
                .GroupBy(arg => arg.Section ?? "General")
                .OrderBy(group => group.Key)
                .ToList();

            if (configuredParams.Any())
            {
                // Create a section for each group
                foreach (var sectionGroup in configuredParams)
                {
                    var sectionPanel = CreateSectionPanel(sectionGroup.Key, sectionGroup.ToList(), app, saveCallback);
                    mainPanel.Children.Add(sectionPanel);
                }
            }
            else
            {
                var configPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 123, 39, 174)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 15),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MaxWidth = 650
                };

                var contentPanel = new StackPanel();

                var configTitle = new TextBlock
                {
                    Text = "⚙️ Configured Parameters",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };

                var noParamsText = new TextBlock
                {
                    Text = "No parameters configured yet.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    Margin = new Thickness(0, 5, 0, 0),
                    FontStyle = FontStyle.Italic
                };

                contentPanel.Children.Add(configTitle);
                contentPanel.Children.Add(noParamsText);
                configPanel.Child = contentPanel;
                mainPanel.Children.Add(configPanel);
            }

            return mainPanel;
        }

        private static Control CreateSectionPanel(string sectionName, List<Argument> parameters, AppBase app, Action? saveCallback = null)
        {
            var sectionPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 123, 39, 174)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            // Section header
            var sectionTitle = new TextBlock
            {
                Text = $"{sectionName}",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            contentPanel.Children.Add(sectionTitle);

            // Sort parameters within section: required first, then by name
            var sortedParams = parameters
                .OrderBy(param => param.Required ? 0 : 1)
                .ThenBy(param => param.NameHuman ?? param.Name)
                .ToList();

            // Add each parameter in this section
            foreach (var param in sortedParams)
            {
                var paramPanel = CreateParameterDisplayPanel(param, app, contentPanel, saveCallback);
                contentPanel.Children.Add(paramPanel);
            }

            sectionPanel.Child = contentPanel;
            return sectionPanel;
        }

        private static Control CreateParameterDisplayPanel(Argument param, AppBase app, StackPanel parentPanel, Action? saveCallback = null)
        {
            var paramPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contentPanel = new StackPanel();

            // Parameter header with name and required indicator
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var nameText = new TextBlock
            {
                Text = param.NameHuman ?? param.Name,
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(nameText);

            if (param.Required)
            {
                var requiredIndicator = new TextBlock
                {
                    Text = " *",
                    FontWeight = FontWeight.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
                    Margin = new Thickness(5, 0, 0, 0)
                };
                headerPanel.Children.Add(requiredIndicator);
            }

            contentPanel.Children.Add(headerPanel);

            // Parameter value display/input - now pass the app parameter
            var inputControl = CreateParameterInputControl(param, saveCallback, app);
            if (inputControl != null)
            {
                contentPanel.Children.Add(inputControl);
            }

            // Description if available
            if (!string.IsNullOrEmpty(param.Description))
            {
                var descText = new TextBlock
                {
                    Text = param.Description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 0)
                };
                contentPanel.Children.Add(descText);
            }

            Grid.SetColumn(contentPanel, 0);
            grid.Children.Add(contentPanel);

            // Remove button for non-required parameters
            if (!param.Required)
            {
                var removeButton = new Button
                {
                    Content = "✖",
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(3),
                    Width = 25,
                    Height = 25,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold
                };

                ToolTip.SetTip(removeButton, "Remove parameter");

                removeButton.Click += async (sender, e) =>
                {
                    param.Value = null;
                    // Mark as changed for saving
                    param.IsValueChanged = true;
                    // Trigger auto-save
                    saveCallback?.Invoke();
                    
                    // Find the root NewSettingsContent panel and refresh it to update dropdowns
                    var rootPanel = FindRootNewSettingsPanel(removeButton);
                    if (rootPanel != null)
                    {
                        await RefreshNewSettingsContent(app, rootPanel, saveCallback);
                    }
                };

                Grid.SetColumn(removeButton, 1);
                grid.Children.Add(removeButton);
            }

            paramPanel.Child = grid;
            return paramPanel;
        }

        private static Control? CreateParameterInputControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var type = param.GetTypeClear();
            
            // Check if this is an effect parameter
            bool isEffectParameter = IsEffectParameter(param);

            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (isEffectParameter)
                    {
                        return CreateEffectParameterControl(param, saveCallback, app);
                    }
                    else
                    {
                        var textBox = new TextBox
                        {
                            Text = param.Value ?? "",
                            PasswordChar = type == Argument.TypePassword ? '*' : '\0',
                            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                            Foreground = Brushes.White,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(8),
                            CornerRadius = new CornerRadius(3),
                            FontSize = 13
                        };
                        textBox.TextChanged += (s, e) =>
                        {
                            param.Value = textBox.Text;
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        };
                        return textBox;
                    }

                case Argument.TypeBool:
                    var checkBox = new CheckBox
                    {
                        IsChecked = !string.IsNullOrEmpty(param.Value) && (param.Value == "True" || param.Value == "1"),
                        Content = "Enabled",
                        Foreground = Brushes.White,
                        FontSize = 13
                    };
                    checkBox.Checked += (s, e) =>
                    {
                        param.Value = "True";
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };
                    checkBox.Unchecked += (s, e) =>
                    {
                        param.Value = "False";
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };
                    return checkBox;

                case Argument.TypeInt:
                    var intUpDown = new NumericUpDown
                    {
                        Value = int.TryParse(param.Value, out var intVal) ? intVal : 0,
                        Increment = 1,
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        FontSize = 13
                    };
                    intUpDown.ValueChanged += (s, e) =>
                    {
                        param.Value = intUpDown.Value?.ToString() ?? "";
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };
                    return intUpDown;

                case Argument.TypeFloat:
                    var floatUpDown = new NumericUpDown
                    {
                        Value = double.TryParse(param.Value, System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out var doubleVal) ? (decimal)doubleVal : 0,
                        Increment = 0.1m,
                        FormatString = "F2",
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        FontSize = 13
                    };
                    floatUpDown.ValueChanged += (s, e) =>
                    {
                        param.Value = floatUpDown.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };
                    return floatUpDown;

                case Argument.TypeFile:
                case Argument.TypePath:
                    var fileGrid = new Grid();
                    fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var fileTextBox = new TextBox
                    {
                        Text = param.Value ?? "",
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8),
                        CornerRadius = new CornerRadius(3),
                        FontSize = 13
                    };

                    var browseButton = new Button
                    {
                        Content = "Select",
                        Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(3),
                        Width = 30,
                        Height = 30,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    fileTextBox.TextChanged += (s, e) =>
                    {
                        param.Value = fileTextBox.Text;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };

                    Grid.SetColumn(fileTextBox, 0);
                    Grid.SetColumn(browseButton, 1);
                    fileGrid.Children.Add(fileTextBox);
                    fileGrid.Children.Add(browseButton);

                    return fileGrid;

                default:
                    return new TextBlock
                    {
                        Text = param.Value ?? "(not set)",
                        Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                        FontSize = 13,
                        FontStyle = string.IsNullOrEmpty(param.Value) ? FontStyle.Italic : FontStyle.Normal
                    };
            }
        }

        private static bool IsEffectParameter(Argument param)
        {
            return param.Name.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                   param.Name.Contains("effects", StringComparison.OrdinalIgnoreCase) ||
                   (param.NameHuman != null && 
                    (param.NameHuman.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                     param.NameHuman.Contains("effects", StringComparison.OrdinalIgnoreCase)));
        }

        private static Control CreateEffectParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
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

            // Create manual text input as default
            var currentInputControl = CreateManualEffectInput(param, saveCallback);
            inputContainer.Child = currentInputControl;

            // Set default selection based on current value
            if (!string.IsNullOrEmpty(param.Value))
            {
                if (WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).Contains(param.Value))
                {
                    modeSelector.SelectedItem = effectsItem;
                }
                else if (WledApi.FallbackPresets.Contains(param.Value))
                {
                    modeSelector.SelectedItem = presetsItem;
                }
                else if (ColorEffects.Contains(param.Value))
                {
                    modeSelector.SelectedItem = colorsItem;
                }
                else
                {
                    modeSelector.SelectedItem = manualItem;
                }
            }
            else
            {
                modeSelector.SelectedItem = manualItem;
            }

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
                        "colors" => CreateColorEffectsDropdown(param, saveCallback),
                        _ => CreateManualEffectInput(param, saveCallback)
                    };
                    
                    inputContainer.Child = newControl;
                }
            };

            // Initialize with correct mode if not manual
            if (modeSelector.SelectedItem != manualItem)
            {
                // Trigger the selection change to load the correct control
                Task.Run(async () =>
                {
                    await Task.Delay(100); // Small delay to ensure UI is ready
                    if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                    {
                        var mode = selectedItem.Tag?.ToString();
                        Control newControl = mode switch
                        {
                            "effects" => await CreateWledEffectsDropdown(param, saveCallback, app),
                            "presets" => await CreateWledPresetsDropdown(param, saveCallback, app),
                            "colors" => CreateColorEffectsDropdown(param, saveCallback),
                            _ => CreateManualEffectInput(param, saveCallback)
                        };
                        
                        // Update UI on main thread
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            inputContainer.Child = newControl;
                        });
                    }
                });
            }

            mainPanel.Children.Add(modeSelector);
            mainPanel.Children.Add(inputContainer);

            return mainPanel;
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
            // Create a panel to hold dropdown and refresh button
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
                        if (param.Value == effect)
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
                        
                        if (param.Value == effect)
                        {
                            effectDropdown.SelectedItem = effectItem;
                        }
                    }
                    effectDropdown.PlaceholderText = "Select WLED effect...";
                }
            }

            // Initial population
            await PopulateEffects();

            // Refresh button event
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
                    param.Value = effect;
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
            // Create a panel to hold dropdown and refresh button
            var panel = new StackPanel
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

            ToolTip.SetTip(refreshButton, "Refresh presets from WLED controller");

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

                    // Add presets
                    foreach (var preset in presets.OrderBy(p => p.Key))
                    {
                        var presetDisplayName = isLive ? 
                            $"Preset {preset.Key} - {preset.Value}" : 
                            preset.Value;
                        var presetValue = isLive ? preset.Key.ToString() : preset.Value;
                        
                        var presetItem = new ComboBoxItem
                        {
                            Content = presetDisplayName,
                            Tag = presetValue,
                            Foreground = Brushes.White
                        };
                        presetDropdown.Items.Add(presetItem);
                        
                        // Pre-select if this matches current value
                        if (param.Value == presetValue || param.Value == presetDisplayName)
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
                    // Just use fallback if no app provided
                    foreach (var preset in WledApi.FallbackPresets)
                    {
                        var presetItem = new ComboBoxItem
                        {
                            Content = preset,
                            Tag = preset,
                            Foreground = Brushes.White
                        };
                        presetDropdown.Items.Add(presetItem);
                        
                        if (param.Value == preset)
                        {
                            presetDropdown.SelectedItem = presetItem;
                        }
                    }
                    presetDropdown.PlaceholderText = "Select preset...";
                }
            }

            // Initial population
            await PopulatePresets();

            // Refresh button event
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
                if (presetDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string preset)
                {
                    param.Value = preset;
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                }
            };

            panel.Children.Add(presetDropdown);
            panel.Children.Add(refreshButton);
            return panel;
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

            foreach (var colorEffect in ColorEffects)
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

        private static Control CreateAddParameterSection(AppBase app, StackPanel mainPanel, Action? saveCallback = null)
        {
            var addPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 76, 175, 80)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var addTitle = new TextBlock
            {
                Text = "➕ Add Parameter",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            contentPanel.Children.Add(addTitle);

            // Debug: Let's check what arguments are being filtered out
            var allArgs = app.Configuration.Arguments.ToList();
            var effectArgs = allArgs.Where(arg => arg.Name.Contains("effect") || arg.Name.Contains("effects") || 
                                                  (arg.NameHuman != null && (arg.NameHuman.Contains("_effect") || arg.NameHuman.Contains("_effects")))).ToList();
            
            // Get available parameters (not configured and not runtime) grouped by section
            var availableParamsBySection = app.Configuration.Arguments
                .Where(arg => !arg.IsRuntimeArgument && !arg.Required && string.IsNullOrEmpty(arg.Value))
                .GroupBy(arg => arg.Section ?? "General")
                .OrderBy(group => group.Key)
                .ToList();

            var availableEffectArgs = availableParamsBySection.SelectMany(section => section)
                .Where(arg => arg.Name.Contains("effect") || arg.Name.Contains("effects") || 
                             (arg.NameHuman != null && (arg.NameHuman.Contains("_effect") || arg.NameHuman.Contains("_effects"))))
                .ToList();
            
            // Debug info panel
            var debugPanel = new TextBlock
            {
                Text = $"Debug Info:\n" +
                       $"Total arguments: {allArgs.Count}\n" +
                       $"Effect arguments found: {effectArgs.Count}\n" +
                       $"Effect args with IsRuntimeArgument=true: {effectArgs.Count(a => a.IsRuntimeArgument)}\n" +
                       $"Effect args that are Required: {effectArgs.Count(a => a.Required)}\n" +
                       $"Effect args that have Values: {effectArgs.Count(a => !string.IsNullOrEmpty(a.Value))}\n" +
                       $"Available effect args (filtered): {availableEffectArgs.Count}\n" +
                       $"Available sections: {availableParamsBySection.Count}\n" +
                       $"Total available args: {availableParamsBySection.SelectMany(s => s).Count()}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            contentPanel.Children.Add(debugPanel);

            if (availableParamsBySection.Any())
            {
                // Create section dropdowns
                foreach (var sectionGroup in availableParamsBySection)
                {
                    var sectionParams = sectionGroup.OrderBy(arg => arg.NameHuman ?? arg.Name).ToList();
                    if (sectionParams.Any())
                    {
                        var sectionDropdownPanel = CreateSectionDropdownPanel(sectionGroup.Key, sectionParams, app, mainPanel, saveCallback);
                        contentPanel.Children.Add(sectionDropdownPanel);
                    }
                }
            }
            else
            {
                var noParamsText = new TextBlock
                {
                    Text = "All available parameters are already configured.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    FontStyle = FontStyle.Italic
                };
                contentPanel.Children.Add(noParamsText);
            }

            addPanel.Child = contentPanel;
            return addPanel;
        }

        private static Control CreateSectionDropdownPanel(string sectionName, List<Argument> availableParams, AppBase app, StackPanel mainPanel, Action? saveCallback = null)
        {
            var sectionPanel = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Section title
            var sectionTitle = new TextBlock
            {
                Text = $"{sectionName}",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            sectionPanel.Children.Add(sectionTitle);

            // Dropdown and button for this section
            var dropdownPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var paramDropdown = new ComboBox
            {
                PlaceholderText = $"Select parameter from {sectionName}...",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                MinWidth = 200,
                FontSize = 13
            };

            foreach (var param in availableParams)
            {
                var item = new ComboBoxItem
                {
                    Content = param.NameHuman ?? param.Name,
                    Tag = param,
                    Foreground = Brushes.White
                };
                paramDropdown.Items.Add(item);
            }

            var addButton = new Button
            {
                Content = "Add",
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(15, 8),
                FontWeight = FontWeight.Bold,
                IsEnabled = false
            };

            paramDropdown.SelectionChanged += (s, e) =>
            {
                addButton.IsEnabled = paramDropdown.SelectedItem != null;
            };

            addButton.Click += async (s, e) =>
            {
                if (paramDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is Argument selectedParam)
                {
                    // Set a default value to make it "configured"
                    selectedParam.Value = GetDefaultValueForType(selectedParam.GetTypeClear());
                    selectedParam.IsValueChanged = true;

                    // Trigger auto-save
                    saveCallback?.Invoke();

                    // Find the root NewSettingsContent panel and refresh it
                    var rootPanel = FindRootNewSettingsPanel(addButton);
                    if (rootPanel != null)
                    {
                        await RefreshNewSettingsContent(app, rootPanel, saveCallback);
                    }
                }
            };

            dropdownPanel.Children.Add(paramDropdown);
            dropdownPanel.Children.Add(addButton);
            sectionPanel.Children.Add(dropdownPanel);

            return sectionPanel;
        }

        private static string GetDefaultValueForType(string type)
        {
            return type switch
            {
                Argument.TypeBool => "False",
                Argument.TypeInt => "0",
                Argument.TypeFloat => "0.0",
                _ => "change to activate" // String, Password, File, Path get a non-empty default value
            };
        }

        private static StackPanel? FindRootNewSettingsPanel(Control startControl)
        {
            var current = startControl.Parent;
            while (current != null)
            {
                if (current is StackPanel panel && panel.Name == "NewSettingsContent")
                {
                    return panel;
                }
                current = current.Parent;
            }
            return null;
        }

        private static async Task RefreshNewSettingsContent(AppBase app, StackPanel rootPanel, Action? saveCallback = null)
        {
            // Clear and rebuild the settings content
            rootPanel.Children.Clear();
            var newContent = await CreateNewSettingsContent(app, saveCallback);
            
            // Copy children from new content to root panel
            if (newContent is StackPanel newPanel)
            {
                while (newPanel.Children.Count > 0)
                {
                    var child = newPanel.Children[0];
                    newPanel.Children.RemoveAt(0);
                    rootPanel.Children.Add(child);
                }
            }
        }

        private static Control CreateConfigurationPreviewSection(AppBase app)
        {
            var configPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 123, 39, 174)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var configTitle = new TextBlock
            {
                Text = "📋 Configuration Preview",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var configInfo = new TextBlock
            {
                Text = app.IsConfigurable() ? 
                    $"⚙️ App has {app.Configuration?.Arguments?.Count ?? 0} configurable options\n" +
                    "🎛️ Enhanced UI controls active\n" +
                    "⚡ Real-time parameter management\n" +
                    "💡 Advanced validation and tooltips" : 
                    "This application has no configurable settings.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            };

            contentPanel.Children.Add(configTitle);
            contentPanel.Children.Add(configInfo);

            configPanel.Child = contentPanel;
            return configPanel;
        }

        private static Control CreateBetaNotice()
        {
            var noticePanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 193, 7)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 15, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var noticeText = new TextBlock
            {
                Text = "🧪 Enhanced Configuration Mode\n\n" +
                       "This new settings interface provides real-time parameter management. " +
                       "Add or remove parameters as needed, and see changes immediately. " +
                       "You can switch back to the classic settings mode in the About section.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 16
            };

            noticePanel.Child = noticeText;
            return noticePanel;
        }
    }
}