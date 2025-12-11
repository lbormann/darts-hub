using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages console functionality including tabs, content updates and scrolling
    /// </summary>
    public class ConsoleManager
    {
        private readonly Dictionary<string, TabItem> consoleTabs = new Dictionary<string, TabItem>();
        private readonly Dictionary<string, bool> lastKnownRunningStates = new Dictionary<string, bool>();
        private string? currentConsoleTab;
        private bool isAutoScrollEnabled = true;
        private bool isUserScrolling = false;
        private Timer? consoleUpdateTimer;
        private Profile? selectedProfile;

        public TabControl? ConsoleTabControl { get; set; }
        public bool IsAutoScrollEnabled
        {
            get => isAutoScrollEnabled;
            set => isAutoScrollEnabled = value;
        }

        public void Initialize()
        {
            consoleUpdateTimer = new Timer(1000); // Update every 1 second
            consoleUpdateTimer.Elapsed += ConsoleUpdateTimer_Elapsed;
            consoleUpdateTimer.AutoReset = true;
        }

        public void Start()
        {
            consoleUpdateTimer?.Start();
        }

        public void Stop()
        {
            consoleUpdateTimer?.Stop();
        }

        public void Dispose()
        {
            consoleUpdateTimer?.Stop();
            consoleUpdateTimer?.Dispose();
        }

        public void SetSelectedProfile(Profile? profile)
        {
            selectedProfile = profile;
            if (profile != null)
            {
                InitializeConsoleTabs();
            }
        }

        public void InitializeConsoleTabs()
        {
            if (ConsoleTabControl == null) return;

            // Clear existing tabs
            ConsoleTabControl.Items.Clear();
            consoleTabs.Clear();
            currentConsoleTab = null;

            if (selectedProfile == null) return;

            // Create Overview tab
            var overviewTab = CreateOverviewTab();
            ConsoleTabControl.Items.Add(overviewTab);
            consoleTabs.Add("Overview", overviewTab);

            // Create tabs for each app using unique keys to prevent duplicates
            var appCounter = new Dictionary<string, int>(); // Count apps with same CustomName
            
            foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
            {
                var baseName = app.App.CustomName;
                string uniqueKey;
                
                // Check if we already have an app with this CustomName
                if (appCounter.ContainsKey(baseName))
                {
                    appCounter[baseName]++;
                    uniqueKey = $"{baseName} ({appCounter[baseName]})"; // Add number suffix
                }
                else
                {
                    appCounter[baseName] = 1;
                    uniqueKey = baseName; // Use original name for first occurrence
                }
                
                // Ensure the key is truly unique in the dictionary
                int suffix = 2;
                while (consoleTabs.ContainsKey(uniqueKey))
                {
                    uniqueKey = $"{baseName} ({suffix})";
                    suffix++;
                }
                
                var tab = CreateConsoleTab(app.App, uniqueKey);
                ConsoleTabControl.Items.Add(tab);
                consoleTabs.Add(uniqueKey, tab);
            }

            // Select overview tab by default
            if (ConsoleTabControl.Items.Count > 0)
            {
                ConsoleTabControl.SelectedIndex = 0;
                currentConsoleTab = "Overview";
            }
        }

        public void UpdateContent()
        {
            // Update all console tabs
            if (selectedProfile != null && consoleTabs.Any())
            {
                var appCounter = new Dictionary<string, int>(); // Same logic as InitializeConsoleTabs
                
                foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
                {
                    var baseName = app.App.CustomName;
                    string uniqueKey;
                    
                    if (appCounter.ContainsKey(baseName))
                    {
                        appCounter[baseName]++;
                        uniqueKey = $"{baseName} ({appCounter[baseName]})";
                    }
                    else
                    {
                        appCounter[baseName] = 1;
                        uniqueKey = baseName;
                    }
                    
                    // Find the existing tab and update it
                    if (consoleTabs.ContainsKey(uniqueKey))
                    {
                        UpdateConsoleTab(app.App, uniqueKey);
                    }
                }
            }
        }

        public void UpdateOverviewTab()
        {
            if (!consoleTabs.TryGetValue("Overview", out var tab)) return;

            var scrollViewer = tab.Content as ScrollViewer;
            var textBox = scrollViewer?.Content as TextBox;
            if (textBox == null) return;

            // Save horizontal scroll position before update
            var horizontalOffset = scrollViewer?.Offset.X ?? 0;

            var overviewText = new StringBuilder();
            overviewText.AppendLine("=== Darts-Hub Console Overview ===");
            overviewText.AppendLine($"Profile: {selectedProfile?.Name ?? "None"}");
            overviewText.AppendLine($"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            overviewText.AppendLine();

            if (selectedProfile != null)
            {
                overviewText.AppendLine("Applications Status:");
                overviewText.AppendLine();

                foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
                {
                    var status = app.TaggedForStart ? (app.App.AppRunningState ? "RUNNING" : "ENABLED") : "DISABLED";
                    var hasOutput = !string.IsNullOrEmpty(app.App.AppMonitor);
                    
                    overviewText.AppendLine($"• {app.App.CustomName}");
                    overviewText.AppendLine($"  Status: {status}");
                    
                    // Add startup command info for running or recently run apps
                    if (app.App.AppRunningState || hasOutput)
                    {
                        var executable = AppExecutableHelper.GetAppExecutable(app.App);
                        var arguments = AppExecutableHelper.GetAppArgumentsSafe(app.App);
                        
                        overviewText.AppendLine($"  Executable: {executable}");
                        if (!string.IsNullOrEmpty(arguments))
                        {
                            // Limit argument display length for overview
                            var displayArgs = arguments.Length > 80 ? 
                                arguments.Substring(0, 77) + "..." : arguments;
                            overviewText.AppendLine($"  Arguments: {displayArgs}");
                        }
                        else
                        {
                            overviewText.AppendLine($"  Arguments: (none)");
                        }
                    }
                    
                    overviewText.AppendLine($"  Console Output: {(hasOutput ? "Available" : "None")}");

                    if (hasOutput)
                    {
                        var lines = app.App.AppMonitor.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var lastLines = lines.TakeLast(2); // Reduced to 2 lines to make room for command info
                        overviewText.AppendLine($"  Last Output:");
                        foreach (var line in lastLines)
                        {
                            var trimmedLine = line.Trim();
                            if (trimmedLine.Length > 60)
                                trimmedLine = trimmedLine.Substring(0, 57) + "...";
                            overviewText.AppendLine($"    {trimmedLine}");
                        }
                    }
                }
            }
            else
            {
                overviewText.AppendLine("No profile selected.");
            }

            textBox.Text = overviewText.ToString();

            // Auto-scroll vertically if this is the current tab, but preserve horizontal position
            if (isAutoScrollEnabled && !isUserScrolling && currentConsoleTab == "Overview")
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (scrollViewer != null)
                    {
                        // Scroll to end vertically, but restore horizontal position
                        scrollViewer.Offset = new Avalonia.Vector(horizontalOffset, scrollViewer.ScrollBarMaximum.Y);
                    }
                });
            }
            else if (scrollViewer != null)
            {
                // Always restore horizontal position even when not auto-scrolling
                Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer.Offset = scrollViewer.Offset.WithX(horizontalOffset);
                });
            }
        }

        public void ClearAllConsoles()
        {
            // Clear all console tabs
            foreach (var tab in consoleTabs.Values)
            {
                var scrollViewer = tab.Content as ScrollViewer;
                var textBox = scrollViewer?.Content as TextBox;
                if (textBox != null)
                {
                    textBox.Text = "Console cleared.\n\n";
                }
            }
            
            // Re-update all tabs with fresh content
            UpdateContent();
        }

        public void ClearCurrentConsole()
        {
            if (string.IsNullOrEmpty(currentConsoleTab)) return;

            if (consoleTabs.TryGetValue(currentConsoleTab, out var tab))
            {
                var scrollViewer = tab.Content as ScrollViewer;
                var textBox = scrollViewer?.Content as TextBox;
                if (textBox != null)
                {
                    textBox.Text = $"Console cleared for {currentConsoleTab}.\n\n";
                }
                
                // Re-update this specific tab
                if (currentConsoleTab == "Overview")
                {
                    UpdateOverviewTab();
                }
                else if (selectedProfile != null)
                {
                    // Find the app by matching the uniqueKey logic
                    var app = FindAppByUniqueKey(currentConsoleTab);
                    if (app != null)
                    {
                        UpdateConsoleTab(app, currentConsoleTab);
                    }
                }
            }
        }

        public string? ExportCurrentConsole()
        {
            if (string.IsNullOrEmpty(currentConsoleTab) || selectedProfile == null)
                return null;

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName;
            string content;

            if (currentConsoleTab == "Overview")
            {
                fileName = $"{timestamp}_Overview.log";
                
                // Get overview content
                if (consoleTabs.TryGetValue("Overview", out var overviewTab))
                {
                    var scrollViewer = overviewTab.Content as ScrollViewer;
                    var textBox = scrollViewer?.Content as TextBox;
                    content = textBox?.Text ?? "No overview content available.";
                }
                else
                {
                    content = "Overview tab not found.";
                }
            }
            else
            {
                // Export specific app console
                var app = FindAppByUniqueKey(currentConsoleTab);
                if (app == null)
                    return null;

                fileName = $"{timestamp}_{app.CustomName.Replace(" ", "_")}.log";
                
                // Get console content for this app
                if (consoleTabs.TryGetValue(currentConsoleTab, out var appTab))
                {
                    var scrollViewer = appTab.Content as ScrollViewer;
                    var textBox = scrollViewer?.Content as TextBox;
                    content = textBox?.Text ?? "No console content available.";
                }
                else
                {
                    content = "Console tab not found.";
                }
            }

            // Create logs directory if it doesn't exist
            var logsDir = System.IO.Path.Combine(Environment.CurrentDirectory, "logs");
            if (!System.IO.Directory.Exists(logsDir))
            {
                System.IO.Directory.CreateDirectory(logsDir);
            }

            var filePath = System.IO.Path.Combine(logsDir, fileName);
            
            // Save to file
            System.IO.File.WriteAllText(filePath, content);
            return filePath;
        }

        private AppBase? FindAppByUniqueKey(string uniqueKey)
        {
            if (selectedProfile == null) return null;
            
            var appCounter = new Dictionary<string, int>();
            
            foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
            {
                var baseName = app.App.CustomName;
                string currentUniqueKey;
                
                if (appCounter.ContainsKey(baseName))
                {
                    appCounter[baseName]++;
                    currentUniqueKey = $"{baseName} ({appCounter[baseName]})";
                }
                else
                {
                    appCounter[baseName] = 1;
                    currentUniqueKey = baseName;
                }
                
                if (currentUniqueKey == uniqueKey)
                {
                    return app.App;
                }
            }
            
            return null;
        }

        public void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConsoleTabControl?.SelectedItem is TabItem selectedTab)
            {
                if (selectedTab.Header is string headerText)
                {
                    currentConsoleTab = headerText;
                }
                else if (selectedTab.Header is StackPanel headerPanel)
                {
                    var textBlock = headerPanel.Children.OfType<TextBlock>().FirstOrDefault();
                    currentConsoleTab = textBlock?.Text;
                }
                
                // Reset user scrolling state when switching tabs
                isUserScrolling = false;
            }
        }

        public void OnScrollChanged(object sender, ScrollChangedEventArgs e, string appName)
        {
            if (sender is not ScrollViewer scrollViewer) return;

            // Detect if user is manually scrolling
            if (e.OffsetDelta.Y != 0)
            {
                // Check if user scrolled up (not at bottom)
                var isAtBottom = Math.Abs(scrollViewer.Offset.Y - scrollViewer.ScrollBarMaximum.Y) < 1;
                
                if (currentConsoleTab == appName)
                {
                    isUserScrolling = !isAtBottom;
                }
            }

            // Verbessertes horizontales Scrolling
            if (e.OffsetDelta.X != 0 && Math.Abs(e.OffsetDelta.X) < 50)
            {
                var textBox = scrollViewer.Content as TextBox;
                if (textBox != null && textBox.TextWrapping == Avalonia.Media.TextWrapping.NoWrap)
                {
                    var currentHorizontalOffset = scrollViewer.Offset.X;
                    
                    if (currentHorizontalOffset == 0 && e.OffsetDelta.X < 0)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (scrollViewer.Offset.X == 0 && scrollViewer.ScrollBarMaximum.X > 0)
                            {
                                scrollViewer.Offset = scrollViewer.Offset.WithX(Math.Min(50, scrollViewer.ScrollBarMaximum.X));
                            }
                        });
                    }
                }
            }
        }

        private void ConsoleUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateContent();
                UpdateOverviewTab();
            });
        }

        private TabItem CreateOverviewTab()
        {
            var tab = new TabItem
            {
                Header = "Overview",
                FontSize = 11
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Avalonia.Thickness(10, 10, 10, -30) // Increased bottom padding to prevent cutoff
            };

            var textBox = new TextBox
            {
                Name = "OverviewConsoleOutput",
                Text = "Overview of all applications...",
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Background = Brushes.Transparent,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                IsReadOnly = true,
                BorderThickness = new Avalonia.Thickness(0),
                FontFamily = "Consolas,Monaco,Courier New,monospace",
                FontSize = 12,
                AcceptsReturn = true,
                UseLayoutRounding = true
            };

            scrollViewer.Content = textBox;
            tab.Content = scrollViewer;

            return tab;
        }

        private TabItem CreateConsoleTab(AppBase app, string uniqueKey)
        {
            var tab = new TabItem
            {
                Header = uniqueKey, // Use unique key as header
                FontSize = 11
            };

            // Add running indicator to tab header if app is running
            if (app.AppRunningState)
            {
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                headerPanel.Children.Add(new TextBlock 
                { 
                    Text = uniqueKey, // Use unique key for display
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 5, 0),
                    FontSize = 11
                });

                headerPanel.Children.Add(new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                tab.Header = headerPanel;
            }

            var scrollViewer = new ScrollViewer
            {
                Name = $"{app.CustomName}ScrollViewer",
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Avalonia.Thickness(10, 10, 10, -30) // Increased bottom padding to prevent cutoff
            };

            var textBox = new TextBox
            {
                Name = $"{app.CustomName}ConsoleOutput",
                Text = $"Console output for {uniqueKey}...",
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Background = Brushes.Transparent,
                TextWrapping = Avalonia.Media.TextWrapping.NoWrap,
                IsReadOnly = true,
                BorderThickness = new Avalonia.Thickness(0),
                FontFamily = "Consolas,Monaco,Courier New,monospace",
                FontSize = 12,
                AcceptsReturn = true,
                UseLayoutRounding = true
            };

            // Add scroll change tracking for this specific app using uniqueKey
            scrollViewer.ScrollChanged += (s, e) => OnScrollChanged(s, e, uniqueKey);

            scrollViewer.Content = textBox;
            tab.Content = scrollViewer;

            return tab;
        }

        private void UpdateConsoleTab(AppBase app, string? uniqueKey = null)
        {
            // If no uniqueKey provided, try to find it
            if (string.IsNullOrEmpty(uniqueKey))
            {
                uniqueKey = app.CustomName;
            }
            
            if (!consoleTabs.TryGetValue(uniqueKey, out var tab)) return;

            var scrollViewer = tab.Content as ScrollViewer;
            var textBox = scrollViewer?.Content as TextBox;
            if (textBox == null) return;

            // Save horizontal scroll position before update
            var horizontalOffset = scrollViewer?.Offset.X ?? 0;

            var consoleText = new StringBuilder();
            consoleText.AppendLine($"=== {uniqueKey} Console Output ===");
            consoleText.AppendLine($"Status: {(app.AppRunningState ? "RUNNING" : "STOPPED")}");
            consoleText.AppendLine($"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            consoleText.AppendLine();
            
            // Add startup command information
            if (app.AppRunningState || !string.IsNullOrEmpty(app.AppMonitor))
            {
                consoleText.AppendLine("=== STARTUP COMMAND ===");
                
                // Get executable path with minimal logging
                var executable = "";
                try
                {
                    executable = AppExecutableHelper.GetAppExecutable(app);
                    consoleText.AppendLine($"Executable: {executable}");
                }
                catch (Exception ex)
                {
                    var executableError = $"Error getting executable: {ex.Message}";
                    consoleText.AppendLine($"Executable: {executableError}");
                    System.Diagnostics.Debug.WriteLine($"[ConsoleManager] {executableError}");
                }
                
                // Get composed arguments with minimal logging
                var arguments = "";
                try
                {
                    arguments = AppExecutableHelper.GetAppArgumentsSafe(app);
                    if (!string.IsNullOrEmpty(arguments))
                    {
                        // Show truncated version in console if too long
                        if (arguments.Length > 2000)
                        {
                            consoleText.AppendLine($"Arguments: {arguments.Substring(0, 2000)}... [TRUNCATED - {arguments.Length} total chars]");
                        }
                        else
                        {
                            consoleText.AppendLine($"Arguments: {arguments}");
                        }
                    }
                    else
                    {
                        consoleText.AppendLine("Arguments: (none)");
                    }
                }
                catch (Exception ex)
                {
                    var argumentsError = $"Error getting arguments: {ex.Message}";
                    consoleText.AppendLine($"Arguments: {argumentsError}");
                    System.Diagnostics.Debug.WriteLine($"[ConsoleManager] {argumentsError}");
                }
                
                // Show full command line
                try
                {
                    var fullCommand = !string.IsNullOrEmpty(executable) ? 
                        $"\"{executable}\"" + (!string.IsNullOrEmpty(arguments) ? $" {arguments}" : "") : 
                        "(executable not determined)";
                        
                    if (fullCommand.Length > 3000)
                    {
                        consoleText.AppendLine($"Full Command: {fullCommand.Substring(0, 3000)}... [TRUNCATED]");
                    }
                    else
                    {
                        consoleText.AppendLine($"Full Command: {fullCommand}");
                    }
                }
                catch (Exception ex)
                {
                    var commandError = $"Error composing full command: {ex.Message}";
                    consoleText.AppendLine($"Full Command: {commandError}");
                    System.Diagnostics.Debug.WriteLine($"[ConsoleManager] {commandError}");
                }
                
                consoleText.AppendLine();
                consoleText.AppendLine("=== CONSOLE OUTPUT ===");
            }

            if (!string.IsNullOrEmpty(app.AppMonitor))
            {
                var lines = app.AppMonitor.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        consoleText.AppendLine(trimmedLine);
                    }
                }
            }
            else
            {
                consoleText.AppendLine("No console output available yet.");
                if (app.AppRunningState)
                {
                    consoleText.AppendLine("Application is running but hasn't produced output yet.");
                }
                else
                {
                    consoleText.AppendLine("Start the application to see console output.");
                }
            }

            textBox.Text = consoleText.ToString();

            // Update tab header with running indicator
            UpdateTabHeader(tab, app, uniqueKey);

            // Auto-scroll to bottom vertically if enabled and this is the current tab, but preserve horizontal position
            if (isAutoScrollEnabled && !isUserScrolling && currentConsoleTab == uniqueKey)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (scrollViewer != null)
                    {
                        // Scroll to end vertically, but restore horizontal position
                        scrollViewer.Offset = new Avalonia.Vector(horizontalOffset, scrollViewer.ScrollBarMaximum.Y);
                    }
                });
            }
            else if (scrollViewer != null)
            {
                // Always restore horizontal position even when not auto-scrolling
                Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer.Offset = scrollViewer.Offset.WithX(horizontalOffset);
                });
            }
        }

        private void UpdateTabHeader(TabItem tab, AppBase app, string uniqueKey)
        {
            if (app.AppRunningState)
            {
                if (tab.Header is not StackPanel)
                {
                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    headerPanel.Children.Add(new TextBlock 
                    { 
                        Text = uniqueKey, 
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 0, 5, 0),
                        FontSize = 11
                    });
                    headerPanel.Children.Add(new Ellipse
                    {
                        Width = 8,
                        Height = 8,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                    tab.Header = headerPanel;
                }
            }
            else
            {
                if (tab.Header is StackPanel)
                {
                    tab.Header = uniqueKey;
                }
            }
        }
    }
}