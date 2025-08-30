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
                                    System.Diagnostics.Debug.WriteLine($"? Updated description for argument {argument.Name}");
                                    System.Diagnostics.Debug.WriteLine($"  Old: '{oldDescription}'");
                                    System.Diagnostics.Debug.WriteLine($"  New: '{description}'");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"? Skipped update for {argument.Name} (empty parsed description)");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"? No description found for argument {argument.Name} in parsed data");
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
                Text = $"{app.CustomName} - New Settings Mode",
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
                Content = app.AppRunningState ? "■ Stop" : "▶️ Start",
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
                    Text = "Configured Parameters",
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
                            System.Diagnostics.Debug.WriteLine($"  ? Added description TextBlock for {param.Name}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"  ? Cached description for {param.Name} is empty");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  ? No cached description found for parameter {param.Name}");
                        System.Diagnostics.Debug.WriteLine($"  Available cached parameters: {string.Join(", ", appDescriptions.Keys)}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  ? No cache entry found for app {app.Name}");
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
                    removeButton.Content = "X";
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
                Text = $"{description}",
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
            
            System.Diagnostics.Debug.WriteLine($"=== PARAMETER INPUT CONTROL CREATION ===");
            System.Diagnostics.Debug.WriteLine($"App Name: {app?.Name ?? "NULL"}");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: {param.Name}");
            System.Diagnostics.Debug.WriteLine($"Parameter Type: {type}");
            
            // Check if this is a Pixelit effect parameter (includes both regular and score area effects for darts-pixelit)
            bool isPixelitEffectParameter = PixelitSettings.IsPixelitEffectParameter(param, app);
            System.Diagnostics.Debug.WriteLine($"Is Pixelit Effect Parameter: {isPixelitEffectParameter}");
            
            // Check if this is a WLED score area effect parameter (only for darts-wled, not darts-pixelit)
            bool isWledScoreAreaEffectParameter = app?.Name == "darts-wled" && 
                                                 WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
            System.Diagnostics.Debug.WriteLine($"Is WLED Score Area Effect Parameter: {isWledScoreAreaEffectParameter}");
            
            // Check if this is a WLED regular effect parameter (only for darts-wled, not darts-pixelit, and not score area)
            bool isWledEffectParameter = app?.Name == "darts-wled" && 
                                       WledSettings.IsEffectParameter(param) && 
                                       !isWledScoreAreaEffectParameter;
            System.Diagnostics.Debug.WriteLine($"Is WLED Effect Parameter: {isWledEffectParameter}");

            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    if (isPixelitEffectParameter)
                    {
                        System.Diagnostics.Debug.WriteLine($"CREATING: Pixelit Effect Parameter Control");
                        return PixelitSettings.CreateAdvancedPixelitParameterControl(param, saveCallback, app);
                    }
                    else if (isWledScoreAreaEffectParameter)
                    {
                        System.Diagnostics.Debug.WriteLine($"CREATING: WLED Score Area Effect Parameter Control");
                        return WledScoreAreaHelper.CreateScoreAreaEffectParameterControl(param, saveCallback, app);
                    }
                    else if (isWledEffectParameter)
                    {
                        System.Diagnostics.Debug.WriteLine($"CREATING: WLED Effect Parameter Control");
                        return WledSettings.CreateAdvancedEffectParameterControl(param, saveCallback, app);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"CREATING: Standard TextBox Control");
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
                Text = "+ Add Parameter",
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
                Text = "Configuration Preview",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            var configInfo = new TextBlock
            {
                Text = app.IsConfigurable() ? 
                    $"App has {app.Configuration?.Arguments?.Count ?? 0} configurable options\n" +
                    "Enhanced UI controls active\n" +
                    "Real-time parameter management\n" +
                    "Advanced validation and tooltips" : 
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
                Text = "Enhanced Configuration Mode\n\n" +
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