using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using darts_hub.control;
using darts_hub.model;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Handles rendering of app settings UI
    /// </summary>
    public class AppSettingsRenderer
    {
        private readonly MainWindow mainWindow;
        private readonly Configurator configurator;
        private readonly ReadmeParser readmeParser;
        private Dictionary<string, string>? currentTooltips;

        public AppSettingsRenderer(MainWindow mainWindow, Configurator configurator)
        {
            this.mainWindow = mainWindow;
            this.configurator = configurator;
            this.readmeParser = new ReadmeParser();
        }

        public async Task RenderAppSettings(AppBase app)
        {
            var settingsPanel = mainWindow.FindControl<StackPanel>("SettingsPanel");
            var newSettingsContent = mainWindow.FindControl<StackPanel>("NewSettingsContent");
            
            settingsPanel?.Children.Clear();
            newSettingsContent?.Children.Clear();
            
            // Check if this is a custom app
            bool isCustomApp = app.Name.StartsWith("custom-") || app.Name.StartsWith("custom-url-");
            
            if (!app.IsConfigurable())
            {
                var message = new TextBlock
                {
                    Text = $"{app.CustomName} has no configurable settings.",
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                };

                if (configurator.Settings.NewSettingsMode)
                {
                    var contentModeManager = mainWindow.GetContentModeManager();
                    contentModeManager.ShowNewSettingsMode();
                    newSettingsContent?.Children.Add(message);
                }
                else
                {
                    var contentModeManager = mainWindow.GetContentModeManager();
                    contentModeManager.ShowClassicSettingsMode(hideTooltipForCustomApp: isCustomApp);
                    settingsPanel?.Children.Add(message);
                }
                return;
            }

            // Check if new settings mode is enabled
            if (configurator.Settings.NewSettingsMode)
            {
                await RenderNewSettingsMode(app);
            }
            else
            {
                await RenderClassicSettingsMode(app);
            }
        }

        private async Task RenderNewSettingsMode(AppBase app)
        {
            var contentModeManager = mainWindow.GetContentModeManager();
            contentModeManager.ShowNewSettingsMode();
            
            // Load new settings content with save callback and selected profile
            var selectedProfile = mainWindow.SelectedProfile;
            var newSettingsContent = await NewSettingsContentProvider.CreateNewSettingsContent(app, () => mainWindow.Save(), selectedProfile);
            
            // Clear existing content and add new content
            var newSettingsPanel = mainWindow.FindControl<StackPanel>("NewSettingsContent");
            newSettingsPanel?.Children.Clear();
            
            if (newSettingsContent is StackPanel newPanel)
            {
                // Copy children from the created content to our NewSettingsContent panel
                while (newPanel.Children.Count > 0)
                {
                    var child = newPanel.Children[0];
                    newPanel.Children.RemoveAt(0);
                    newSettingsPanel?.Children.Add(child);
                }
            }
            else
            {
                newSettingsPanel?.Children.Add(newSettingsContent);
            }
        }

        private async Task RenderClassicSettingsMode(AppBase app)
        {
            // Check if this is a custom app (custom-1 to custom-5, or custom-url-1 to custom-url-5)
            bool isCustomApp = app.Name.StartsWith("custom-") || app.Name.StartsWith("custom-url-");
            
            var contentModeManager = mainWindow.GetContentModeManager();
            contentModeManager.ShowClassicSettingsMode(hideTooltipForCustomApp: isCustomApp);
            
            // Load tooltips for this app (only for non-custom apps since custom apps won't show tooltips)
            if (!isCustomApp)
            {
                await LoadTooltipsForApp(app);
            }

            var settingsPanel = mainWindow.FindControl<StackPanel>("SettingsPanel");
            
            // Create header with app controls
            var headerPanel = CreateAppSettingsHeader(app);
            settingsPanel?.Children.Add(headerPanel);

            // Add Custom Name section
            var customNameSection = CreateCustomNameSection(app);
            settingsPanel?.Children.Add(customNameSection);

            // Add Autostart section
            var autostartSection = CreateAutostartSection(app);
            settingsPanel?.Children.Add(autostartSection);

            // Render configuration sections
            await RenderConfigurationSections(app);
        }

        private StackPanel CreateAppSettingsHeader(AppBase app)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 20, 0, 20)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = app.CustomName,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            // Add action buttons
            var buttonPanel = CreateAppActionButtons(app);
            headerPanel.Children.Add(buttonPanel);

            return headerPanel;
        }

        /// <summary>
        /// Creates a section for editing the CustomName of the app
        /// </summary>
        private Control CreateCustomNameSection(AppBase app)
        {
            var expander = new Expander
            {
                Header = "Display Name",
                IsExpanded = false, // Start collapsed in classic mode
                Margin = new Thickness(0, 10),
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var sectionPanel = new StackPanel 
            { 
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            // Description
            var descriptionText = new TextBlock
            {
                Text = "Customize how this app appears in the interface",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            sectionPanel.Children.Add(descriptionText);

            // Input panel
            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 10)
            };

            var customNameLabel = new TextBlock
            {
                Text = "Display Name:",
                FontSize = 14,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 120,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var customNameTextBox = new TextBox
            {
                Text = app.CustomName ?? app.Name,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                MinWidth = 200,
                MaxLength = 50, // Reasonable limit for display names
                Watermark = "Enter custom display name..."
            };

            var resetButton = new Button
            {
                Content = "Reset",
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(10, 6),
                Margin = new Thickness(10, 0, 0, 0),
                FontSize = 12
            };

            ToolTip.SetTip(resetButton, "Reset to original name");

            // Info text
            var infoText = new TextBlock
            {
                Text = $"Original name: '{app.Name}'",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            // Event handlers
            customNameTextBox.TextChanged += (s, e) =>
            {
                var newName = customNameTextBox.Text?.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != app.CustomName)
                {
                    var oldName = app.CustomName;
                    app.CustomName = newName;
                    
                    System.Diagnostics.Debug.WriteLine($"CustomName changed from '{oldName}' to '{newName}' for app '{app.Name}'");
                    
                    // Update the header immediately
                    UpdateAppHeaderTitle(app);
                    
                    // Trigger save
                    mainWindow.Save();
                    
                    // Update info text
                    infoText.Text = $"Custom name: '{newName}' (Original: '{app.Name}')";
                }
                else if (string.IsNullOrEmpty(newName))
                {
                    // Reset to original name if empty
                    app.CustomName = app.Name;
                    customNameTextBox.Text = app.Name;
                    
                    // Update the header immediately
                    UpdateAppHeaderTitle(app);
                    
                    infoText.Text = $"Using original name: '{app.Name}'";
                    mainWindow.Save();
                }
            };

            resetButton.Click += (s, e) =>
            {
                // ? Prevent multiple clicks
                if (resetButton.Tag?.ToString() == "processing") return;
                resetButton.Tag = "processing";
                resetButton.IsEnabled = false;
                
                try
                {
                    var originalName = app.Name;
                    customNameTextBox.Text = originalName;
                    app.CustomName = originalName;
                    
                    // Update the header immediately
                    UpdateAppHeaderTitle(app);
                    
                    infoText.Text = $"Reset to original name: '{originalName}'";
                    
                    System.Diagnostics.Debug.WriteLine($"CustomName reset to original '{originalName}' for app '{app.Name}'");
                    
                    // Trigger save
                    mainWindow.Save();
                }
                finally
                {
                    // Re-enable button after short delay
                    Task.Delay(500).ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            resetButton.Tag = null;
                            resetButton.IsEnabled = true;
                        });
                    });
                }
            };

            // Assemble the input panel
            inputPanel.Children.Add(customNameLabel);
            inputPanel.Children.Add(customNameTextBox);
            inputPanel.Children.Add(resetButton);

            // Assemble the section
            sectionPanel.Children.Add(inputPanel);
            sectionPanel.Children.Add(infoText);

            expander.Content = sectionPanel;
            return expander;
        }

        /// <summary>
        /// Updates the app header title to reflect the current CustomName
        /// </summary>
        private void UpdateAppHeaderTitle(AppBase app)
        {
            try
            {
                var settingsPanel = mainWindow.FindControl<StackPanel>("SettingsPanel");
                if (settingsPanel?.Children.Count > 0)
                {
                    // Find the header panel (should be the first child)
                    if (settingsPanel.Children[0] is StackPanel headerPanel)
                    {
                        // Find the title TextBlock (should be the first child)
                        if (headerPanel.Children.Count > 0 && headerPanel.Children[0] is TextBlock titleBlock)
                        {
                            titleBlock.Text = app.CustomName;
                            System.Diagnostics.Debug.WriteLine($"Updated header title to: {app.CustomName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating app header title: {ex.Message}");
            }
        }

        private StackPanel CreateAppActionButtons(AppBase app)
        {
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 0, 0)
            };

            // App Control Buttons
            var controlButtonPanel = CreateAppControlButtons(app);
            buttonPanel.Children.Add(controlButtonPanel);

            // Help and Changelog buttons
            var helpButtonPanel = CreateHelpButtons(app);
            buttonPanel.Children.Add(helpButtonPanel);

            return buttonPanel;
        }

        private StackPanel CreateAppControlButtons(AppBase app)
        {
            var controlButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 20, 0)
            };

            // Start/Stop Button
            var startStopButton = CreateStartStopButton(app);
            controlButtonPanel.Children.Add(startStopButton);

            // Restart Button
            var restartButton = CreateRestartButton(app);
            controlButtonPanel.Children.Add(restartButton);

            return controlButtonPanel;
        }

        private Button CreateStartStopButton(AppBase app)
        {
            var startStopButton = new Button
            {
                Content = app.AppRunningState ? "Stop" : "Start",
                Background = app.AppRunningState ? new SolidColorBrush(Color.FromRgb(220, 53, 69)) : new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6),
                Margin = new Thickness(5, 0),
                CornerRadius = new CornerRadius(3),
                FontWeight = FontWeight.Bold
            };
            
            startStopButton.Click += async (s, e) =>
            {
                var appControlManager = new AppControlManager(mainWindow);
                await appControlManager.HandleStartStopApp(app);
            };

            return startStopButton;
        }

        private Button CreateRestartButton(AppBase app)
        {
            var restartButton = new Button
            {
                Content = "Restart",
                Background = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 6),
                Margin = new Thickness(5, 0),
                CornerRadius = new CornerRadius(3),
                FontWeight = FontWeight.Bold,
                IsEnabled = app.AppRunningState
            };
            
            if (!app.AppRunningState)
            {
                restartButton.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                restartButton.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            }
            
            restartButton.Click += async (s, e) =>
            {
                var appControlManager = new AppControlManager(mainWindow);
                await appControlManager.HandleRestartApp(app);
            };

            return restartButton;
        }

        private StackPanel CreateHelpButtons(AppBase app)
        {
            var helpButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            if (!string.IsNullOrEmpty(app.ChangelogUrl))
            {
                var changelogBtn = new Button
                {
                    Content = new Image { Width = 24, Height = 24, Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/changelog.png"))) },
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(5, 0)
                };
                ToolTip.SetTip(changelogBtn, "View Changelog");
                changelogBtn.Click += async (s, e) =>
                {
                    var changelogText = await Helper.AsyncHttpGet(app.ChangelogUrl, 4);
                    if (string.IsNullOrEmpty(changelogText)) 
                        changelogText = "Changelog not available. Please try again later.";
                    
                    await mainWindow.RenderMessageBox("Changelog", changelogText, MsBox.Avalonia.Enums.Icon.None, ButtonEnum.Ok, mainWindow.Width, mainWindow.Height, 0);
                };
                helpButtonPanel.Children.Add(changelogBtn);
            }

            if (!string.IsNullOrEmpty(app.HelpUrl))
            {
                var helpBtn = new Button
                {
                    Content = new Image { Width = 24, Height = 24, Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/help.png"))) },
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(5, 0)
                };
                ToolTip.SetTip(helpBtn, "Get Help");
                helpBtn.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(app.HelpUrl)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _ = mainWindow.RenderMessageBox("Error", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.Ok, null, null, 0);
                    }
                };
                helpButtonPanel.Children.Add(helpBtn);
            }

            return helpButtonPanel;
        }

        private async Task RenderConfigurationSections(AppBase app)
        {
            var settingsPanel = mainWindow.FindControl<StackPanel>("SettingsPanel");
            var appConfiguration = app.Configuration;
            if (appConfiguration == null) return;
            
            var argumentsBySection = appConfiguration.Arguments.GroupBy(a => a.Section);

            foreach (var section in argumentsBySection)
            {
                var expander = new Expander
                {
                    Header = section.Key,
                    IsExpanded = true,
                    Margin = new Thickness(0, 10),
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var sectionPanel = new StackPanel 
                { 
                    Margin = new Thickness(10),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                foreach (var argument in section)
                {
                    if (argument.IsRuntimeArgument) continue;

                    var argumentControl = await CreateArgumentControl(argument);
                    if (argumentControl != null)
                    {
                        argumentControl.Margin = new Thickness(0, 15);
                        argumentControl.HorizontalAlignment = HorizontalAlignment.Stretch;
                        sectionPanel.Children.Add(argumentControl);
                    }
                }

                expander.Content = sectionPanel;
                settingsPanel?.Children.Add(expander);
            }
        }

        private Control CreateAutostartSection(AppBase app)
        {
            var expander = new Expander
            {
                Header = "Application Control",
                IsExpanded = true,
                Margin = new Thickness(0, 10),
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var sectionPanel = new StackPanel 
            { 
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var selectedProfile = mainWindow.SelectedProfile;
            var appState = selectedProfile?.Apps.Values.FirstOrDefault(a => a.App.CustomName == app.CustomName);
            
            if (appState != null)
            {
                var autostartPanel = CreateAutostartPanel(appState);
                sectionPanel.Children.Add(autostartPanel);
            }

            expander.Content = sectionPanel;
            return expander;
        }

        private StackPanel CreateAutostartPanel(ProfileState appState)
        {
            var autostartPanel = new StackPanel 
            { 
                Margin = new Thickness(0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            
            var autostartLabel = new TextBlock
            {
                Text = "Enable at startup",
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            autostartPanel.Children.Add(autostartLabel);

            var autostartCheckBox = new CheckBox
            {
                Content = "Start this application when the profile is launched",
                IsChecked = appState.TaggedForStart,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            autostartCheckBox.Checked += (s, e) => 
            {
                appState.TaggedForStart = true;
                mainWindow.Save();
            };
            
            autostartCheckBox.Unchecked += (s, e) => 
            {
                appState.TaggedForStart = false;
                mainWindow.Save();
            };

            autostartPanel.Children.Add(autostartCheckBox);
            return autostartPanel;
        }

        private async Task<Control?> CreateArgumentControl(Argument argument)
        {
            var argumentControlFactory = new ArgumentControlFactory(mainWindow);
            return await argumentControlFactory.CreateControl(argument, AutoSaveConfiguration, ShowTooltip);
        }

        private void AutoSaveConfiguration(Argument argument)
        {
            try
            {
                argument.IsValueChanged = true;
                mainWindow.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
        }

        private void ShowTooltip(Argument argument)
        {
            try
            {
                var tooltipDescription = mainWindow.FindControl<TextBlock>("TooltipDescription");
                if (tooltipDescription == null) return;

                // Inlines leeren, um vorherigen Text zu entfernen
                tooltipDescription.Inlines?.Clear();

                // Argument-Namen in fett hinzufügen
                tooltipDescription.Inlines?.Add(new Run(argument.NameHuman) { FontWeight = FontWeight.Bold });

                // Doppelpunkt als Trenner
                tooltipDescription.Inlines?.Add(new Run(": "));
                tooltipDescription.Inlines?.Add(new LineBreak());
                tooltipDescription.Inlines?.Add(new LineBreak());

                // Beschreibungstext in normaler Schrift hinzufügen
                string description;
                if (currentTooltips != null && currentTooltips.TryGetValue(argument.Name, out var tooltip))
                {
                    description = tooltip;
                }
                else
                {
                    description = argument.Description ?? "No description available.";
                }

                tooltipDescription.Inlines?.Add(new Run(description));
            }
            catch (Exception ex)
            {
                var tooltipDescription = mainWindow.FindControl<TextBlock>("TooltipDescription");
                if (tooltipDescription != null)
                    tooltipDescription.Text = "Error loading tooltip.";
                System.Diagnostics.Debug.WriteLine($"Error showing tooltip: {ex.Message}");
            }
        }

        private async Task LoadTooltipsForApp(AppBase app)
        {
            try
            {
                string readmeUrl = GetReadmeUrlForApp(app.CustomName);
                if (readmeUrl != "error")
                {
                    currentTooltips = await readmeParser.GetArgumentsFromReadme(readmeUrl);
                }
                else
                {
                    currentTooltips = new Dictionary<string, string>();
                }
            }
            catch (Exception ex)
            {
                currentTooltips = new Dictionary<string, string>();
                System.Diagnostics.Debug.WriteLine($"Error loading tooltips: {ex.Message}");
            }
        }

        private string GetReadmeUrlForApp(string appName)
        {
            return appName switch
            {
                "darts-caller" => "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md",
                "darts-wled" => "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md",
                "darts-pixelit" => "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md",
                "darts-gif" => "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md",
                "darts-voice" => "https://raw.githubusercontent.com/lbormann/darts-voice/refs/heads/main/README.md",
                "darts-extern" => "https://raw.githubusercontent.com/lbormann/darts-extern/refs/heads/master/README.md",
                _ => "error"
            };
        }
    }
}