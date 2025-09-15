using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages the app navigation panel and app state tracking
    /// </summary>
    public class NavigationManager
    {
        private readonly Dictionary<string, bool> lastKnownRunningStates = new Dictionary<string, bool>();
        private Timer? navigationUpdateTimer;
        private Profile? selectedProfile;
        private Action? refreshSettingsCallback;
        private Action<AppBase>? appSelectedCallback;
        private Action? saveCallback;

        public StackPanel? AppNavigationPanel { get; set; }
        public AppBase? SelectedApp { get; private set; }

        public void Initialize(Action? refreshSettings = null, Action<AppBase>? appSelected = null, Action? save = null)
        {
            refreshSettingsCallback = refreshSettings;
            appSelectedCallback = appSelected;
            saveCallback = save;

            // Initialize navigation update timer
            navigationUpdateTimer = new Timer(3000); // Update every 3 seconds
            navigationUpdateTimer.Elapsed += NavigationUpdateTimer_Elapsed;
            navigationUpdateTimer.AutoReset = true;
            navigationUpdateTimer.Start(); // Always running to update app states
        }

        public void Dispose()
        {
            navigationUpdateTimer?.Stop();
            navigationUpdateTimer?.Dispose();
        }

        public void SetSelectedProfile(Profile? profile)
        {
            selectedProfile = profile;
            lastKnownRunningStates.Clear();
            if (profile != null)
            {
                RenderAppNavigation();
            }
        }

        public void RenderAppNavigation()
        {
            if (AppNavigationPanel == null) return;

            AppNavigationPanel.Children.Clear();
            
            if (selectedProfile == null) return;

            // Custom ordering: darts-apps first, with darts-caller at the very top
            var sortedApps = selectedProfile.Apps
                .OrderBy(a => GetAppSortPriority(a.Value.App.CustomName))
                .ThenBy(a => a.Value.App.CustomName)
                .ThenByDescending(a => a.Value.TaggedForStart)
                .ThenByDescending(a => a.Value.IsRequired);

            foreach (var app in sortedApps)
            {
                var appState = app.Value;
                
                // Create app button content with running indicator if app is running
                var buttonContent = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                
                // App name text
                var appNameText = new TextBlock
                {
                    Text = appState.App.CustomName,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = appState.TaggedForStart ? Brushes.White : new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontWeight = appState.TaggedForStart ? FontWeight.Bold : FontWeight.Normal
                };
                buttonContent.Children.Add(appNameText);
                
                // Add running indicator as part of the button content if app is running
                if (appState.App.AppRunningState)
                {
                    var runningIndicator = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(7, 2, 155, 250)), // RGBA with transparency
                        CornerRadius = new Avalonia.CornerRadius(8),
                        Padding = new Avalonia.Thickness(5, 1),
                        Margin = new Avalonia.Thickness(10, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "RUNNING",
                            Foreground = Brushes.White,
                            FontSize = 10,
                            FontWeight = FontWeight.Bold
                        }
                    };
                    
                    // Add subtle glow effect with RGBA
                    runningIndicator.Effect = new Avalonia.Media.DropShadowEffect
                    {
                        Color = Color.FromRgb(2, 155, 250), // RGBA shadow color
                        BlurRadius = 20,
                        OffsetX = 0,
                        OffsetY = 0,
                        Opacity = 1.0
                    };
                    
                    buttonContent.Children.Add(runningIndicator);
                }

                // Linie oben
                var topLine = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Margin = new Avalonia.Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // App-Button ohne Border
                var appButton = new Button
                {
                    Content = buttonContent,
                    Background = Brushes.Transparent,
                    BorderThickness = new Avalonia.Thickness(0), // Keine Border!
                    CornerRadius = new Avalonia.CornerRadius(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Avalonia.Thickness(10, 8),
                    Width = double.NaN,
                    Tag = appState.App
                };

                // Linie unten
                var bottomLine = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Margin = new Avalonia.Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // Container für alles
                var buttonContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Avalonia.Thickness(0, 0, 0, 0)
                };

                buttonContainer.Children.Add(topLine);
                buttonContainer.Children.Add(appButton);
                buttonContainer.Children.Add(bottomLine);

                AppNavigationPanel.Children.Add(buttonContainer);

                appButton.Click += (s, e) =>
                {
                    SelectedApp = appState.App;
                    appSelectedCallback?.Invoke(appState.App);
                };

                // Add context menu for app actions
                var contextMenu = CreateAppContextMenu(appState);
                appButton.ContextMenu = contextMenu;
            }
        }

        private ContextMenu CreateAppContextMenu(ProfileState appState)
        {
            var contextMenu = new ContextMenu();
            
            var configMenuItem = new MenuItem { Header = "Configure" };
            configMenuItem.Click += (s, e) => appSelectedCallback?.Invoke(appState.App);
            contextMenu.Items.Add(configMenuItem);
            
            if (!string.IsNullOrEmpty(appState.App.AppMonitor))
            {
                var monitorMenuItem = new MenuItem { Header = "Show Monitor" };
                monitorMenuItem.Click += async (s, e) => 
                {
                    // This would need to be handled by the main window since we don't have access to it here
                    // Could be improved with an event system
                };
                contextMenu.Items.Add(monitorMenuItem);
            }
            
            var toggleMenuItem = new MenuItem 
            { 
                Header = appState.TaggedForStart ? "Disable" : "Enable"
            };
            toggleMenuItem.Click += (s, e) =>
            {
                if (!appState.IsRequired)
                {
                    appState.TaggedForStart = !appState.TaggedForStart;
                    if (!appState.TaggedForStart)
                    {
                        appState.App.Close();
                    }
                    RenderAppNavigation();
                    saveCallback?.Invoke();
                }
            };
            contextMenu.Items.Add(toggleMenuItem);

            return contextMenu;
        }

        private void NavigationUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Update navigation to refresh running states
            Dispatcher.UIThread.Post(() =>
            {
                var currentSelectedApp = SelectedApp;
                bool needsSettingsRefresh = false;
                
                // Check if any app running state has changed
                if (selectedProfile != null)
                {
                    foreach (var appState in selectedProfile.Apps.Values)
                    {
                        var appName = appState.App.CustomName;
                        var currentRunningState = appState.App.AppRunningState;
                        
                        if (lastKnownRunningStates.TryGetValue(appName, out var lastKnownState))
                        {
                            if (lastKnownState != currentRunningState)
                            {
                                // Running state changed
                                lastKnownRunningStates[appName] = currentRunningState;
                                
                                // If this is the currently selected app, we need to refresh settings
                                if (currentSelectedApp != null && currentSelectedApp.CustomName == appName)
                                {
                                    needsSettingsRefresh = true;
                                }
                            }
                        }
                        else
                        {
                            // First time seeing this app, store its state
                            lastKnownRunningStates[appName] = currentRunningState;
                        }
                    }
                }
                
                RenderAppNavigation();
                
                // Only refresh app settings if needed
                if (needsSettingsRefresh && currentSelectedApp != null)
                {
                    refreshSettingsCallback?.Invoke();
                }
            });
        }

        private int GetAppSortPriority(string appName)
        {
            // darts-caller gets highest priority (0)
            if (appName == "darts-caller") return 0;
            
            // Other darts-apps get priority 1-10
            if (appName.StartsWith("darts-")) return GetDartsAppPriority(appName);
            
            // All other apps get priority 100+
            return 100;
        }

        private int GetDartsAppPriority(string appName)
        {
            return appName switch
            {
                "darts-caller" => 1,
                "darts-extern" => 2,
                "darts-wled" => 3,
                "darts-pixelit" => 4,
                "darts-gif" => 5,
                "darts-voice" => 6,
                _ => 10 // Other darts-apps
            };
        }
    }
}