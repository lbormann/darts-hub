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
                    contentModeManager.ShowClassicSettingsMode();
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
            
            // Load new settings content with save callback
            var newSettingsContent = await NewSettingsContentProvider.CreateNewSettingsContent(app, () => mainWindow.Save());
            
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
            var contentModeManager = mainWindow.GetContentModeManager();
            contentModeManager.ShowClassicSettingsMode();
            
            // Load tooltips for this app
            await LoadTooltipsForApp(app);

            var settingsPanel = mainWindow.FindControl<StackPanel>("SettingsPanel");
            
            // Create header with app controls
            var headerPanel = CreateAppSettingsHeader(app);
            settingsPanel?.Children.Add(headerPanel);

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