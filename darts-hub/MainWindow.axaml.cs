using darts_hub.control;
using darts_hub.model;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Threading.Tasks;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.ViewModels;
using System.ComponentModel;
using MsBox.Avalonia.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using System.Timers;
using System.IO;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;

namespace darts_hub
{
    public class UpdaterViewModel : INotifyPropertyChanged
    {
        private bool _isBetaTester;

        public bool IsBetaTester
        {
            get => _isBetaTester;
            set
            {
                if (_isBetaTester != value)
                {
                    _isBetaTester = value;
                    OnPropertyChanged(nameof(IsBetaTester));
                    Updater.IsBetaTester = value;
                    SaveBetaTesterStatus(value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveBetaTesterStatus(bool isBetaTester)
        {
            var configurator = new Configurator("config.json");
            configurator.Settings.IsBetaTester = isBetaTester;
            configurator.SaveSettings();
        }
    }

    public partial class MainWindow : Window
    {
        // ATTRIBUTES
        private const string ConfigPath = "config.json";

        private ProfileManager profileManager;
        private Profile? selectedProfile;
        private AppBase? selectedApp;
        private Configurator configurator;
        private ReadmeParser readmeParser;
        private Dictionary<string, string>? currentTooltips;
        private bool isAutoScrollEnabled = true;
        private bool isUserScrolling = false;
        private Timer? consoleUpdateTimer;
        private Timer? navigationUpdateTimer;
        private Dictionary<string, TabItem> consoleTabs = new Dictionary<string, TabItem>();
        private string? currentConsoleTab;

        private enum ContentMode
        {
            Settings,
            Console,
            Changelog
        }

        private ContentMode currentContentMode = ContentMode.Settings;

        // METHODS
        public MainWindow()
        {
            InitializeComponent();
            
            configurator = new Configurator("config.json");
            readmeParser = new ReadmeParser();
            
            var viewModel = new UpdaterViewModel
            {
                IsBetaTester = configurator.Settings.IsBetaTester
            };
            DataContext = viewModel;

            WindowHelper.CenterWindowOnScreen(this);
            
            // Initialize console update timer
            consoleUpdateTimer = new Timer(2000); // Update every 2 seconds
            consoleUpdateTimer.Elapsed += ConsoleUpdateTimer_Elapsed;
            consoleUpdateTimer.AutoReset = true;
            
            // Initialize navigation update timer
            navigationUpdateTimer = new Timer(3000); // Update every 3 seconds
            navigationUpdateTimer.Elapsed += NavigationUpdateTimer_Elapsed;
            navigationUpdateTimer.AutoReset = true;
            navigationUpdateTimer.Start(); // Always running to update app states
            
            Opened += MainWindow_Opened;
            Closing += Window_Closing;
        }

        private async void MainWindow_Opened(object sender, EventArgs e)
        {
            try
            {
                configurator = new(ConfigPath);
                CheckBoxStartProfileOnProgramStart.IsChecked = configurator.Settings.StartProfileOnStart;

                profileManager = new ProfileManager();
                profileManager.AppDownloadStarted += ProfileManager_AppDownloadStarted;
                profileManager.AppDownloadFinished += ProfileManager_AppDownloadFinished;
                profileManager.AppDownloadFailed += ProfileManager_AppDownloadFailed;
                profileManager.AppDownloadProgressed += ProfileManager_AppDownloadProgressed;
                profileManager.AppInstallStarted += ProfileManager_AppInstallStarted;
                profileManager.AppInstallFinished += ProfileManager_AppInstallFinished;
                profileManager.AppInstallFailed += ProfileManager_AppInstallFailed;
                profileManager.AppConfigurationRequired += ProfileManager_AppConfigurationRequired;

                profileManager.LoadAppsAndProfiles();

                try
                {
                    profileManager.CloseApps();
                }
                catch (Exception ex)
                {
                    await RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                }

                RenderProfiles();

                Updater.NewReleaseFound += Updater_NewReleaseFound;
                Updater.NoNewReleaseFound += Updater_NoNewReleaseFound;
                Updater.ReleaseInstallInitialized += Updater_ReleaseInstallInitialized;
                Updater.ReleaseDownloadStarted += Updater_ReleaseDownloadStarted;
                Updater.ReleaseDownloadFailed += Updater_ReleaseDownloadFailed;
                Updater.ReleaseDownloadProgressed += Updater_ReleaseDownloadProgressed;
                Updater.CheckNewVersion();
                SetWait(true, "Checking for update...");
            }
            catch (ConfigurationException ex)
            {
                SetWait(false);
                ShowCorruptedConfigHandlingBox(ex);
            }
            catch (Exception ex)
            {
                await RenderMessageBox("", "Something went wrong: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                Environment.Exit(1);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Stop and dispose the console update timer
                consoleUpdateTimer?.Stop();
                consoleUpdateTimer?.Dispose();
                
                // Stop and dispose the navigation update timer
                navigationUpdateTimer?.Stop();
                navigationUpdateTimer?.Dispose();
                
                profileManager?.CloseApps();
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void Buttonstart_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedProfile(true);
        }

        private async void Buttonabout_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            await new AboutWindow(configurator).ShowDialog(this);
            WindowState = WindowState.Normal;
        }

        private void ButtonConsole_Click(object sender, RoutedEventArgs e)
        {
            if (currentContentMode == ContentMode.Console)
            {
                ShowSettingsMode();
            }
            else
            {
                ShowConsoleMode();
            }
        }

        private async void ButtonChangelog_Click(object sender, RoutedEventArgs e)
        {
            if (currentContentMode == ContentMode.Changelog)
            {
                ShowSettingsMode();
            }
            else
            {
                await ShowChangelogMode();
            }
        }

        private void ShowSettingsMode()
        {
            currentContentMode = ContentMode.Settings;
            
            // Stop the console update timer
            consoleUpdateTimer?.Stop();
            
            // Show tooltip panel and splitter
            MainGrid.ColumnDefinitions[4].Width = new GridLength(300, GridUnitType.Pixel);
            TooltipPanel.IsVisible = true;
            TooltipSplitter.IsVisible = true;
            
            // Hide console panel
            ConsolePanel.IsVisible = false;
            
            // Show settings, hide others
            SettingsScrollViewer.IsVisible = true;
            ChangelogScrollViewer.IsVisible = false;
            
            // Update button states
            ButtonConsole.Background = Brushes.Transparent;
            ButtonChangelog.Background = Brushes.Transparent;
        }

        private void ShowConsoleMode()
        {
            currentContentMode = ContentMode.Console;
            
            // Hide tooltip panel and splitter
            MainGrid.ColumnDefinitions[4].Width = new GridLength(0);
            TooltipPanel.IsVisible = false;
            TooltipSplitter.IsVisible = false;
            
            // Hide settings and changelog
            SettingsScrollViewer.IsVisible = false;
            ChangelogScrollViewer.IsVisible = false;
            
            // Show console panel (spans across both content columns)
            ConsolePanel.IsVisible = true;
            
            // Initialize console tabs
            InitializeConsoleTabs();
            
            // Start the console update timer
            consoleUpdateTimer?.Start();
            
            // Update button states
            ButtonConsole.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
            ButtonChangelog.Background = Brushes.Transparent;
        }

        private async Task ShowChangelogMode()
        {
            currentContentMode = ContentMode.Changelog;
            
            // Stop the console update timer
            consoleUpdateTimer?.Stop();
            
            // Hide tooltip panel and splitter
            MainGrid.ColumnDefinitions[4].Width = new GridLength(0);
            TooltipPanel.IsVisible = false;
            TooltipSplitter.IsVisible = false;
            
            // Hide console panel
            ConsolePanel.IsVisible = false;
            
            // Show changelog, hide others
            SettingsScrollViewer.IsVisible = false;
            ChangelogScrollViewer.IsVisible = true;
            
            // Load changelog content
            await LoadChangelogContent();
            
            // Update button states
            ButtonConsole.Background = Brushes.Transparent;
            ButtonChangelog.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));
        }

        private void UpdateConsoleContent()
        {
            // Update all console tabs
            if (selectedProfile != null && consoleTabs.Any())
            {
                foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
                {
                    UpdateConsoleTab(app.App);
                }
            }
        }

        private void InitializeConsoleTabs()
        {
            // Clear existing tabs
            ConsoleTabControl.Items.Clear();
            consoleTabs.Clear();
            currentConsoleTab = null;

            if (selectedProfile == null) return;

            // Create Overview tab
            var overviewTab = CreateOverviewTab();
            ConsoleTabControl.Items.Add(overviewTab);
            consoleTabs.Add("Overview", overviewTab);

            // Create tabs for each app
            foreach (var app in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
            {
                var tab = CreateConsoleTab(app.App);
                ConsoleTabControl.Items.Add(tab);
                consoleTabs.Add(app.App.CustomName, tab);
            }

            // Select overview tab by default
            if (ConsoleTabControl.Items.Count > 0)
            {
                ConsoleTabControl.SelectedIndex = 0;
                currentConsoleTab = "Overview";
            }
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
                Padding = new Thickness(10)
            };

            var textBox = new TextBox
            {
                Name = "OverviewConsoleOutput",
                Text = "Overview of all applications...",
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                FontFamily = "Consolas,Monaco,Courier New,monospace",
                FontSize = 12,
                AcceptsReturn = true
            };

            scrollViewer.Content = textBox;
            tab.Content = scrollViewer;

            return tab;
        }

        private TabItem CreateConsoleTab(AppBase app)
        {
            var tab = new TabItem
            {
                Header = app.CustomName,
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
                    Text = app.CustomName, 
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0),
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
                Padding = new Thickness(10)
            };

            var textBox = new TextBox
            {
                Name = $"{app.CustomName}ConsoleOutput",
                Text = $"Console output for {app.CustomName}...",
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                FontFamily = "Consolas,Monaco,Courier New,monospace",
                FontSize = 12,
                AcceptsReturn = true
            };

            // Add scroll change tracking for this specific app
            scrollViewer.ScrollChanged += (s, e) => ConsoleScrollViewer_ScrollChanged(s, e, app.CustomName);

            scrollViewer.Content = textBox;
            tab.Content = scrollViewer;

            return tab;
        }

        private void UpdateConsoleTab(AppBase app)
        {
            if (!consoleTabs.TryGetValue(app.CustomName, out var tab)) return;

            var scrollViewer = tab.Content as ScrollViewer;
            var textBox = scrollViewer?.Content as TextBox;
            if (textBox == null) return;

            var consoleText = new StringBuilder();
            consoleText.AppendLine($"=== {app.CustomName} Console Output ===");
            consoleText.AppendLine($"Status: {(app.AppRunningState ? "RUNNING" : "STOPPED")}");
            consoleText.AppendLine($"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            consoleText.AppendLine();

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
            UpdateTabHeader(tab, app);

            // Auto-scroll to bottom if enabled and this is the current tab
            if (isAutoScrollEnabled && !isUserScrolling && currentConsoleTab == app.CustomName)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer?.ScrollToEnd();
                });
            }
        }

        private void UpdateTabHeader(TabItem tab, AppBase app)
        {
            if (app.AppRunningState)
            {
                if (tab.Header is not StackPanel)
                {
                    var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    headerPanel.Children.Add(new TextBlock 
                    { 
                        Text = app.CustomName, 
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 5, 0),
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
                    tab.Header = app.CustomName;
                }
            }
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

        private void UpdateOverviewTab()
        {
            if (!consoleTabs.TryGetValue("Overview", out var tab)) return;

            var scrollViewer = tab.Content as ScrollViewer;
            var textBox = scrollViewer?.Content as TextBox;
            if (textBox == null) return;

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
                    overviewText.AppendLine($"  Console Output: {(hasOutput ? "Available" : "None")}");

                    if (hasOutput)
                    {
                        var lines = app.App.AppMonitor.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var lastLines = lines.TakeLast(3);
                        overviewText.AppendLine($"  Last Output:");
                        foreach (var line in lastLines)
                        {
                            overviewText.AppendLine($"    {line.Trim()}");
                        }
                    }
                    overviewText.AppendLine();
                }
            }
            else
            {
                overviewText.AppendLine("No profile selected.");
            }

            textBox.Text = overviewText.ToString();

            // Auto-scroll if this is the current tab
            if (isAutoScrollEnabled && !isUserScrolling && currentConsoleTab == "Overview")
            {
                Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer?.ScrollToEnd();
                });
            }
        }

        // New event handlers for console functionality
        private void ConsoleClearButton_Click(object sender, RoutedEventArgs e)
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
            UpdateConsoleContent();
        }

        private void ConsoleClearCurrentButton_Click(object sender, RoutedEventArgs e)
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
                    var app = selectedProfile.Apps.Values.FirstOrDefault(appState => appState.App.CustomName == currentConsoleTab)?.App;
                    if (app != null)
                    {
                        UpdateConsoleTab(app);
                    }
                }
            }
        }

        private void ConsoleTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConsoleTabControl.SelectedItem is TabItem selectedTab)
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

        private async void ProfileManager_AppDownloadStarted(object? sender, AppEventArgs e)
        {
            SetWait(true, "Downloading " + e.App.Name + "...");
        }

        private void ProfileManager_AppDownloadFinished(object? sender, AppEventArgs e)
        {
            SetWait(false);
            RunSelectedProfile(true);
        }

        private void ProfileManager_AppDownloadFailed(object? sender, AppEventArgs e)
        {
            SetWait(false, "Download " + e.App.Name + " failed. Please check your internet connection and try again. " + e.Message);
        }

        private void ProfileManager_AppDownloadProgressed(object? sender, DownloadProgressChangedEventArgs e)
        {
            SetWait(true);
        }

        private void ProfileManager_AppInstallStarted(object? sender, AppEventArgs e)
        {
            SetWait(true, "Installing " + e.App.Name + "...");
        }

        private void ProfileManager_AppInstallFinished(object? sender, AppEventArgs e)
        {
            SetWait(false);
        }

        private void ProfileManager_AppInstallFailed(object? sender, AppEventArgs e)
        {
            SetWait(false, "Install " + e.App.Name + " failed. " + e.Message);
        }

        private async void ProfileManager_AppConfigurationRequired(object? sender, AppEventArgs e)
        {
            selectedApp = e.App;
            await RenderAppSettings(e.App);
        }

        private void Save()
        {
            try
            {
                profileManager.StoreApps();
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private async void RunSelectedProfile(bool minimize = true)
        {
            try
            {
                SetWait(true, "Starting profile...");
                if (ProfileManager.RunProfile(selectedProfile) && minimize) 
                    WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                await RenderMessageBox("", "An error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }
            finally
            {
                SetWait(false);
            }
        }

        private void SetWait(bool wait, string waitingText = "")
        {
            string waitingMessage = string.IsNullOrEmpty(waitingText) ? WaitingText.Text : waitingText;
            
            LoadingOverlay.IsVisible = wait;
            WaitingText.Text = waitingMessage;
            
            // Update console content if in console mode (but don't interfere with manual scrolling)
            if (currentContentMode == ContentMode.Console && !wait)
            {
                UpdateConsoleContent();
            }
        }

        private async void RenderProfiles()
        {
            ComboBoxItem? lastItemTaggedForStart = null;
            var profiles = profileManager.GetProfiles();
            if (profiles.Count == 0)
            {
                await RenderMessageBox("", "No profiles available.", MsBox.Avalonia.Enums.Icon.Warning);
                Environment.Exit(1);
            }

            var cbiProfiles = new List<ComboBoxItem>();
            foreach (var profile in profiles)
            {
                var comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = profile.Name;
                comboBoxItem.Tag = profile;
                cbiProfiles.Add(comboBoxItem);

                if (profile.IsTaggedForStart) lastItemTaggedForStart = comboBoxItem;
            }
            
            Comboboxportal.Items.Clear();
            foreach (var item in cbiProfiles)
            {
                Comboboxportal.Items.Add(item);
            }
            
            Comboboxportal.SelectedItem = lastItemTaggedForStart ?? cbiProfiles[0];
            RenderAppNavigation();
        }

        private void RenderAppNavigation()
        {
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
                
                // Create main button container - now just contains the button
                //var buttonContainer = new StackPanel
                //{
                //    Orientation = Orientation.Horizontal,
                //    Margin = new Thickness(0, 2)
                //};
                
                // Create app button content with running indicator if needed
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
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(5, 1),
                        Margin = new Thickness(10, 0, 0, 0),
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
                //// Linie oben
                //var topLine = new Border
                //{
                //    Height = 1,
                //    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                //    Margin = new Thickness(0, 4, 0, 0),
                //    HorizontalAlignment = HorizontalAlignment.Stretch
                //};
                //var appButton = new Button
                //{
                //    Content = buttonContent,
                //    Background = Brushes.Transparent,
                //    //BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 71)),
                //    //BorderThickness = new Thickness(1),
                //    //CornerRadius = new CornerRadius(3),
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    HorizontalContentAlignment = HorizontalAlignment.Left,
                //    Padding = new Thickness(10, 8),
                //    Width = double.NaN; // Slightly wider to accommodate running indicator
                //    Tag = appState.App
                //};
                //// Linie unten
                //var bottomLine = new Border
                //{
                //    Height = 1,
                //    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                //    Margin = new Thickness(0, 0, 0, 4),
                //    HorizontalAlignment = HorizontalAlignment.Stretch
                //};
                // Linie oben
                var topLine = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Margin = new Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // App-Button ohne Border
                var appButton = new Button
                {
                    Content = buttonContent,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0), // Keine Border!
                    CornerRadius = new CornerRadius(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 8),
                    Width = double.NaN,
                    Tag = appState.App
                };

                // Linie unten
                var bottomLine = new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    Margin = new Thickness(0, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                // Container für alles
                var buttonContainer = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 0, 0, 0)
                };

                buttonContainer.Children.Add(topLine);
                buttonContainer.Children.Add(appButton);
                buttonContainer.Children.Add(bottomLine);

                AppNavigationPanel.Children.Add(buttonContainer);

                appButton.Click += async (s, e) =>
                {
                    selectedApp = appState.App;

                    // Update button selection visual
                    //foreach (var container in AppNavigationPanel.Children.OfType<StackPanel>())
                    //{
                    //    foreach (var child in container.Children.OfType<Button>())
                    //    {
                    //        child.Background = Brushes.Transparent;
                    //    }
                    //}
                    //appButton.Background = new SolidColorBrush(Color.FromRgb(0, 122, 204));

                    // Show settings mode if not already
                    if (currentContentMode != ContentMode.Settings)
                    {
                        ShowSettingsMode();
                    }
                    
                    // Render app settings
                    await RenderAppSettings(appState.App);
                };

                // Add context menu for app actions
                var contextMenu = new ContextMenu();
                
                var configMenuItem = new MenuItem { Header = "Configure" };
                configMenuItem.Click += async (s, e) => await RenderAppSettings(appState.App);
                contextMenu.Items.Add(configMenuItem);
                
                if (!string.IsNullOrEmpty(appState.App.AppMonitor))
                {
                    var monitorMenuItem = new MenuItem { Header = "Show Monitor" };
                    monitorMenuItem.Click += async (s, e) => await new MonitorWindow(appState.App).ShowDialog(this);
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
                        Save();
                    }
                };
                contextMenu.Items.Add(toggleMenuItem);
                
                appButton.ContextMenu = contextMenu;
                //buttonContainer.Children.Add(appButton);
                
                //AppNavigationPanel.Children.Add(buttonContainer);
            }
        }

        private async Task RenderAppSettings(AppBase app)
        {
            SettingsPanel.Children.Clear();
            selectedApp = app;
            
            if (!app.IsConfigurable())
            {
                SettingsPanel.Children.Add(new TextBlock
                {
                    Text = $"{app.CustomName} has no configurable settings.",
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20)
                });
                return;
            }

            // Load tooltips for this app
            await LoadTooltipsForApp(app);

            // Header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
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
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 0, 0)
            };

            // App Control Buttons
            var controlButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 20, 0)
            };

            // Start/Stop Button
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
                try
                {
                    if (app.AppRunningState)
                    {
                        app.Close();
                        await Task.Delay(500); // Give it time to close
                    }
                    else
                    {
                        app.Run();
                        await Task.Delay(500); // Give it time to start
                    }
                    
                    // Refresh the settings page and navigation
                    await RenderAppSettings(app);
                    RenderAppNavigation();
                    
                    // Save changes
                    Save();
                }
                catch (Exception ex)
                {
                    await RenderMessageBox("Error", $"Failed to {(app.AppRunningState ? "stop" : "start")} {app.CustomName}:\n{ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
                }
            };
            
            controlButtonPanel.Children.Add(startStopButton);

            // Restart Button
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
            
            restartButton.Click += async (s, e) =>
            {
                try
                {
                    if (app.AppRunningState)
                    {
                        app.Close();
                        await Task.Delay(2000); // Wait for proper shutdown
                    }
                    
                    app.Run();
                    await Task.Delay(1000); // Give it time to start
                    
                    // Refresh the settings page and navigation
                    await RenderAppSettings(app);
                    RenderAppNavigation();
                    
                    // Save changes
                    Save();
                }
                catch (Exception ex)
                {
                    await RenderMessageBox("Error", $"Failed to restart {app.CustomName}:\n{ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
                }
            };
            
            controlButtonPanel.Children.Add(restartButton);
            buttonPanel.Children.Add(controlButtonPanel);

            // Help and Changelog buttons
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
                    
                    await RenderMessageBox("Changelog", changelogText, MsBox.Avalonia.Enums.Icon.None, ButtonEnum.Ok, Width, Height);
                };
                buttonPanel.Children.Add(changelogBtn);
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
                        RenderMessageBox("Error", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                    }
                };
                buttonPanel.Children.Add(helpBtn);
            }
            
            headerPanel.Children.Add(buttonPanel);
            SettingsPanel.Children.Add(headerPanel);

            // Add Autostart section
            var autostartSection = CreateAutostartSection(app);
            SettingsPanel.Children.Add(autostartSection);

            // Render configuration sections
            var appConfiguration = app.Configuration;
            var argumentsBySection = appConfiguration.Arguments.GroupBy(a => a.Section);

            foreach (var section in argumentsBySection)
            {
                var expander = new Expander
                {
                    Header = section.Key,
                    IsExpanded = true,
                    Margin = new Thickness(0, 20), // Größerer Abstand zwischen den Expandern (20px)
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
                };

                var sectionPanel = new StackPanel 
                { 
                    Margin = new Thickness(10),
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
                };

                foreach (var argument in section)
                {
                    if (argument.IsRuntimeArgument) continue;

                    var argumentControl = await CreateArgumentControl(argument);
                    if (argumentControl != null)
                    {
                        // Remove margin from individual controls to use full width
                        argumentControl.Margin = new Thickness(0, 5);
                        argumentControl.HorizontalAlignment = HorizontalAlignment.Stretch; // Volle Breite nutzen
                        sectionPanel.Children.Add(argumentControl);
                    }
                }

                expander.Content = sectionPanel;
                SettingsPanel.Children.Add(expander);
            }
        }

        private Control CreateAutostartSection(AppBase app)
        {
            var expander = new Expander
            {
                Header = "Application Control",
                IsExpanded = true,
                Margin = new Thickness(0, 20), // Größerer Abstand (20px)
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
            };

            var sectionPanel = new StackPanel 
            { 
                Margin = new Thickness(10),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
            };

            // Get the current app state from the selected profile
            var appState = selectedProfile?.Apps.Values.FirstOrDefault(a => a.App.CustomName == app.CustomName);
            
            if (appState != null)
            {
                // Autostart checkbox
                var autostartPanel = new StackPanel 
                { 
                    Margin = new Thickness(0, 5),
                    HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
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
                    HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
                };

                autostartCheckBox.Checked += (s, e) => 
                {
                    appState.TaggedForStart = true;
                    Save(); // Save profile changes
                    RenderAppNavigation(); // Update navigation to reflect changes
                };
                
                autostartCheckBox.Unchecked += (s, e) => 
                {
                    appState.TaggedForStart = false;
                    Save(); // Save profile changes
                    RenderAppNavigation(); // Update navigation to reflect changes
                };

                autostartPanel.Children.Add(autostartCheckBox);
                sectionPanel.Children.Add(autostartPanel);
            }

            expander.Content = sectionPanel;
            return expander;
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

        private async Task<Control?> CreateArgumentControl(Argument argument)
        {
            var mainPanel = new StackPanel 
            { 
                Margin = new Thickness(0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
            };
            
            // Label
            var label = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : ""),
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            mainPanel.Children.Add(label);

            Control? inputControl = null;
            string type = argument.GetTypeClear();

            // Create appropriate input control based on argument type
            switch (type)
            {
                case Argument.TypeString:
                case Argument.TypePassword:
                    inputControl = new TextBox
                    {
                        Text = argument.Value,
                        PasswordChar = type == Argument.TypePassword ? '*' : '\0',
                        FontSize = 14,
                        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        Padding = new Thickness(8),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ((TextBox)inputControl).TextChanged += (s, e) => 
                    {
                        argument.Value = ((TextBox)s).Text;
                        AutoSaveConfiguration(argument);
                    };
                    break;

                case Argument.TypeBool:
                    inputControl = new CheckBox
                    {
                        Content = argument.NameHuman,
                        IsChecked = bool.TryParse(argument.Value, out var boolVal) && boolVal,
                        FontSize = 14,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ((CheckBox)inputControl).Checked += (s, e) => 
                    {
                        argument.Value = "true";
                        AutoSaveConfiguration(argument);
                    };
                    ((CheckBox)inputControl).Unchecked += (s, e) => 
                    {
                        argument.Value = "false";
                        AutoSaveConfiguration(argument);
                    };
                    break;

                case Argument.TypeInt:
                    inputControl = new NumericUpDown
                    {
                        Value = int.TryParse(argument.Value, out var intVal) ? intVal : 0,
                        FontSize = 14,
                        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ((NumericUpDown)inputControl).ValueChanged += (s, e) => 
                    {
                        argument.Value = ((NumericUpDown)s).Value?.ToString() ?? "";
                        AutoSaveConfiguration(argument);
                    };
                    break;

                case Argument.TypeFloat:
                    inputControl = new NumericUpDown
                    {
                        Value = double.TryParse(argument.Value, out var doubleVal) ? (decimal)doubleVal : 0,
                        Increment = 0.1m,
                        FormatString = "F1",
                        FontSize = 14,
                        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    ((NumericUpDown)inputControl).ValueChanged += (s, e) => 
                    {
                        argument.Value = ((NumericUpDown)s).Value?.ToString() ?? "";
                        AutoSaveConfiguration(argument);
                    };
                    break;

                case Argument.TypeFile:
                case Argument.TypePath:
                    var fileTextBox = new TextBox
                    {
                        Text = argument.Value,
                        FontSize = 14,
                        Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                        Foreground = Brushes.White,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        Padding = new Thickness(8),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    fileTextBox.TextChanged += (s, e) => 
                    {
                        argument.Value = fileTextBox.Text;
                        AutoSaveConfiguration(argument);
                    };

                    var browseButton = new Button
                    {
                        Content = "Browse",
                        Margin = new Thickness(5, 0, 0, 0),
                        Padding = new Thickness(10, 5),
                        Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Width = 80
                    };

                    browseButton.Click += async (s, e) =>
                    {
                        if (type == Argument.TypePath)
                        {
                            var dialog = new OpenFolderDialog();
                            var result = await dialog.ShowAsync(this);
                            if (result != null)
                            {
                                fileTextBox.Text = result;
                                argument.Value = result;
                                AutoSaveConfiguration(argument);
                            }
                        }
                        else
                        {
                            var dialog = new OpenFileDialog { AllowMultiple = false };
                            var result = await dialog.ShowAsync(this);
                            if (result != null && result.Length > 0)
                            {
                                fileTextBox.Text = result[0];
                                argument.Value = result[0];
                                AutoSaveConfiguration(argument);
                            }
                        }
                    };

                    // Set up grid for proper sizing
                    var grid = new Grid
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch // Volle Breite nutzen
                    };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    
                    Grid.SetColumn(fileTextBox, 0);
                    Grid.SetColumn(browseButton, 1);
                    
                    grid.Children.Add(fileTextBox);
                    grid.Children.Add(browseButton);
                    
                    inputControl = grid;
                    break;
            }

            if (inputControl != null)
            {
                // Add hover and click events for tooltip display
                inputControl.PointerEntered += (s, e) => ShowTooltip(argument);
                inputControl.PointerPressed += (s, e) => ShowTooltip(argument);
                
                mainPanel.Children.Add(inputControl);
            }

            return mainPanel;
        }

        // Timer event handler for console updates
        private void ConsoleUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Only update if we're in console mode
            if (currentContentMode == ContentMode.Console)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateConsoleContent();
                    UpdateOverviewTab();
                });
            }
        }

        private void NavigationUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Update navigation to refresh running states
            Dispatcher.UIThread.Post(() =>
            {
                RenderAppNavigation();
            });
        }

        private void AutoSaveConfiguration(Argument argument)
        {
            try
            {
                // Mark the argument as changed
                argument.IsValueChanged = true;
                
                // Save the apps configuration immediately
                Save();
                
                // Optional: Show a brief visual feedback (could be a small tooltip or status)
                // For now, we'll just ensure the save happens silently
            }
            catch (Exception ex)
            {
                // Silent failure - we don't want to interrupt user experience
                System.Diagnostics.Debug.WriteLine($"Auto-save failed: {ex.Message}");
            }
        }

        private void ShowTooltip(Argument argument)
        {
            if (currentContentMode != ContentMode.Settings) return;

            TooltipTitle.Text = argument.NameHuman;
            
            var description = "No description available.";
            if (currentTooltips != null && currentTooltips.TryGetValue(argument.Name, out var tooltipDesc))
            {
                description = tooltipDesc;
            }
            else if (!string.IsNullOrEmpty(argument.Description))
            {
                description = argument.Description;
            }
            
            TooltipDescription.Text = description;
            
            // Force layout update to ensure text wrapping works properly
            TooltipDescription.InvalidateMeasure();
            TooltipDescription.InvalidateArrange();
        }

        private void ConsoleScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ConsoleScrollViewer_ScrollChanged(sender, e, currentConsoleTab);
        }
        
        private void ConsoleScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e, string? tabName)
        {
            if (sender is not ScrollViewer scrollViewer || string.IsNullOrEmpty(tabName)) return;
            
            // Check if user is manually scrolling (not at the bottom)
            if (scrollViewer != null)
            {
                // Allow small tolerance for floating point precision
                var isAtBottom = Math.Abs(scrollViewer.Offset.Y - scrollViewer.ScrollBarMaximum.Y) < 5;
                
                // If user scrolled up from bottom, disable auto-scroll
                if (!isAtBottom && e.OffsetDelta.Y < 0)
                {
                    isUserScrolling = true;
                    if (ConsoleAutoScrollCheckBox.IsChecked == true)
                    {
                        ConsoleAutoScrollCheckBox.IsChecked = false;
                        isAutoScrollEnabled = false;
                    }
                }
                // If user scrolled back to bottom, re-enable auto-scroll
                else if (isAtBottom && isUserScrolling)
                {
                    isUserScrolling = false;
                    if (ConsoleAutoScrollCheckBox.IsChecked == false)
                    {
                        ConsoleAutoScrollCheckBox.IsChecked = true;
                        isAutoScrollEnabled = true;
                    }
                }
            }
        }

        private async void ConsoleExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = currentConsoleTab == "Overview" 
                    ? $"{timestamp}_DartsHub_Overview.log"
                    : $"{timestamp}_{currentConsoleTab}_Console.log";
                
                // Get the directory path where the app is running
                var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var logPath = System.IO.Path.Combine(appPath ?? Environment.CurrentDirectory, "logs");
                
                // Create logs directory if it doesn't exist
                if (!System.IO.Directory.Exists(logPath))
                {
                    System.IO.Directory.CreateDirectory(logPath);
                }
                
                var fullPath = System.IO.Path.Combine(logPath, filename);
                
                // Create detailed log content
                var logContent = new StringBuilder();
                
                if (currentConsoleTab == "Overview")
                {
                    // Export overview of all apps
                    logContent.AppendLine($"Darts-Hub Overview Export");
                    logContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    logContent.AppendLine($"Profile: {selectedProfile?.Name ?? "None"}");
                    logContent.AppendLine("".PadRight(80, '='));
                    logContent.AppendLine();
                    
                    if (selectedProfile != null)
                    {
                        foreach (var appState in selectedProfile.Apps.Values.OrderBy(a => a.App.CustomName))
                        {
                            logContent.AppendLine($"App: {appState.App.CustomName}");
                            logContent.AppendLine($"Status: {(appState.TaggedForStart ? (appState.App.AppRunningState ? "RUNNING" : "ENABLED") : "DISABLED")}");
                            logContent.AppendLine($"Running State: {appState.App.AppRunningState}");
                            logContent.AppendLine("".PadRight(60, '-'));
                            
                            if (!string.IsNullOrEmpty(appState.App.AppMonitor))
                            {
                                logContent.AppendLine("Console Output:");
                                logContent.AppendLine(appState.App.AppMonitor);
                            }
                            else
                            {
                                logContent.AppendLine("No console output available.");
                            }
                            
                            logContent.AppendLine();
                            logContent.AppendLine("".PadRight(80, '='));
                            logContent.AppendLine();
                        }
                    }
                }
                else
                {
                    // Export specific app console
                    var app = selectedProfile?.Apps.Values.FirstOrDefault(a => a.App.CustomName == currentConsoleTab)?.App;
                    if (app != null)
                    {
                        logContent.AppendLine($"{app.CustomName} Console Export");
                        logContent.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        logContent.AppendLine($"Profile: {selectedProfile?.Name ?? "None"}");
                        logContent.AppendLine($"App Status: {(app.AppRunningState ? "RUNNING" : "STOPPED")}");
                        logContent.AppendLine("".PadRight(80, '='));
                        logContent.AppendLine();
                        
                        if (!string.IsNullOrEmpty(app.AppMonitor))
                        {
                            logContent.AppendLine("Console Output:");
                            logContent.AppendLine(app.AppMonitor);
                        }
                        else
                        {
                            logContent.AppendLine("No console output available.");
                        }
                    }
                    else
                    {
                        logContent.AppendLine($"App '{currentConsoleTab}' not found.");
                    }
                }
                
                // Write to file
                await System.IO.File.WriteAllTextAsync(fullPath, logContent.ToString());
                
                await RenderMessageBox("Export Success", 
                    $"Console log exported successfully to:\n{fullPath}", 
                    MsBox.Avalonia.Enums.Icon.Success);
            }
            catch (Exception ex)
            {
                await RenderMessageBox("Export Error", 
                    $"Failed to export console log:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void ConsoleAutoScrollCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            isAutoScrollEnabled = ConsoleAutoScrollCheckBox.IsChecked ?? true;
            
            // If auto-scroll was re-enabled, scroll to bottom
            if (isAutoScrollEnabled)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Scroll the current tab to bottom
                    if (!string.IsNullOrEmpty(currentConsoleTab) && consoleTabs.TryGetValue(currentConsoleTab, out var tab))
                    {
                        var scrollViewer = tab.Content as ScrollViewer;
                        scrollViewer?.ScrollToEnd();
                    }
                });
            }
        }

        private async Task LoadChangelogContent()
        {
            try
            {
                var changelogUrl = "https://raw.githubusercontent.com/lbormann/darts-hub/main/CHANGELOG.md";
                var changelogText = await Helper.AsyncHttpGet(changelogUrl, 10);
                
                if (string.IsNullOrEmpty(changelogText))
                {
                    changelogText = "Changelog not available. Please check your internet connection and try again.";
                }
                
                ChangelogContent.Text = changelogText;
            }
            catch (Exception ex)
            {
                ChangelogContent.Text = $"Error loading changelog: {ex.Message}";
            }
        }

        private void Comboboxportal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedProfile != null) selectedProfile.IsTaggedForStart = false;
            selectedProfile = ((ComboBoxItem)Comboboxportal.SelectedItem).Tag as Profile;
            if (selectedProfile == null) return;
            selectedProfile.IsTaggedForStart = true;
            RenderAppNavigation();
            Save();
        }

        private void CheckBoxStartProfileOnProgramStartChanged(object sender, RoutedEventArgs e)
        {
            configurator.Settings.StartProfileOnStart = (bool)CheckBoxStartProfileOnProgramStart.IsChecked;
            configurator.SaveSettings();
        }

        private async void ShowCorruptedConfigHandlingBox(ConfigurationException ex)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                IsEnabled = false;
                Opacity = 0.25;

                var result = await RenderMessageBox("", 
                    $"Configuration file '{ex.File}' not readable ('{ex.Message}'). " +
                    "You can fix it by yourself or let it be recreated. " +
                    "Do you want to reset it? (All settings will be lost)", 
                    MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.YesNo);
                    
                if (result == ButtonResult.Yes)
                {
                    try
                    {
                        profileManager.DeleteConfigurationFile(ex.File);
                    }
                    catch (Exception e)
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Error", 
                            "Configuration file deletion failed. Please delete it manually. " + e.Message)
                            .ShowWindowAsync();
                    }
                }
                await RenderMessageBox("", "Application will close now. Please restart it.", MsBox.Avalonia.Enums.Icon.Warning);
                Environment.Exit(1);
            });
        }

        private async Task<ButtonResult> RenderMessageBox(string title = "",
                                                          string message = "",
                                                          MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None,
                                                          ButtonEnum buttons = ButtonEnum.Ok,
                                                          double width = -1,
                                                          double height = -1,
                                                          int autoCloseDelayInSeconds = 0)
        {
            if (width < 0) width = Width / 1.3;
            if (height < 0) height = Height / 1.3;

            var buttonDefinitions = new List<ButtonDefinition>();
            switch (buttons)
            {
                case ButtonEnum.Ok:
                    buttonDefinitions.Add(new ButtonDefinition { Name = "Ok", IsDefault = true });
                    break;
                case ButtonEnum.YesNo:
                    buttonDefinitions.Add(new ButtonDefinition { Name = "Yes", IsDefault = true });
                    buttonDefinitions.Add(new ButtonDefinition { Name = "No", IsCancel = true });
                    break;
            }

            var messageBoxParams = new MessageBoxCustomParams
            {
                Icon = icon,
                WindowIcon = Icon,
                Width = width,
                Height = height,
                MaxWidth = width,
                MaxHeight = height,
                CanResize = (width == -1) ? false : true,
                SystemDecorations = SystemDecorations.Full,
                WindowStartupLocation = WindowStartupLocation,
                ButtonDefinitions = buttonDefinitions,
                ContentTitle = title,
                ContentMessage = message
            };

            var msBoxWindow = MessageBoxManager.GetMessageBoxCustom(messageBoxParams);
            var result = await msBoxWindow.ShowWindowAsync();

            if (autoCloseDelayInSeconds > 0)
            {
                await Task.Delay(autoCloseDelayInSeconds * 1000);
                ((Window)msBoxWindow).Close();
                return ButtonResult.No;
            }
            
            return result switch
            {
                "Ok" => ButtonResult.Ok,
                "Yes" => ButtonResult.Yes,
                "No" => ButtonResult.No,
                _ => ButtonResult.None
            };
        }

        // Event handlers for updates and apps
        private async void Updater_NoNewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SetWait(false);
                if (configurator.Settings.StartProfileOnStart) RunSelectedProfile();
            });
        }

        private async void Updater_NewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var update = ButtonResult.No;
                if (!configurator.Settings.SkipUpdateConfirmation)
                {
                    update = await RenderMessageBox($"Update available",
                        $"New Version '{e.Version}' available!\r\n\r\nDO YOU WANT TO UPDATE?\r\n\r\n" +
                        $"------------------  CHANGELOG  ------------------\r\n\r\n{e.Message}",
                        MsBox.Avalonia.Enums.Icon.Success, ButtonEnum.YesNo, 600.0, 800.0);
                }
                else
                {
                    update = ButtonResult.Yes;
                }
                
                if (update == ButtonResult.Yes)
                {
                    try
                    {
                        Updater.UpdateToNewVersion();
                    }
                    catch (Exception ex)
                    {
                        await RenderMessageBox("", "Update to new version failed: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                    }
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        SetWait(false);
                        if (configurator.Settings.StartProfileOnStart) RunSelectedProfile();
                    });
                }
            });
        }

        private void Updater_ReleaseDownloadStarted(object? sender, ReleaseEventArgs e)
        {
            SetWait(true, "Downloading " + e.Version + "...");
        }

        private async void Updater_ReleaseDownloadFailed(object? sender, ReleaseEventArgs e)
        {
            await RenderMessageBox("", "Check or update to new version failed: " + e.Message, MsBox.Avalonia.Enums.Icon.Error, autoCloseDelayInSeconds: 5);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SetWait(false);
                if (configurator.Settings.StartProfileOnStart) RunSelectedProfile();
            });
        }

        private void Updater_ReleaseDownloadProgressed(object? sender, DownloadProgressChangedEventArgs e)
        {
            SetWait(true);
        }

        private void Updater_ReleaseInstallInitialized(object? sender, ReleaseEventArgs e)
        {
            Close();
        }
    }
}
