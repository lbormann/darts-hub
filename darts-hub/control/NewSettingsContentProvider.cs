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
        private static readonly ReadmeParser readmeParser = new ReadmeParser();
        private static readonly Dictionary<string, Dictionary<string, string>> argumentDescriptionsCache = new();

        public static readonly List<string> ColorEffects = new List<string>
        {
            // Basic solid colors
            "red", "green", "blue", "white", "black", "yellow", "orange", "purple", "pink", "cyan", "magenta",
            
            // Extended color variations
            "red1", "red2", "red3", "red4",
            "green1", "green2", "green3", "green4", 
            "blue1", "blue2", "blue3", "blue4",
            "yellow1", "yellow2", "yellow3", "yellow4",
            "cyan2", "cyan3", "cyan4",
            "magenta2", "magenta3", "magenta4",
            "orange1", "orange2", "orange3", "orange4",
            "purple1", "purple2", "purple3", "purple4",
            "pink1", "pink2", "pink3", "pink4",
            
            // Named colors (most commonly used)
            "aliceblue", "antiquewhite", "aqua", "beige", "brown", "coral", "crimson", "darkgreen", "darkblue", "darkred",
            "emeraldgreen", "firebrick", "forestgreen", "gold1", "gold2", "goldenrod", "gray", "greenyellow",
            "hotpink", "indianred", "indigo", "ivory1", "khaki", "lavender", "lightblue", "lightcoral", "lightgreen",
            "lime", "limegreen", "maroon", "mint", "navy", "olive", "orchid", "salmon", "seagreen1", "silver", 
            "skyblue", "steelblue", "tan", "teal", "turquoise", "violet", "wheat",
            
            // Special colors
            "banana", "brick", "chocolate", "cobalt", "eggshell", "flesh", "mint", "peacock", "raspberry",
            
            // Grayscale variations  
            "gray1", "gray10", "gray20", "gray30", "gray40", "gray50", "gray60", "gray70", "gray80", "gray90", "gray99",
            "dimgray", "darkgray", "lightgrey", "whitesmoke",
            
            // Additional web colors
            "fuchsia", "aquamarine1", "cornflowerblue", "darkorchid", "deeppink1", "dodgerblue1", "mediumorchid",
            "palegreen", "royalblue", "springgreen", "violetred"
        };

        /// <summary>
        /// Clears the argument descriptions cache for debugging purposes
        /// </summary>
        public static void ClearDescriptionsCache()
        {
            argumentDescriptionsCache.Clear();
            System.Diagnostics.Debug.WriteLine("Cleared argument descriptions cache");
        }

        /// <summary>
        /// Clears the cache for a specific app
        /// </summary>
        public static void ClearDescriptionsCacheForApp(string appName)
        {
            if (argumentDescriptionsCache.ContainsKey(appName))
            {
                argumentDescriptionsCache.Remove(appName);
                System.Diagnostics.Debug.WriteLine($"Cleared cache for {appName}");
            }
        }

        /// <summary>
        /// Creates the new settings content for an app
        /// /// <remarks>
        ///  - Enhanced validation and tooltips."/>
        /// </remarks>
        /// </summary>
        /// <param name="app">The app to create settings for</param>
        /// <returns>A control containing the new settings UI</returns>
        public static async Task<Control> CreateNewSettingsContent(AppBase app, Action? saveCallback = null)
        {
            System.Diagnostics.Debug.WriteLine($"=== CREATE NEW SETTINGS CONTENT START ===");
            System.Diagnostics.Debug.WriteLine($"App: {app.Name} ({app.CustomName})");
            System.Diagnostics.Debug.WriteLine($"App type: {app.GetType().Name}");
            
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

            System.Diagnostics.Debug.WriteLine($"About to load argument descriptions...");
            
            // Load argument descriptions for this app
            await LoadArgumentDescriptionsForApp(app);
            
            System.Diagnostics.Debug.WriteLine($"Argument descriptions loaded, creating UI components...");

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
                System.Diagnostics.Debug.WriteLine($"App is configurable, creating parameter sections...");
                
                // Configured parameters section
                var configuredSection = CreateConfiguredParametersSection(app, saveCallback);
                mainPanel.Children.Add(configuredSection);

                // Add parameter dropdown section
                var addParameterSection = CreateAddParameterSection(app, mainPanel, saveCallback);
                mainPanel.Children.Add(addParameterSection);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"App is not configurable, creating preview section...");
                
                // Fallback for non-configurable apps
                var configSection = CreateConfigurationPreviewSection(app);
                mainPanel.Children.Add(configSection);
            }

            // Beta notice
            var betaNotice = CreateBetaNotice();
            mainPanel.Children.Add(betaNotice);

            System.Diagnostics.Debug.WriteLine($"=== CREATE NEW SETTINGS CONTENT COMPLETE ===");
            return mainPanel;
        }

        /// <summary>
        /// Loads argument descriptions from README for the given app
        /// </summary>
        private static async Task LoadArgumentDescriptionsForApp(AppBase app)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LOAD DESCRIPTIONS START for {app.Name} ===");
                System.Diagnostics.Debug.WriteLine($"App CustomName: {app.CustomName}");
                System.Diagnostics.Debug.WriteLine($"App Type: {app.GetType().Name}");
                
                // Check if we already have descriptions cached for this app
                if (argumentDescriptionsCache.ContainsKey(app.Name))
                {
                    System.Diagnostics.Debug.WriteLine($"Found cached descriptions for {app.Name}, but forcing reload to get latest README data");
                    // Remove from cache to force fresh reload
                    argumentDescriptionsCache.Remove(app.Name);
                }

                // Determine the README URL based on app name
                string? readmeUrl = GetReadmeUrlForApp(app.Name);
                System.Diagnostics.Debug.WriteLine($"README URL for {app.Name}: {readmeUrl ?? "NULL"}");
                
                if (!string.IsNullOrEmpty(readmeUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"Loading descriptions for {app.Name} from {readmeUrl}");
                    var argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
                    
                    System.Diagnostics.Debug.WriteLine($"Parser returned {argumentDescriptions.Count} descriptions");
                    
                    // Cache the descriptions
                    argumentDescriptionsCache[app.Name] = argumentDescriptions;
                    System.Diagnostics.Debug.WriteLine($"Cached {argumentDescriptions.Count} descriptions for {app.Name}");
                    
                    // Update the app's argument descriptions
                    if (app.Configuration?.Arguments != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"App has {app.Configuration.Arguments.Count} arguments to update");
                        int updatedCount = 0;
                        
                        foreach (var argument in app.Configuration.Arguments)
                        {
                            System.Diagnostics.Debug.WriteLine($"Checking argument: {argument.Name} (current description: '{argument.Description}')");
                            
                            if (argumentDescriptions.TryGetValue(argument.Name, out var description))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found description for {argument.Name} in parsed data: {description?.Substring(0, Math.Min(50, description?.Length ?? 0))}...");
                                
                                if (!string.IsNullOrEmpty(description))
                                {
                                    // Always update description with parsed one, even if existing description exists
                                    var oldDescription = argument.Description;
                                    argument.Description = description;
                                    updatedCount++;
                                    System.Diagnostics.Debug.WriteLine($"✓ Updated description for argument {argument.Name}");
                                    System.Diagnostics.Debug.WriteLine($"  Old: '{oldDescription}'");
                                    System.Diagnostics.Debug.WriteLine($"  New: '{description}'");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"✗ Skipped update for {argument.Name} (empty parsed description)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"✗ No description found for argument {argument.Name} in parsed data");
                            }
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Updated {updatedCount} argument descriptions for {app.Name}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"App {app.Name} has no Configuration.Arguments to update");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No README URL configured for {app.Name}");
                }
                
                System.Diagnostics.Debug.WriteLine($"=== LOAD DESCRIPTIONS COMPLETE for {app.Name} ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in LoadArgumentDescriptionsForApp for {app.Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Remove from cache if there was an error
                if (argumentDescriptionsCache.ContainsKey(app.Name))
                {
                    argumentDescriptionsCache.Remove(app.Name);
                }
                
                // Continue without descriptions if loading fails
            }
        }

        /// <summary>
        /// Returns the README URL for a given app name
        /// </summary>
        private static string? GetReadmeUrlForApp(string appName)
        {
            return appName switch
            {
                "darts-caller" => "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md",
                "darts-extern" => "https://raw.githubusercontent.com/lbormann/darts-extern/refs/heads/master/README.md",
                "darts-wled" => "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md",
                "darts-pixelit" => "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md",
                "darts-gif" => "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md",
                "darts-voice" => "https://raw.githubusercontent.com/lbormann/darts-voice/refs/heads/main/README.md",
                "cam-loader" => "https://raw.githubusercontent.com/lbormann/cam-loader/refs/heads/master/README.md",
                _ => null
            };
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
                Margin = new Thickness(0, 5, 0, 0),
                Width = 550 // Fixed width for consistency
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

            // Description if available - enhanced with README parsing
            System.Diagnostics.Debug.WriteLine($"Creating description for parameter {param.Name}:");
            System.Diagnostics.Debug.WriteLine($"  param.Description: '{param.Description}'");
            
            if (!string.IsNullOrEmpty(param.Description))
            {
                System.Diagnostics.Debug.WriteLine($"  Using param.Description directly");
                var descText = CreateDescriptionTextBlock(param.Description);
                contentPanel.Children.Add(descText);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  param.Description is empty, checking cache...");
                
                // Try to get description from cache if not already set
                if (argumentDescriptionsCache.TryGetValue(app.Name, out var appDescriptions))
                {
                    System.Diagnostics.Debug.WriteLine($"  Found cache entry for {app.Name} with {appDescriptions.Count} descriptions");
                    
                    if (appDescriptions.TryGetValue(param.Name, out var cachedDescription))
                    {
                        System.Diagnostics.Debug.WriteLine($"  Found cached description for {param.Name}: '{cachedDescription?.Substring(0, Math.Min(50, cachedDescription?.Length ?? 0))}...'");
                        
                        if (!string.IsNullOrEmpty(cachedDescription))
                        {
                            var descText = CreateDescriptionTextBlock(cachedDescription);
                            contentPanel.Children.Add(descText);
                            System.Diagnostics.Debug.WriteLine($"  ✓ Added description TextBlock for {param.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  ✗ Cached description for {param.Name} is empty");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✗ No cached description found for parameter {param.Name}");
                        System.Diagnostics.Debug.WriteLine($"  Available cached parameters: {string.Join(", ", appDescriptions.Keys)}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  ✗ No cache entry found for app {app.Name}");
                    System.Diagnostics.Debug.WriteLine($"  Available cached apps: {string.Join(", ", argumentDescriptionsCache.Keys)}");
                }
            }

            Grid.SetColumn(contentPanel, 0);
            grid.Children.Add(contentPanel);

            // Remove button for non-required parameters
            if (!param.Required)
            {
                var removeButton = new Button
                {
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(3),
                    Width = 25,
                    Height = 25,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Padding = new Thickness(0)
                };

                // Try to use clear.png icon, fallback to text
                try
                {
                    var image = new Avalonia.Controls.Image
                    {
                        Source = new Avalonia.Media.Imaging.Bitmap(
                            Avalonia.Platform.AssetLoader.Open(new Uri("avares://darts-hub/Assets/clear.png"))),
                        Width = 16,
                        Height = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    removeButton.Content = image;
                }
                catch
                {
                    // Fallback to text if image not found
                    removeButton.Content = "✖";
                    removeButton.Foreground = Brushes.White;
                    removeButton.FontSize = 12;
                    removeButton.FontWeight = FontWeight.Bold;
                }

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

        /// <summary>
        /// Creates a formatted description TextBlock with enhanced styling
        /// </summary>
        private static Control CreateDescriptionTextBlock(string description)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 135, 206, 235)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 5, 0, 0)
            };

            var textBlock = new TextBlock
            {
                Text = $"💡 {description}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 240, 255)),
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = textBlock;
            return border;
        }

        private static Control? CreateParameterInputControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var type = param.GetTypeClear();
            
            // Check if this is a score area effect parameter (has special handling with range dropdowns)
            bool isScoreAreaEffectParameter = WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
            
            // Check if this is a regular effect parameter
            bool isEffectParameter = IsEffectParameter(param) && !isScoreAreaEffectParameter;

            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (isScoreAreaEffectParameter)
                    {
                        return WledScoreAreaHelper.CreateScoreAreaEffectParameterControl(param, saveCallback, app);
                    }
                    else if (isEffectParameter)
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

        private static bool IsPresetParameter(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            
            // Check for ps|1, ps|2, etc. format (with or without duration)
            var parts = value.Split('|');
            
            return parts.Length >= 2 && 
                   parts[0].Equals("ps", StringComparison.OrdinalIgnoreCase) && 
                   int.TryParse(parts[1], out _);
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

            // Analyze current value to determine mode and content
            string? currentEffectValue = param.Value;
            bool isManualMode = true;
            
            System.Diagnostics.Debug.WriteLine($"=== EFFECT PARAMETER PARSING START ===");
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
                else if (WledApi.FallbackEffectCategories.SelectMany(kv => kv.Value).Contains(param.Value))
                {
                    modeSelector.SelectedItem = effectsItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: EFFECTS");
                }
                else if (ColorEffects.Contains(param.Value))
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
                    
                    // Initialize effects asynchronously
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var effectControl = await CreateWledEffectsDropdown(param, saveCallback, app);
                            
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                inputContainer.Child = effectControl;
                            });
                        }
                        catch
                        {
                            // Fallback to manual input on error
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                inputContainer.Child = CreateManualEffectInput(param, saveCallback);
                            });
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
                Content = "⏹️",
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

            testButton.Click += async (s, e) =>
            {
                if (effectDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string effect && 
                    app != null)
                {
                    testButton.IsEnabled = false;
                    testButton.Content = "⏳";
                    try
                    {
                        var success = await WledApi.TestEffectAsync(app, effect);
                        if (success)
                        {
                            testButton.Content = "✅";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            testButton.Content = "❌";
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
                    stopButton.Content = "⏳";
                    try
                    {
                        var success = await WledApi.StopEffectsAsync(app);
                        if (success)
                        {
                            stopButton.Content = "✅";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            stopButton.Content = "❌";
                            await Task.Delay(1000);
                        }
                    }
                    finally
                    {
                        stopButton.Content = "⏹️";
                        stopButton.IsEnabled = true;
                    }
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
            panel.Children.Add(testButton);
            panel.Children.Add(stopButton);
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
                Content = "⏹️",
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

            // Parse current value if it contains duration info
            string? selectedPreset = null;
            decimal selectedDuration = 0m; // Default to 0
            
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
                        selectedDuration = Math.Max(0m, Math.Min(60m, parsedDuration)); // Clamp to valid range
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
                    selectedItem.Tag is Argument selectedParam &&
                    durationUpDown.Value.HasValue)
                {
                    isUpdating = true;
                    try
                    {
                        var durationValue = Math.Round(durationUpDown.Value.Value, 0); // Round to whole seconds
                        
                        // If duration is 0, save only the preset (e.g., "ps|1")
                        // If duration > 0, save preset with duration (e.g., "ps|1|5")
                        if (durationValue == 0)
                        {
                            param.Value = selectedItem.Tag.ToString()!;
                        }
                        else
                        {
                            param.Value = $"{selectedItem.Tag.ToString()}|{durationValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
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
                                testButton.Content = "✅";
                                await Task.Delay(1000);
                            }
                            else
                            {
                                testButton.Content = "❌";
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
                            stopButton.Content = "✅";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            stopButton.Content = "❌";
                            await Task.Delay(1000);
                        }
                    }
                    finally
                    {
                        stopButton.Content = "⏹️";
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

        private static Control CreateColorEffectsDropdown(Argument param, Action? saveCallback = null, AppBase? app = null)
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
                Content = "⏹️",
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

            // Event handlers
            testButton.Click += async (s, e) =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect && 
                    app != null)
                {
                    testButton.IsEnabled = false;
                    testButton.Content = "⏳";
                    try
                    {
                        var success = await WledApi.TestColorAsync(app, colorEffect);
                        if (success)
                        {
                            testButton.Content = "✅";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            testButton.Content = "❌";
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
                    stopButton.Content = "⏳";
                    try
                    {
                        var success = await WledApi.StopEffectsAsync(app);
                        if (success)
                        {
                            stopButton.Content = "✅";
                            await Task.Delay(1000);
                        }
                        else
                        {
                            stopButton.Content = "❌";
                            await Task.Delay(1000);
                        }
                    }
                    finally
                    {
                        stopButton.Content = "⏹️";
                        stopButton.IsEnabled = true;
                    }
                }
            };

            colorDropdown.SelectionChanged += (s, e) =>
            {
                if (colorDropdown.SelectedItem is ComboBoxItem selectedItem && 
                    selectedItem.Tag is string colorEffect)
                {
                    param.Value = colorEffect; // Just the color effect name, no duration
                    param.IsValueChanged = true;
                    saveCallback?.Invoke();
                }
            };

            panel.Children.Add(colorDropdown);
            panel.Children.Add(testButton);
            panel.Children.Add(stopButton);
            return panel;
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

            // Get available parameters (not configured and not runtime) grouped by section
            var availableParamsBySection = app.Configuration.Arguments
                .Where(arg => !arg.IsRuntimeArgument && !arg.Required && string.IsNullOrEmpty(arg.Value))
                .GroupBy(arg => arg.Section ?? "General")
                .OrderBy(group => group.Key)
                .ToList();

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
                       "This new settings interface provides real-time parameter management with enhanced descriptions from README files. " +
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