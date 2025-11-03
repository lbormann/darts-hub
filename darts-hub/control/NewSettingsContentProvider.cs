using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using darts_hub.model;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System;
using darts_hub.control; // Add this using directive to enable access to MainWindow

namespace darts_hub.control
{
    /// <summary>
    /// New settings content mode for enhanced app configuration
    /// </summary>
    public class NewSettingsContentProvider
    {
        private static readonly ReadmeParser readmeParser = new ReadmeParser();
        private static readonly Dictionary<string, Dictionary<string, string>> argumentDescriptionsCache = new();

        // Default color configurations for WLED apps
        public const string DEFAULT_WLED_COLOR = "blue";
        public const string DEFAULT_WLED_SCORE_AREA_COLOR = "green";

        // Specific parameter color mappings - extend this for more parameters
        public static readonly Dictionary<string, string> ParameterColorDefaults = new()
        {
            // WLED specific parameters
            { "TOE", "yellow" },     // Turn on effect
            { "CE", "pink" },        // Calibration effect  
            { "IDE", "green" },  // Idle effect
            { "IDE2", "lightcoral" }, // Player 2 idle
            { "IDE3", "lightgoldenrod1" }, // Player 3 idle
            { "IDE4", "lightyellow1" }, // Player 4 idle
            { "IDE5", "lightpink" },  // Player 5 idle
            { "IDE6", "lightcyan1" },  // Player 6 idle
            { "B", "red3" },  // Busted effect
            { "BSE", "red1" },  // Bord Stop effect
            
            // Score area effects (A1-A12)
            { "A1", "red1" },
            { "A2", "green1" },
            { "A3", "blue1" },
            { "A4", "yellow1" },
            { "A5", "purple1" },
            { "A6", "orange1" },
            { "A7", "pink1" },
            { "A8", "cyan" },
            { "A9", "magenta" },
            { "A10", "lime" },
            { "A11", "violet" },
            { "A12", "turquoise" },
            
            // General fallbacks
            { "EFFECT", "blue" },
            { "COLOR", "white" },
        };

        public static readonly List<string> ColorEffects = new List<string>
        {
            "aliceblue",
            "antiquewhite", "antiquewhite1", "antiquewhite2", "antiquewhite3", "antiquewhite4",
            "aqua", "aquamarine1", "aquamarine2", "aquamarine3", "aquamarine4",
            "azure1", "azure2", "azure3", "azure4",
            "banana",
            "beige",
            "bisque1", "bisque2", "bisque3", "bisque4",
            "black",
            "blanchedalmond",
            "blue", "blue2", "blue3", "blue4", "blueviolet",
            "brick",
            "brown", "brown1", "brown2", "brown3", "brown4",
            "burlywood", "burlywood1", "burlywood2", "burlywood3", "burlywood4",
            "burntsienna", "burntumber",
            "cadetblue", "cadetblue1", "cadetblue2", "cadetblue3", "cadetblue4",
            "cadmiumorange", "cadmiumyellow",
            "carrot",
            "chartreuse1", "chartreuse2", "chartreuse3", "chartreuse4",
            "chocolate", "chocolate1", "chocolate2", "chocolate3", "chocolate4",
            "cobalt", "cobaltgreen",
            "coldgrey",
            "coral", "coral1", "coral2", "coral3", "coral4",
            "cornflowerblue",
            "cornsilk1", "cornsilk2", "cornsilk3", "cornsilk4",
            "crimson",
            "cyan2", "cyan3", "cyan4",
            "darkgoldenrod", "darkgoldenrod1", "darkgoldenrod2", "darkgoldenrod3", "darkgoldenrod4",
            "darkgray", "darkgreen", "darkkhaki", "darkolivegreen", "darkolivegreen1", "darkolivegreen2", "darkolivegreen3", "darkolivegreen4", "darkorange", "darkorange1", "darkorange2", "darkorange3", "darkorange4", "darkorchid", "darkorchid1", "darkorchid2", "darkorchid3", "darkorchid4", "darksalmon", "darkseagreen", "darkseagreen1", "darkseagreen2", "darkseagreen3", "darkseagreen4", "darkslateblue", "darkslategray", "darkslategray1", "darkslategray2", "darkslategray3", "darkslategray4", "darkturquoise", "darkviolet",
            "deeppink1", "deeppink2", "deeppink3", "deeppink4",
            "deepskyblue1", "deepskyblue2", "deepskyblue3", "deepskyblue4",
            "dimgray",
            "dodgerblue1", "dodgerblue2", "dodgerblue3", "dodgerblue4",
            "eggshell",
            "emeraldgreen",
            "firebrick", "firebrick1", "firebrick2", "firebrick3", "firebrick4",
            "flesh",
            "floralwhite",
            "forestgreen",
            "gainsboro",
            "ghostwhite",
            "gold1", "gold2", "gold3", "gold4", "goldenrod", "goldenrod1", "goldenrod2", "goldenrod3", "goldenrod4",
            "gray", "gray1", "gray2", "gray3", "gray4", "gray5", "gray6", "gray7", "gray8", "gray9", "gray10", "gray11", "gray12", "gray13", "gray14", "gray15", "gray16", "gray17", "gray18", "gray19", "gray20", "gray21", "gray22", "gray23", "gray24", "gray25", "gray26", "gray27", "gray28", "gray29", "gray30", "gray31", "gray32", "gray33", "gray34", "gray35", "gray36", "gray37", "gray38", "gray39", "gray40", "gray42", "gray43", "gray44", "gray45", "gray46", "gray47", "gray48", "gray49", "gray50", "gray51", "gray52", "gray53", "gray54", "gray55", "gray56", "gray57", "gray58", "gray59", "gray60", "gray61", "gray62", "gray63", "gray64", "gray65", "gray66", "gray67", "gray68", "gray69", "gray70", "gray71", "gray72", "gray73", "gray74", "gray75", "gray76", "gray77", "gray78", "gray79", "gray80", "gray81", "gray82", "gray83", "gray84", "gray85", "gray86", "gray87", "gray88", "gray89", "gray90", "gray91", "gray92", "gray93", "gray94", "gray95", "gray97", "gray98", "gray99",
            "green", "green1", "green2", "green3", "green4", "greenyellow",
            "honeydew1", "honeydew2", "honeydew3", "honeydew4",
            "hotpink", "hotpink1", "hotpink2", "hotpink3", "hotpink4",
            "indianred", "indianred1", "indianred2", "indianred3", "indianred4",
            "indigo",
            "ivory1", "ivory2", "ivory3", "ivory4", "ivoryblack",
            "khaki", "khaki1", "khaki2", "khaki3", "khaki4",
            "lavender", "lavenderblush1", "lavenderblush2", "lavenderblush3", "lavenderblush4",
            "lawngreen",
            "lemonchiffon1", "lemonchiffon2", "lemonchiffon3", "lemonchiffon4",
            "lightblue", "lightblue1", "lightblue2", "lightblue3", "lightblue4", "lightcoral", "lightcyan1", "lightcyan2", "lightcyan3", "lightcyan4", "lightgoldenrod1", "lightgoldenrod2", "lightgoldenrod3", "lightgoldenrod4", "lightgoldenrodyellow", "lightgrey", "lightpink", "lightpink1", "lightpink2", "lightpink3", "lightpink4", "lightsalmon1", "lightsalmon2", "lightsalmon3", "lightsalmon4", "lightseagreen", "lightskyblue", "lightskyblue1", "lightskyblue2", "lightskyblue3", "lightskyblue4", "lightslateblue", "lightslategray", "lightsteelblue", "lightsteelblue1", "lightsteelblue2", "lightsteelblue3", "lightsteelblue4", "lightyellow1", "lightyellow2", "lightyellow3", "lightyellow4",
            "limegreen",
            "linen",
            "magenta", "magenta2", "magenta3", "magenta4",
            "manganeseblue",
            "maroon", "maroon1", "maroon2", "maroon3", "maroon4",
            "mediumorchid", "mediumorchid1", "mediumorchid2", "mediumorchid3", "mediumorchid4", "mediumpurple", "mediumpurple1", "mediumpurple2", "mediumpurple3", "mediumpurple4", "mediumseagreen", "mediumslateblue", "mediumspringgreen", "mediumturquoise", "mediumvioletred",
            "melon",
            "midnightblue",
            "mint", "mintcream",
            "mistyrose1", "mistyrose2", "mistyrose3", "mistyrose4",
            "moccasin",
            "navajowhite1", "navajowhite2", "navajowhite3", "navajowhite4",
            "navy",
            "oldlace",
            "olive", "olivedrab", "olivedrab1", "olivedrab2", "olivedrab3", "olivedrab4",
            "orange", "orange1", "orange2", "orange3", "orange4", "orangered1", "orangered2", "orangered3", "orangered4",
            "orchid", "orchid1", "orchid2", "orchid3", "orchid4",
            "palegoldenrod", "palegreen", "palegreen1", "palegreen2", "palegreen3", "palegreen4", "paleturquoise1", "paleturquoise2", "paleturquoise3", "paleturquoise4", "palevioletred", "palevioletred1", "palevioletred2", "palevioletred3", "palevioletred4",
            "papayawhip",
            "peachpuff1", "peachpuff2", "peachpuff3", "peachpuff4",
            "peacock",
            "pink", "pink1", "pink2", "pink3", "pink4",
            "plum", "plum1", "plum2", "plum3", "plum4",
            "powderblue",
            "purple", "purple1", "purple2", "purple3", "purple4",
            "raspberry",
            "rawsienna",
            "red1", "red2", "red3", "red4",
            "rosybrown", "rosybrown1", "rosybrown2", "rosybrown3", "rosybrown4",
            "royalblue", "royalblue1", "royalblue2", "royalblue3", "royalblue4",
            "salmon", "salmon1", "salmon2", "salmon3", "salmon4",
            "sandybrown",
            "sapgreen",
            "seagreen1", "seagreen2", "seagreen3", "seagreen4",
            "seashell1", "seashell2", "seashell3", "seashell4",
            "sepia",
            "sgibeet", "sgibrightgray", "sgichartreuse", "sgidarkgray", "sgigray12", "sgigray16", "sgigray32", "sgigray36", "sgigray52", "sgigray56", "sgigray72", "sgigray76", "sgigray92", "sgigray96", "sgilightblue", "sgilightgray", "sgiolivedrab", "sgisalmon", "sgislateblue", "sgiteal",
            "sienna", "sienna1", "sienna2", "sienna3", "sienna4",
            "silver",
            "skyblue", "skyblue1", "skyblue2", "skyblue3", "skyblue4",
            "slateblue", "slateblue1", "slateblue2", "slateblue3", "slateblue4", "slategray", "slategray1", "slategray2", "slategray3", "slategray4",
            "snow1", "snow2", "snow3", "snow4",
            "springgreen", "springgreen1", "springgreen2", "springgreen3",
            "steelblue", "steelblue1", "steelblue2", "steelblue3", "steelblue4",
            "tan", "tan1", "tan2", "tan3", "tan4",
            "teal",
            "thistle", "thistle1", "thistle2", "thistle3", "thistle4",
            "tomato1", "tomato2", "tomato3", "tomato4",
            "turquoise", "turquoise1", "turquoise2", "turquoise3", "turquoise4", "turquoiseblue",
            "violet", "violetred", "violetred1", "violetred2", "violetred3", "violetred4",
            "warmgrey",
            "wheat", "wheat1", "wheat2", "wheat3", "wheat4",
            "white", "whitesmoke",
            "yellow1", "yellow2", "yellow3", "yellow4"
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
        /// <param name="saveCallback">Callback to save changes</param>
        /// <param name="selectedProfile">The currently selected profile (for autostart functionality)</param>
        /// <returns>A control containing the new settings UI</returns>
        public static async Task<Control> CreateNewSettingsContent(AppBase app, Action? saveCallback = null, Profile? selectedProfile = null)
        {
            System.Diagnostics.Debug.WriteLine($"=== CREATE NEW SETTINGS CONTENT START ===");
            System.Diagnostics.Debug.WriteLine($"App: {app.Name} ({app.CustomName})");
            System.Diagnostics.Debug.WriteLine($"App type: {app.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Selected profile: {selectedProfile?.Name ?? "NULL"}");
            
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

            // Custom Name section
            var customNameSection = CreateCustomNameSection(app, saveCallback);
            mainPanel.Children.Add(customNameSection);

            // Enable at startup section - NEW!
            var autostartSection = CreateAutostartSection(app, saveCallback, selectedProfile);
            mainPanel.Children.Add(autostartSection);

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
                // ⭐ Prevent multiple clicks
                if (startStopButton.Tag?.ToString() == "processing") return;
                startStopButton.Tag = "processing";
                var originalIsEnabled = startStopButton.IsEnabled;
                startStopButton.IsEnabled = false;
                
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
                finally
                {
                    // Restore button state after a delay to prevent rapid clicking
                    Task.Delay(1000).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            startStopButton.Tag = null;
                            startStopButton.IsEnabled = originalIsEnabled;
                        });
                    });
                }
            };

            restartButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (restartButton.Tag?.ToString() == "processing") return;
                restartButton.Tag = "processing";
                var originalIsEnabled = restartButton.IsEnabled;
                restartButton.IsEnabled = false;
                
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
                finally
                {
                    // Restore button state after a delay to prevent rapid clicking
                    Task.Delay(2000).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            restartButton.Tag = null;
                            restartButton.IsEnabled = originalIsEnabled;
                        });
                    });
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

                removeButton.Click += (sender, e) =>
                {
                    // ⭐ Prevent multiple clicks
                    if (removeButton.Tag?.ToString() == "processing") return;
                    removeButton.Tag = "processing";
                    removeButton.IsEnabled = false;
                    
                    try
                    {
                        param.Value = null;
                        // Mark as changed for saving
                        param.IsValueChanged = true;
                        // Trigger auto-save
                        saveCallback?.Invoke();
                        
                        System.Diagnostics.Debug.WriteLine($"=== REMOVE PARAMETER CLICK ===");
                        System.Diagnostics.Debug.WriteLine($"Removing parameter: {param.Name}");
                        
                        // Find the main panel by traversing up to the root
                        var current = removeButton.Parent;
                        StackPanel? rootMainPanel = null;
                        
                        // Traverse up to find the main panel that contains the header
                        while (current != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Checking parent: {current.GetType().Name}");
                            
                            if (current is StackPanel stackPanel)
                            {
                                // Look for the root main panel that contains the header
                                var hasSettingsHeader = stackPanel.Children.OfType<StackPanel>()
                                    .Any(sp => sp.Children.OfType<TextBlock>()
                                           .Any(tb => tb.Text?.Contains("Settings Mode") == true));
                                
                                System.Diagnostics.Debug.WriteLine($"  Has settings header: {hasSettingsHeader}");
                                
                                if (hasSettingsHeader)
                                {
                                    rootMainPanel = stackPanel;
                                    System.Diagnostics.Debug.WriteLine($"✓ Found root main panel");
                                    break;
                                }
                            }
                            current = current.Parent;
                        }
                        
                        if (rootMainPanel != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Triggering section refresh for parameter removal");
                            // Use the same force refresh method for consistency
                            ForceCompleteSettingsRefresh(param, app, rootMainPanel, saveCallback);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ERROR: Could not find root main panel for parameter removal");
                        }
                    }
                    finally
                    {
                        // Reset processing flag (even though UI will be refreshed)
                        removeButton.Tag = null;
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
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");
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
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        FontSize = 13
                    };

                    // Apply type-based range constraints
                    if (ArgumentTypeHelper.TryGetNumericRange(param, out var minInt, out var maxInt))
                    {
                        intUpDown.Minimum = minInt;
                        intUpDown.Maximum = maxInt;
                        System.Diagnostics.Debug.WriteLine($"[NewSettings] Applied int range constraints to {param.Name}: Min={minInt}, Max={maxInt}");
                    }
                    else
                    {
                        // Fallback to reasonable defaults
                        intUpDown.Minimum = int.MinValue;
                        intUpDown.Maximum = int.MaxValue;
                    }

                    // Set increment step based on type
                    intUpDown.Increment = ArgumentTypeHelper.GetIncrementStep(param);
                    intUpDown.FormatString = ArgumentTypeHelper.GetFormatString(param);

                    intUpDown.ValueChanged += (s, e) =>
                    {
                        if (intUpDown.Value.HasValue)
                        {
                            param.Value = ((int)intUpDown.Value.Value).ToString();
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        }
                    };
                    return intUpDown;

                case Argument.TypeFloat:
                    var floatUpDown = new NumericUpDown
                    {
                        Value = double.TryParse(param.Value, System.Globalization.NumberStyles.Float, 
                                System.Globalization.CultureInfo.InvariantCulture, out var doubleVal) ? (decimal)doubleVal : 0,
                        Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        FontSize = 13
                    };

                    // Apply type-based range constraints
                    if (ArgumentTypeHelper.TryGetNumericRange(param, out var minFloat, out var maxFloat))
                    {
                        floatUpDown.Minimum = minFloat;
                        floatUpDown.Maximum = maxFloat;
                        System.Diagnostics.Debug.WriteLine($"[NewSettings] Applied float range constraints to {param.Name}: Min={minFloat}, Max={maxFloat}");
                    }
                    else
                    {
                        // Fallback to reasonable defaults
                        floatUpDown.Minimum = decimal.MinValue;
                        floatUpDown.Maximum = decimal.MaxValue;
                    }

                    // Set increment step and format based on type
                    floatUpDown.Increment = ArgumentTypeHelper.GetIncrementStep(param);
                    floatUpDown.FormatString = ArgumentTypeHelper.GetFormatString(param);

                    floatUpDown.ValueChanged += (s, e) =>
                    {
                        if (floatUpDown.Value.HasValue)
                        {
                            param.Value = floatUpDown.Value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            param.IsValueChanged = true;
                            saveCallback?.Invoke();
                        }
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
                        Width = 60,
                        Height = 30,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    fileTextBox.TextChanged += (s, e) =>
                    {
                        param.Value = fileTextBox.Text;
                        param.IsValueChanged = true;
                        saveCallback?.Invoke();
                    };

                    // Add functionality to the browse button
                    browseButton.Click += async (s, e) =>
                    {
                        // ⭐ Prevent multiple clicks
                        if (browseButton.Tag?.ToString() == "processing") return;
                        browseButton.Tag = "processing";
                        browseButton.IsEnabled = false;
                        
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"Browse button clicked for parameter: {param.Name}, type: {type}");
                            
                            // Get the top level window for the dialog
                            var topLevel = TopLevel.GetTopLevel(browseButton);
                            if (topLevel is not Window window)
                            {
                                System.Diagnostics.Debug.WriteLine("Could not find parent window for file dialog");
                                return;
                            }

                            string? result = null;

                            if (type == Argument.TypePath)
                            {
                                // Open folder dialog for path selection
                                System.Diagnostics.Debug.WriteLine("Opening folder dialog...");
                                var folderDialog = new OpenFolderDialog
                                {
                                    Title = $"Select folder for {param.NameHuman ?? param.Name}"
                                };

                                // Set initial directory if current value exists and is valid
                                if (!string.IsNullOrEmpty(param.Value) && System.IO.Directory.Exists(param.Value))
                                {
                                    folderDialog.Directory = param.Value;
                                }

                                result = await folderDialog.ShowAsync(window);
                                System.Diagnostics.Debug.WriteLine($"Folder dialog result: {result ?? "CANCELLED"}");
                            }
                            else // Argument.TypeFile
                            {
                                // Open file dialog for file selection
                                System.Diagnostics.Debug.WriteLine("Opening file dialog...");
                                var fileDialog = new OpenFileDialog
                                {
                                    Title = $"Select file for {param.NameHuman ?? param.Name}",
                                    AllowMultiple = false
                                };

                                // Set initial directory if current value exists
                                if (!string.IsNullOrEmpty(param.Value))
                                {
                                    try
                                    {
                                        var directory = System.IO.Path.GetDirectoryName(param.Value);
                                        if (!string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory))
                                        {
                                            fileDialog.Directory = directory;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Error setting initial directory: {ex.Message}");
                                    }
                                }

                                var fileResults = await fileDialog.ShowAsync(window);
                                result = fileResults?.FirstOrDefault();
                                System.Diagnostics.Debug.WriteLine($"File dialog result: {result ?? "CANCELLED"}");
                            }

                            // Update the textbox and parameter if a selection was made
                            if (!string.IsNullOrEmpty(result))
                            {
                                fileTextBox.Text = result;
                                param.Value = result;
                                param.IsValueChanged = true;
                                saveCallback?.Invoke();
                                System.Diagnostics.Debug.WriteLine($"Updated parameter {param.Name} with value: {result}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in browse button click: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        }
                        finally
                        {
                            // Re-enable button after operation
                            browseButton.Tag = null;
                            browseButton.IsEnabled = true;
                        }
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

            addButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (addButton.Tag?.ToString() == "processing") return;
                addButton.Tag = "processing";
                addButton.IsEnabled = false;
                
                try
                {
                    if (paramDropdown.SelectedItem is ComboBoxItem selectedItem && 
                        selectedItem.Tag is Argument selectedParam)
                    {
                        System.Diagnostics.Debug.WriteLine($"=== ADD PARAMETER BUTTON CLICKED ===");
                        System.Diagnostics.Debug.WriteLine($"Adding parameter: {selectedParam.Name}");
                        System.Diagnostics.Debug.WriteLine($"Parameter section: {selectedParam.Section ?? "General"}");
                        
                        // Set a default value to make it "configured"
                        selectedParam.Value = GetDefaultValueForParameter(selectedParam.GetTypeClear(), selectedParam, app);
                        selectedParam.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"Set parameter value to: {selectedParam.Value}");

                        // Trigger auto-save
                        saveCallback?.Invoke();
                        System.Diagnostics.Debug.WriteLine("Triggered auto-save");

                        // Find the REAL main panel by traversing up to the root (like we do in remove button)
                        System.Diagnostics.Debug.WriteLine("Finding REAL main panel...");
                        var current = addButton.Parent;
                        StackPanel? realMainPanel = null;
                        
                        // Traverse up to find the main panel that contains the header
                        while (current != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Checking parent: {current.GetType().Name}");
                            
                            if (current is StackPanel stackPanel)
                            {
                                // Look for the root main panel that contains the header
                                var hasSettingsHeader = stackPanel.Children.OfType<StackPanel>()
                                    .Any(sp => sp.Children.OfType<TextBlock>()
                                           .Any(tb => tb.Text?.Contains("Settings Mode") == true));
                                
                                System.Diagnostics.Debug.WriteLine($"  Has settings header: {hasSettingsHeader}");
                                
                                if (hasSettingsHeader)
                                {
                                    realMainPanel = stackPanel;
                                    System.Diagnostics.Debug.WriteLine($"✓ Found real main panel");
                                    break;
                                }
                            }
                            current = current.Parent;
                        }
                        
                        if (realMainPanel != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Triggering section refresh for parameter addition");
                            // Use the same force refresh method for consistency
                            ForceCompleteSettingsRefresh(selectedParam, app, realMainPanel, saveCallback);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ERROR: Could not find real main panel for parameter addition");
                        }
                    }
                }
                finally
                {
                    // Reset processing flag (even though UI will be refreshed)
                    addButton.Tag = null;
                }
            };

            dropdownPanel.Children.Add(paramDropdown);
            dropdownPanel.Children.Add(addButton);
            sectionPanel.Children.Add(dropdownPanel);

            return sectionPanel;
        }

        /// <summary>
        /// Forces a complete settings refresh by rebuilding the entire UI
        /// </summary>
        private static void ForceCompleteSettingsRefresh(Argument changedParam, AppBase app, StackPanel mainPanel, Action? saveCallback)
        {
            System.Diagnostics.Debug.WriteLine($"=== FORCE COMPLETE SETTINGS REFRESH ===");
            System.Diagnostics.Debug.WriteLine($"Changed parameter: {changedParam.Name} (Section: {changedParam.Section ?? "General"})");
            System.Diagnostics.Debug.WriteLine($"Main panel type: {mainPanel.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"Main panel children count: {mainPanel.Children.Count}");
            
            try
            {
                // Verify this is the correct main panel by checking for expected structure
                bool hasExpectedStructure = mainPanel.Children.OfType<StackPanel>()
                    .Any(sp => sp.Children.OfType<TextBlock>()
                           .Any(tb => tb.Text?.Contains("Settings Mode") == true));
                
                System.Diagnostics.Debug.WriteLine($"Main panel has expected structure: {hasExpectedStructure}");
                
                if (!hasExpectedStructure)
                {
                    System.Diagnostics.Debug.WriteLine("WARNING: Main panel does not have expected structure!");
                    
                    // Try to find the real main panel
                    var realMainPanel = FindRealMainPanel(mainPanel);
                    if (realMainPanel != null && realMainPanel != mainPanel)
                    {
                        System.Diagnostics.Debug.WriteLine("Found different real main panel, using that instead");
                        ForceCompleteSettingsRefresh(changedParam, app, realMainPanel, saveCallback);
                        return;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Could not find better main panel, proceeding with current one");
                    }
                }

                // Try to get the selected profile for autostart functionality
                Profile? selectedProfile = null;
                try
                {
                    if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        if (desktop.MainWindow is MainWindow mainWindow)
                        {
                            selectedProfile = mainWindow.SelectedProfile;
                            System.Diagnostics.Debug.WriteLine($"Retrieved selected profile: {selectedProfile?.Name ?? "NULL"}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not get selected profile: {ex.Message}");
                }
                
                // Clear the entire main panel
                mainPanel.Children.Clear();
                System.Diagnostics.Debug.WriteLine("Cleared entire main panel");
                
                // Recreate header
                var headerPanel = CreateHeaderPanel(app);
                mainPanel.Children.Add(headerPanel);
                System.Diagnostics.Debug.WriteLine("Added header panel");
                
                // Recreate custom name section
                var customNameSection = CreateCustomNameSection(app, saveCallback);
                mainPanel.Children.Add(customNameSection);
                System.Diagnostics.Debug.WriteLine("Added custom name section");
                
                // Recreate autostart section
                var autostartSection = CreateAutostartSection(app, saveCallback, selectedProfile);
                mainPanel.Children.Add(autostartSection);
                System.Diagnostics.Debug.WriteLine("Added autostart section");
                
                // Recreate status section
                var statusSection = CreateStatusSection(app);
                mainPanel.Children.Add(statusSection);
                System.Diagnostics.Debug.WriteLine("Added status section");
                
                // Recreate actions section
                var actionsSection = CreateQuickActionsSection(app);
                mainPanel.Children.Add(actionsSection);
                System.Diagnostics.Debug.WriteLine("Added actions section");
                
                // Recreate configured parameters section (this will show the new parameter)
                var configuredSection = CreateConfiguredParametersSection(app, saveCallback);
                mainPanel.Children.Add(configuredSection);
                System.Diagnostics.Debug.WriteLine("Added configured parameters section");
                
                // Recreate add parameter section (this will have updated dropdowns)
                var addParameterSection = CreateAddParameterSection(app, mainPanel, saveCallback);
                mainPanel.Children.Add(addParameterSection);
                System.Diagnostics.Debug.WriteLine("Added add parameter section");
                
                // Recreate beta notice
                var betaNotice = CreateBetaNotice();
                mainPanel.Children.Add(betaNotice);
                System.Diagnostics.Debug.WriteLine("Added beta notice");
                
                System.Diagnostics.Debug.WriteLine($"✓ Force complete settings refresh successful, final children count: {mainPanel.Children.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR during force complete settings refresh: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Tries to find the real main panel by traversing the UI tree
        /// </summary>
        private static StackPanel? FindRealMainPanel(StackPanel startPanel)
        {
            System.Diagnostics.Debug.WriteLine($"=== FINDING REAL MAIN PANEL ===");
            System.Diagnostics.Debug.WriteLine($"Starting from panel: {startPanel.GetType().Name}");
            
            // First, traverse up to find the topmost parent
            var current = startPanel.Parent;
            StackPanel? rootCandidate = startPanel;
            
            while (current != null)
            {
                System.Diagnostics.Debug.WriteLine($"Traversing up: {current.GetType().Name}");
                
                if (current is StackPanel parentStackPanel)
                {
                    rootCandidate = parentStackPanel;
                }
                current = current.Parent;
            }
            
            System.Diagnostics.Debug.WriteLine($"Root candidate: {rootCandidate?.GetType().Name}");
            
            // Now traverse down from the root to find the main panel with settings content
            return FindMainPanelInHierarchy(rootCandidate);
        }

        /// <summary>
        /// Recursively searches for the main panel with settings mode content
        /// </summary>
        private static StackPanel? FindMainPanelInHierarchy(Control? control)
        {
            if (control == null) return null;
            
            if (control is StackPanel stackPanel)
            {
                // Check if this panel has the expected structure
                bool hasExpectedStructure = stackPanel.Children.OfType<StackPanel>()
                    .Any(sp => sp.Children.OfType<TextBlock>()
                           .Any(tb => tb.Text?.Contains("Settings Mode") == true));
                
                if (hasExpectedStructure)
                {
                    System.Diagnostics.Debug.WriteLine($"Found main panel with expected structure");
                    return stackPanel;
                }
                
                // Recursively search children
                foreach (var child in stackPanel.Children)
                {
                    var found = FindMainPanelInHierarchy(child);
                    if (found != null) return found;
                }
            }
            else if (control is ContentPresenter presenter && presenter.Content is Control content)
            {
                return FindMainPanelInHierarchy(content);
            }
            else if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindMainPanelInHierarchy(child);
                    if (found != null) return found;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Gets a default value for a parameter type
        /// </summary>
        private static string GetDefaultValueForParameter(string type)
        {
            return type switch
            {
                Argument.TypeBool => "False",
                Argument.TypeInt => "0",
                Argument.TypeFloat => "0.0",
                _ => "change to activate" // String, Password, File, Path get a non-empty default value
            };
        }

        /// <summary>
        /// Gets a default value for a parameter type with special handling for WLED color effects
        /// </summary>
        private static string GetDefaultValueForParameter(string type, Argument? param = null, AppBase? app = null)
        {
            // Special handling for WLED color effect parameters
            if (app?.Name == "darts-wled" && param != null)
            {
                // First check if we have a specific color mapping for this parameter
                if (ParameterColorDefaults.TryGetValue(param.Name, out var specificColor))
                {
                    System.Diagnostics.Debug.WriteLine($"Using specific color '{specificColor}' for parameter '{param.Name}'");
                    return $"solid|{specificColor}";
                }
                
                // Check if it's a score area effect parameter
                if (WledScoreAreaHelper.IsScoreAreaEffectParameter(param))
                {
                    return $"solid|{DEFAULT_WLED_SCORE_AREA_COLOR}"; // Green for score areas
                }
                // Check if it's a regular effect parameter
                else if (WledSettings.IsEffectParameter(param))
                {
                    return $"solid|{DEFAULT_WLED_COLOR}"; // Blue for regular effects
                }
            }

            // Special handling for other apps that might use color effects
            if (param != null && (param.Name.ToLower().Contains("color") || param.Name.ToLower().Contains("effect")))
            {
                // Check if we have a specific color mapping for this parameter
                if (ParameterColorDefaults.TryGetValue(param.Name, out var specificColor))
                {
                    System.Diagnostics.Debug.WriteLine($"Using specific color '{specificColor}' for parameter '{param.Name}' in app '{app?.Name}'");
                    return $"solid|{specificColor}";
                }
            }

            return type switch
            {
                Argument.TypeBool => "False",
                Argument.TypeInt => "0",
                Argument.TypeFloat => "0.0",
                _ => "change to activate" // String, Password, File, Path get a non-empty default value
            };
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

        /// <summary>
        /// Creates a section for editing the CustomName of the app
        /// </summary>
        private static Control CreateCustomNameSection(AppBase app, Action? saveCallback = null)
        {
            // Check if this app should allow CustomName editing
            var protectedAppNames = new[] { "darts-caller", "darts-wled", "darts-voice", "darts-gif", "darts-pixelit", "cam-loader" };
            bool isProtectedApp = protectedAppNames.Contains(app.Name, StringComparer.OrdinalIgnoreCase);
            
            if (isProtectedApp)
            {
                // For protected apps, show a read-only display name section
                var readOnlyPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 108, 117, 125)), // Gray background for read-only
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 15),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MaxWidth = 650
                };

                var readOnlyContent = new StackPanel();

                var readOnlyTitle = new TextBlock
                {
                    Text = "Application Name",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };

                var readOnlyName = new TextBlock
                {
                    Text = app.CustomName ?? app.Name,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    Margin = new Thickness(0, 0, 0, 5),
                    TextWrapping = TextWrapping.Wrap
                };

                var readOnlyInfo = new TextBlock
                {
                    Text = "The name of this official darts extension cannot be modified to maintain consistency.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    FontStyle = FontStyle.Italic,
                    Margin = new Thickness(0, 5, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };

                readOnlyContent.Children.Add(readOnlyTitle);
                readOnlyContent.Children.Add(readOnlyName);
                readOnlyContent.Children.Add(readOnlyInfo);
                readOnlyPanel.Child = readOnlyContent;
                
                return readOnlyPanel;
            }

            // For non-protected apps, show the editable CustomName section
            var customNamePanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 193, 7)), // Amber background
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var customNameTitle = new TextBlock
            {
                Text = "Custom Display Name",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)), // Dark text for amber background
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            // Create input grid
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var customNameTextBox = new TextBox
            {
                Text = app.CustomName ?? app.Name,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)), // Dark gray background like other textboxes
                Foreground = Brushes.White, // White text like other textboxes
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8), // Consistent padding with other textboxes
                CornerRadius = new CornerRadius(3), // Consistent corner radius with other textboxes
                FontSize = 13, // Consistent font size with other textboxes
                MaxLength = 50, // Reasonable limit for display names
                Watermark = "Enter custom display name..."
            };

            // Fix focus/caret issues with consistent dark theme styling
            customNameTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 123, 255)); // Blue selection
            customNameTextBox.CaretBrush = Brushes.White; // White caret for dark background
            
            // Enhanced focus behavior for dark theme consistency
            customNameTextBox.GotFocus += (s, e) =>
            {
                // Keep consistent dark theme when focused
                customNameTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                customNameTextBox.Foreground = Brushes.White;
                customNameTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 123, 255)); // Blue border when focused
                customNameTextBox.CaretBrush = Brushes.White;
                customNameTextBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 123, 255));
            };
            
            customNameTextBox.LostFocus += (s, e) =>
            {
                // Restore normal dark theme styling when focus is lost
                customNameTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                customNameTextBox.Foreground = Brushes.White;
                customNameTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100));
            };

            // Also handle pointer events to ensure consistent dark theme
            customNameTextBox.PointerEntered += (s, e) =>
            {
                // Keep dark theme on hover
                customNameTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                customNameTextBox.Foreground = Brushes.White;
            };

            customNameTextBox.PointerExited += (s, e) =>
            {
                // Keep dark theme after hover
                customNameTextBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                customNameTextBox.Foreground = Brushes.White;
            };

            var resetButton = new Button
            {
                Content = "🔄",
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                Width = 35,
                Height = 35,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            ToolTip.SetTip(resetButton, "Reset to original name");

            // Info text - adjusted for dark theme context
            var infoText = new TextBlock
            {
                Text = $"Customize how this app appears in the interface. Original name: '{app.Name}'",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)), // Dark text for amber background
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
                    
                    // Trigger save callback
                    saveCallback?.Invoke();
                    
                    // Update info text to show current vs original
                    infoText.Text = $"Custom name: '{newName}' (Original: '{app.Name}')";
                }
                else if (string.IsNullOrEmpty(newName))
                {
                    // Reset to original name if empty
                    app.CustomName = app.Name;
                    customNameTextBox.Text = app.Name;
                    infoText.Text = $"Using original name: '{app.Name}'";
                    saveCallback?.Invoke();
                }
            };

            resetButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (resetButton.Tag?.ToString() == "processing") return;
                resetButton.Tag = "processing";
                resetButton.IsEnabled = false;
                
                try
                {
                    var originalName = app.Name;
                    customNameTextBox.Text = originalName;
                    app.CustomName = originalName;
                    infoText.Text = $"Reset to original name: '{originalName}'";
                    
                    System.Diagnostics.Debug.WriteLine($"CustomName reset to original '{originalName}' for app '{app.Name}'");
                    
                    // Trigger save callback
                    saveCallback?.Invoke();
                }
                finally
                {
                    // Re-enable button after short delay
                    Task.Delay(500).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            resetButton.Tag = null;
                            resetButton.IsEnabled = true;
                        });
                    });
                }
            };

            // Assemble the UI
            Grid.SetColumn(customNameTextBox, 0);
            Grid.SetColumn(resetButton, 1);
            inputGrid.Children.Add(customNameTextBox);
            inputGrid.Children.Add(resetButton);

            contentPanel.Children.Add(customNameTitle);
            contentPanel.Children.Add(inputGrid);
            contentPanel.Children.Add(infoText);

            customNamePanel.Child = contentPanel;
            return customNamePanel;
        }

        /// <summary>
        /// Creates the autostart section for controlling whether the app starts with the profile
        /// </summary>
        private static Control CreateAutostartSection(AppBase app, Action? saveCallback = null, Profile? selectedProfile = null)
        {
            var autostartPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 99, 71)), // Orange-red background for startup control
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var contentPanel = new StackPanel();

            var autostartTitle = new TextBlock
            {
                Text = "⚡ Enable at Startup",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            // Find the profile state for this app
            ProfileState? appState = null;
            if (selectedProfile != null)
            {
                appState = selectedProfile.Apps.Values.FirstOrDefault(a => a.App.CustomName == app.CustomName);
                System.Diagnostics.Debug.WriteLine($"Found app state for {app.CustomName}: TaggedForStart={appState?.TaggedForStart}, IsRequired={appState?.IsRequired}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"No selected profile provided for autostart section");
            }

            var autostartCheckBox = new CheckBox
            {
                Content = "Start this application automatically when the profile is launched",
                IsChecked = appState?.TaggedForStart ?? false,
                FontSize = 13,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var infoText = new TextBlock
            {
                Text = appState?.IsRequired == true 
                    ? "This is a required application and will always start with the profile."
                    : "Control whether this application starts automatically when you launch your darts profile.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Margin = new Thickness(0, 5, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            // Event handlers for the checkbox
            autostartCheckBox.Checked += (s, e) =>
            {
                if (appState != null && !appState.IsRequired)
                {
                    appState.TaggedForStart = true;
                    saveCallback?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"Enabled autostart for {app.CustomName}");
                }
            };

            autostartCheckBox.Unchecked += (s, e) =>
            {
                if (appState != null && !appState.IsRequired)
                {
                    appState.TaggedForStart = false;
                    if (appState.App.AppRunningState)
                    {
                        appState.App.Close(); // Stop the app if it's running and autostart is disabled
                    }
                    saveCallback?.Invoke();
                    System.Diagnostics.Debug.WriteLine($"Disabled autostart for {app.CustomName}");
                }
            };

            // Disable checkbox for required apps or if no profile state is available
            if (appState?.IsRequired == true)
            {
                autostartCheckBox.IsEnabled = false;
                autostartCheckBox.IsChecked = true; // Required apps are always enabled
            }
            else if (appState == null)
            {
                autostartCheckBox.IsEnabled = false;
                infoText.Text = "Autostart control is not available (no profile selected).";
            }

            contentPanel.Children.Add(autostartTitle);
            contentPanel.Children.Add(autostartCheckBox);
            contentPanel.Children.Add(infoText);

            autostartPanel.Child = contentPanel;
            return autostartPanel;
        }
    }
}