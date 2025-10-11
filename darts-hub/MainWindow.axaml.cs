using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using darts_hub.control;
using darts_hub.control.wizard;
using darts_hub.model;
using darts_hub.UI;
using darts_hub.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using MsBox.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace darts_hub
{
    public partial class MainWindow : Window
    {
        #region Constants and Fields
        private const string ConfigPath = "config.json";
        private readonly double originalAspectRatio = 1004.0 / 800.0;

        // Core managers
        private readonly ConsoleManager consoleManager;
        private readonly NavigationManager navigationManager;
        private readonly ContentModeManager contentModeManager;
        
        // Core components
        private ProfileManager profileManager;
        private Profile? selectedProfile;
        private AppBase? selectedApp;
        private Configurator configurator;
        private ReadmeParser readmeParser;
        private Dictionary<string, string>? currentTooltips;
        #endregion

        #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();
            
            configurator = new Configurator("config.json");
            readmeParser = new ReadmeParser();
            
            // Initialize managers
            consoleManager = new ConsoleManager();
            navigationManager = new NavigationManager();
            contentModeManager = new ContentModeManager(configurator);
            
            InitializeViewModel();
            InitializeWindowSettings();
            InitializeManagers();
            SetupEventHandlers();
        }

        private void InitializeViewModel()
        {
            var viewModel = new UpdaterViewModel
            {
                IsBetaTester = configurator.Settings.IsBetaTester
            };
            DataContext = viewModel;
        }

        private void InitializeWindowSettings()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowResizeHelper.SetupProportionalResize(this, originalAspectRatio, true, false);
        }

        private void InitializeManagers()
        {
            // Initialize console manager
            consoleManager.Initialize();
            consoleManager.ConsoleTabControl = ConsoleTabControl;
            
            // Initialize navigation manager
            navigationManager.Initialize(
                refreshSettings: () => RefreshCurrentAppSettings(),
                appSelected: async (app) => await OnAppSelected(app),
                save: () => Save()
            );
            navigationManager.AppNavigationPanel = AppNavigationPanel;
            
            // Initialize content mode manager
            InitializeContentModeManager();
        }

        private void InitializeContentModeManager()
        {
            contentModeManager.MainGrid = MainGrid;
            contentModeManager.ConsolePanel = ConsolePanel;
            contentModeManager.ChangelogScrollViewer = ChangelogScrollViewer;
            contentModeManager.AboutScrollViewer = AboutScrollViewer;
            contentModeManager.SettingsScrollViewer = SettingsScrollViewer;
            contentModeManager.NewSettingsPanel = NewSettingsPanel;
            contentModeManager.TooltipPanel = TooltipPanel;
            contentModeManager.TooltipSplitter = TooltipSplitter;
            contentModeManager.TooltipTitle = TooltipTitle;
            contentModeManager.TooltipDescription = TooltipDescription;
            contentModeManager.NewSettingsScrollViewer = NewSettingsScrollViewer;
            contentModeManager.ButtonConsole = ButtonConsole;
            contentModeManager.ButtonChangelog = ButtonChangelog;
            contentModeManager.ButtonAbout = this.FindControl<Button>("Buttonabout");
        }

        private void SetupEventHandlers()
        {
            Opened += MainWindow_Opened;
            Closing += Window_Closing;
            
            // Console events
            ConsoleTabControl.SelectionChanged += (s, e) => consoleManager.OnTabSelectionChanged(s, e);
        }
        #endregion

        #region Window Event Handlers
        private async void MainWindow_Opened(object sender, EventArgs e)
        {
            try
            {
                await InitializeApplication();
            }
            catch (ConfigurationException ex)
            {
                SetWait(false);
                await ShowCorruptedConfigHandlingBox(ex);
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
                // Unsubscribe from events
                RetryHelper.RetryProgressChanged -= RetryHelper_ProgressChanged;
                
                consoleManager?.Dispose();
                navigationManager?.Dispose();
                profileManager?.CloseApps();
            }
            catch (Exception ex)
            {
                // Use fire-and-forget for closing events to prevent hanging
                _ = Task.Run(async () => await RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error));
            }
        }
        #endregion

        #region Application Initialization
        private async Task InitializeApplication()
        {
            configurator = new(ConfigPath);
            CheckBoxStartProfileOnProgramStart.IsChecked = configurator.Settings.StartProfileOnStart;
            
            // Initialize About content with app version and settings
            InitializeAboutContent();

            await InitializeProfileManager();
            await InitializeUpdater();
            
            // Check if wizard should be shown
            await CheckAndShowWizard();
        }

        private void InitializeAboutContent()
        {
            try
            {
                // Set the app version
                AboutAppVersion.Content = Updater.version;
                
                // Set the skip update confirmation checkbox
                AboutCheckBoxSkipUpdateConfirmation.IsChecked = configurator.Settings.SkipUpdateConfirmation;
                
                // Set the new settings mode checkbox
                AboutCheckBoxNewSettingsMode.IsChecked = configurator.Settings.NewSettingsMode;
                
                // Show the About content by default
                contentModeManager.ShowAboutMode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing About content: {ex.Message}");
            }
        }

        private async Task InitializeProfileManager()
        {
            profileManager = new ProfileManager();
            SetupProfileManagerEvents();

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
        }

        private async Task InitializeUpdater()
        {
            SetupUpdaterEvents();
            SetupRetryProgressEvents();
            Updater.CheckNewVersion();
            SetWait(true, "Checking for update...");
        }
        #endregion

        #region Button Event Handlers
        private void Buttonstart_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedProfile(true);
        }

        private async void Buttonabout_Click(object sender, RoutedEventArgs e)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.About)
            {
                contentModeManager.ShowSettingsMode();
                consoleManager.Stop();
            }
            else
            {
                contentModeManager.ShowAboutMode();
                consoleManager.Stop();
            }
        }

        private void ButtonConsole_Click(object sender, RoutedEventArgs e)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Console)
            {
                contentModeManager.ShowSettingsMode();
                consoleManager.Stop();
            }
            else
            {
                contentModeManager.ShowConsoleMode();
                consoleManager.Start();
                consoleManager.SetSelectedProfile(selectedProfile);
            }
        }

        private async void ButtonChangelog_Click(object sender, RoutedEventArgs e)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Changelog)
            {
                contentModeManager.ShowSettingsMode();
                consoleManager.Stop();
            }
            else
            {
                contentModeManager.ShowChangelogMode();
                consoleManager.Stop();
                await LoadChangelogContent();
            }
        }

        private void CheckBoxStartProfileOnProgramStartChanged(object sender, RoutedEventArgs e)
        {
            if (CheckBoxStartProfileOnProgramStart.IsChecked.HasValue)
            {
                configurator.Settings.StartProfileOnStart = CheckBoxStartProfileOnProgramStart.IsChecked.Value;
                configurator.SaveSettings();
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button helpButton) return;

            switch (helpButton.Name)
            {
                case "AboutContact1":
                    VisitHelpPage("https://discordapp.com/users/Reepa86#1149");
                    break;
                case "AboutContact2":
                    VisitHelpPage("https://discordapp.com/users/wusaaa#0578");
                    break;
                case "AboutContact3":
                    VisitHelpPage("https://discordapp.com/users/366537096414101504");
                    break;
                case "AboutPaypal":
                    VisitHelpPage("https://paypal.me/I3ull3t");
                    break;
                case "AboutDonation":
                    await HandleBitcoinDonation();
                    break;
                case "AboutBug":
                    VisitHelpPage("https://github.com/lbormann/darts-hub/issues");
                    break;
                case "AboutChangelog":
                    contentModeManager.ShowChangelogMode();
                    consoleManager.Stop();
                    await LoadChangelogContent();
                    break;
            }
        }

        private async Task HandleBitcoinDonation()
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                const string donationAddress = "bc1qr7wsvmmgaj6dle8gae2dl0dcxu5yh8vqlv34x4";
                await clipboard.SetTextAsync(donationAddress);
                await MessageBoxManager.GetMessageBoxStandard("Bitcoin donation address copied", 
                    $"Address copied to clipboard:\n{donationAddress}").ShowWindowAsync();
            }
        }

        private void AboutCheckBoxSkipUpdateConfirmationChanged(object sender, RoutedEventArgs e)
        {
            configurator.Settings.SkipUpdateConfirmation = (bool)AboutCheckBoxSkipUpdateConfirmation.IsChecked;
            configurator.SaveSettings();
        }
        
        private void AboutCheckBoxNewSettingsModeChanged(object sender, RoutedEventArgs e)
        {
            configurator.Settings.NewSettingsMode = (bool)AboutCheckBoxNewSettingsMode.IsChecked;
            configurator.SaveSettings();
        }

        private void NewSettingsBackButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch back to classic mode
            configurator.Settings.NewSettingsMode = false;
            configurator.SaveSettings();
            
            // Update the checkbox in About section
            AboutCheckBoxNewSettingsMode.IsChecked = false;
            
            // Re-render current app settings if any app is selected
            if (selectedApp != null)
            {
                _ = Task.Run(async () =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await RenderAppSettings(selectedApp);
                    });
                });
            }
            else
            {
                // Just switch modes
                contentModeManager.ShowClassicSettingsMode();
            }
        }

        private void ToTopButton_Click(object sender, RoutedEventArgs e)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Settings)
            {
                ScrollViewer scrollViewer;
                if (configurator.Settings.NewSettingsMode)
                {
                    scrollViewer = NewSettingsScrollViewer;
                }
                else
                {
                    scrollViewer = SettingsScrollViewer;
                }
                
                if (scrollViewer == null) return;
                
                AnimateScrollToTop(scrollViewer);
            }
        }

        private void AnimateScrollToTop(ScrollViewer scrollViewer)
        {
            var startOffset = scrollViewer.Offset.Y;
            var duration = TimeSpan.FromMilliseconds(500);
            var startTime = DateTime.Now;

            var scrollTimer = new Timer(16); // ~60 FPS
            scrollTimer.Elapsed += (s, args) =>
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(elapsed.TotalMilliseconds / duration.TotalMilliseconds, 1.0);
                
                var easedProgress = 1 - Math.Pow(1 - progress, 3);
                var currentOffset = startOffset * (1 - easedProgress);
                
                Dispatcher.UIThread.Post(() =>
                {
                    scrollViewer.Offset = scrollViewer.Offset.WithY(currentOffset);
                    
                    if (progress >= 1.0)
                    {
                        scrollTimer.Stop();
                        scrollTimer.Dispose();
                    }
                });
            };
            scrollTimer.Start();
        }

        private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateToTopButtonVisibility(sender as ScrollViewer);
        }

        private void NewSettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateToTopButtonVisibility(sender as ScrollViewer);
        }

        private void UpdateToTopButtonVisibility(ScrollViewer? scrollViewer)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Settings && scrollViewer != null)
            {
                const double showThreshold = 100.0;
                bool shouldShow = scrollViewer.Offset.Y > showThreshold;
                var toTopButton = this.FindControl<Button>("ToTopButton");
                if (toTopButton != null)
                {
                    if (shouldShow && !toTopButton.IsVisible)
                    {
                        toTopButton.IsVisible = true;
                        toTopButton.Opacity = 0.9;
                    }
                    else if (!shouldShow && toTopButton.IsVisible)
                    {
                        toTopButton.IsVisible = false;
                        toTopButton.Opacity = 0.0;
                    }
                }
            }
        }

        private async void SetupWizardButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowSetupWizard();
        }
        #endregion

        #region Console Event Handlers
        private void ConsoleClearButton_Click(object sender, RoutedEventArgs e)
        {
            consoleManager.ClearAllConsoles();
        }

        private void ConsoleClearCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            consoleManager.ClearCurrentConsole();
        }

        private async void ConsoleExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = consoleManager.ExportCurrentConsole();
                if (filePath != null)
                {
                    await RenderMessageBox("Export Success", 
                        $"Console log exported successfully to:\n{filePath}", 
                        MsBox.Avalonia.Enums.Icon.Success);
                }
                else
                {
                    await RenderMessageBox("Export Error", "No console tab selected or profile active.", 
                        MsBox.Avalonia.Enums.Icon.Warning);
                }
            }
            catch (Exception ex)
            {
                await RenderMessageBox("Export Error", 
                    $"Failed to export console log:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private async void ConsoleTestUpdaterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdaterLogger.LogInfo("Opening updater test window from console");
                
                var testWindow = new UpdaterTestWindow()
                {
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner
                };
                
                // Use ShowDialog with this window as owner
                await testWindow.ShowDialog(this);
                
                UpdaterLogger.LogInfo("Updater test window closed successfully");
            }
            catch (System.NotSupportedException ex) when (ex.Message.Contains("Markdown.Avalonia"))
            {
                // Handle the specific markdown binding issue - run a quick test instead
                UpdaterLogger.LogError("Markdown binding issue detected, running quick test instead", ex);
                
                SetWait(true, "Running quick updater test...");
                
                try
                {
                    var quickResults = await UpdaterTestRunner.RunQuickVersionTest();
                    var connectivityResults = await UpdaterTestRunner.RunConnectivityTest();
                    
                    var combinedResults = "=== QUICK TEST RESULTS ===\n\n" +
                                         "VERSION CHECK:\n" + quickResults + "\n" +
                                         "CONNECTIVITY:\n" + connectivityResults + "\n" +
                                         "=== TEST COMPLETED ===\n\n" +
                                         "For more detailed tests use:\n" +
                                         "• dotnet run -- --full (Full Test)\n" +
                                         "• dotnet run -- --version (Version Check)\n" +
                                         "• dotnet run -- --retry (Retry Test)";
                    
                    SetWait(false);
                    
                    await RenderMessageBox("Updater Quick Test", combinedResults, 
                        MsBox.Avalonia.Enums.Icon.Info, ButtonEnum.Ok, 500, 400);
                }
                catch (Exception quickTestEx)
                {
                    SetWait(false);
                    UpdaterLogger.LogError("Quick test also failed", quickTestEx);
                    
                    await RenderMessageBox("Test Interface Error", 
                        "The test interface could not be opened and the quick test also failed.\n\n" +
                        "Please use command line tests:\n\n" +
                        "• dotnet run -- --full (Full Test)\n" +
                        "• dotnet run -- --version (Version Check)\n" +
                        "• dotnet run -- --retry (Retry Test)\n\n" +
                        $"Error details:\n{quickTestEx.Message}", 
                        MsBox.Avalonia.Enums.Icon.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Failed to open updater test window", ex);
                
                // Try quick test as fallback
                SetWait(true, "Running fallback quick test...");
                
                try
                {
                    var quickResults = await UpdaterTestRunner.RunQuickVersionTest();
                    
                    SetWait(false);
                    
                    await RenderMessageBox("Updater Quick Test (Fallback)", 
                        "The test interface could not be opened.\n" +
                        "Here are the results of a quick test:\n\n" +
                        quickResults + "\n" +
                        "For more detailed tests use command line:\n" +
                        "• dotnet run -- --full\n" +
                        "• dotnet run -- --version\n" +
                        "• dotnet run -- --retry", 
                        MsBox.Avalonia.Enums.Icon.Info);
                }
                catch (Exception fallbackEx)
                {
                    SetWait(false);
                    
                    await RenderMessageBox("Test Error", 
                        $"Error opening test interface:\n{ex.Message}\n\n" +
                        $"Fallback test also failed:\n{fallbackEx.Message}\n\n" +
                        "Please use command line tests:\n" +
                        "• dotnet run -- --full\n" +
                        "• dotnet run -- --version\n" +
                        "• dotnet run -- --retry", 
                        MsBox.Avalonia.Enums.Icon.Error);
                }
            }
        }

        private void ConsoleAutoScrollCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            consoleManager.IsAutoScrollEnabled = ConsoleAutoScrollCheckBox.IsChecked == true;
        }
        #endregion

        #region Profile and App Management
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
        }

        private void Comboboxportal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Comboboxportal.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is Profile profile)
            {
                selectedProfile = profile;
                navigationManager.SetSelectedProfile(profile);
                consoleManager.SetSelectedProfile(profile);
                
                SettingsPanel.Children.Clear();
                selectedApp = null;
                
                if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Console)
                {
                    consoleManager.InitializeConsoleTabs();
                }
            }
        }

        private async Task OnAppSelected(AppBase app)
        {
            selectedApp = app;

            // Show settings mode if not already
            if (contentModeManager.CurrentContentMode != ContentModeManager.ContentMode.Settings)
            {
                contentModeManager.ShowSettingsMode();
                consoleManager.Stop();
            }
            
            // Render app settings
            await RenderAppSettings(app);
        }

        private void RefreshCurrentAppSettings()
        {
            if (selectedApp != null && contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Settings)
            {
                _ = Task.Run(async () =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await RenderAppSettings(selectedApp);
                    });
                });
            }
        }
        #endregion

        #region App Settings Rendering
        private async Task RenderAppSettings(AppBase app)
        {
            SettingsPanel.Children.Clear();
            NewSettingsContent.Children.Clear();
            selectedApp = app;
            
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
                    contentModeManager.ShowNewSettingsMode();
                    NewSettingsContent.Children.Add(message);
                }
                else
                {
                    contentModeManager.ShowClassicSettingsMode();
                    SettingsPanel.Children.Add(message);
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
            contentModeManager.ShowNewSettingsMode();
            
            // Load new settings content with save callback
            var newSettingsContent = await NewSettingsContentProvider.CreateNewSettingsContent(app, () => Save());
            
            // Clear existing content and add new content
            NewSettingsContent.Children.Clear();
            if (newSettingsContent is StackPanel newPanel)
            {
                // Copy children from the created content to our NewSettingsContent panel
                while (newPanel.Children.Count > 0)
                {
                    var child = newPanel.Children[0];
                    newPanel.Children.RemoveAt(0);
                    NewSettingsContent.Children.Add(child);
                }
            }
            else
            {
                NewSettingsContent.Children.Add(newSettingsContent);
            }
        }

        private async Task RenderClassicSettingsMode(AppBase app)
        {
            contentModeManager.ShowClassicSettingsMode();
            
            // Load tooltips for this app
            await LoadTooltipsForApp(app);

            // Create header with app controls
            var headerPanel = CreateAppSettingsHeader(app);
            SettingsPanel.Children.Add(headerPanel);

            // Add Autostart section
            var autostartSection = CreateAutostartSection(app);
            SettingsPanel.Children.Add(autostartSection);

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
                await HandleStartStopApp(app);
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
                await HandleRestartApp(app);
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
                    
                    await RenderMessageBox("Changelog", changelogText, MsBox.Avalonia.Enums.Icon.None, ButtonEnum.Ok, Width, Height);
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
                        RenderMessageBox("Error", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                    }
                };
                helpButtonPanel.Children.Add(helpBtn);
            }

            return helpButtonPanel;
        }

        private async Task RenderConfigurationSections(AppBase app)
        {
            var appConfiguration = app.Configuration;
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
                SettingsPanel.Children.Add(expander);
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
                Save();
            };
            
            autostartCheckBox.Unchecked += (s, e) => 
            {
                appState.TaggedForStart = false;
                Save();
            };

            autostartPanel.Children.Add(autostartCheckBox);
            return autostartPanel;
        }

        private async Task<Control?> CreateArgumentControl(Argument argument)
        {
            var argumentControlFactory = new ArgumentControlFactory(this);
            return await argumentControlFactory.CreateControl(argument, AutoSaveConfiguration, ShowTooltip);
        }

        private void AutoSaveConfiguration(Argument argument)
        {
            try
            {
                argument.IsValueChanged = true;
                Save();
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
                if (TooltipDescription == null) return;

                // Inlines leeren, um vorherigen Text zu entfernen
                TooltipDescription.Inlines.Clear();

                // Argument-Namen in fett hinzufügen
                TooltipDescription.Inlines.Add(new Run(argument.NameHuman) { FontWeight = FontWeight.Bold });

                // Doppelpunkt als Trenner
                TooltipDescription.Inlines.Add(new Run(": "));
                TooltipDescription.Inlines.Add(new LineBreak());
                TooltipDescription.Inlines.Add(new LineBreak());

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

                TooltipDescription.Inlines.Add(new Run(description));
            }
            catch (Exception ex)
            {
                if (TooltipDescription != null)
                    TooltipDescription.Text = "Error loading tooltip.";
                System.Diagnostics.Debug.WriteLine($"Error showing tooltip: {ex.Message}");
            }
        }
        #endregion

        #region App Control Actions
        private async Task HandleStartStopApp(AppBase app)
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
                
                // Save changes
                Save();
            }
            catch (Exception ex)
            {
                await RenderMessageBox("Error", $"Failed to {(app.AppRunningState ? "stop" : "start")} {app.CustomName}:\n{ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private async Task HandleRestartApp(AppBase app)
        {
            if (!app.AppRunningState) return;
            
            try
            {
                SetWait(true, $"Restarting {app.CustomName}...");
                
                if (app.AppRunningState)
                {
                    app.Close();
                    await Task.Delay(2000);
                }
                
                app.Run();
                
                int attempts = 0;
                bool startedSuccessfully = false;
                while (attempts < 10)
                {
                    await Task.Delay(500);
                    attempts++;
                    if (app.AppRunningState)
                    {
                        startedSuccessfully = true;
                        break;
                    }
                }
                
                SetWait(false);
                
                if (!startedSuccessfully)
                {
                    await RenderMessageBox("Warning", $"Restart initiated for {app.CustomName}, but the app may not have started properly. Please check the console for details.", MsBox.Avalonia.Enums.Icon.Warning);
                }
                
                await RenderAppSettings(app);
                Save();
            }
            catch (Exception ex)
            {
                SetWait(false);
                await RenderMessageBox("Error", $"Failed to restart {app.CustomName}:\n{ex.Message}", MsBox.Avalonia.Enums.Icon.Error);
                await RenderAppSettings(app);
            }
        }
        #endregion

        #region ProfileManager Event Handlers
        private void SetupProfileManagerEvents()
        {
            profileManager.AppDownloadStarted += ProfileManager_AppDownloadStarted;
            profileManager.AppDownloadFinished += ProfileManager_AppDownloadFinished;
            profileManager.AppDownloadFailed += ProfileManager_AppDownloadFailed;
            profileManager.AppDownloadProgressed += ProfileManager_AppDownloadProgressed;
            profileManager.AppInstallStarted += ProfileManager_AppInstallStarted;
            profileManager.AppInstallFinished += ProfileManager_AppInstallFinished;
            profileManager.AppInstallFailed += ProfileManager_AppInstallFailed;
            profileManager.AppConfigurationRequired += ProfileManager_AppConfigurationRequired;
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
        #endregion

        #region Updater Event Handlers
        private void SetupUpdaterEvents()
        {
            Updater.NewReleaseFound += Updater_NewReleaseFound;
            Updater.NoNewReleaseFound += Updater_NoNewReleaseFound;
            Updater.ReleaseInstallInitialized += Updater_ReleaseInstallInitialized;
            Updater.ReleaseDownloadStarted += Updater_ReleaseDownloadStarted;
            Updater.ReleaseDownloadFailed += Updater_ReleaseDownloadFailed;
            Updater.ReleaseDownloadProgressed += Updater_ReleaseDownloadProgressed;
        }

        private void SetupRetryProgressEvents()
        {
            RetryHelper.RetryProgressChanged += RetryHelper_ProgressChanged;
        }

        private async void RetryHelper_ProgressChanged(object? sender, RetryProgressEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetWait(true, e.Message);
            });
        }

        private async void Updater_NoNewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SetWait(false);
                
                // Check if wizard should be shown first
                if (!configurator.Settings.WizardCompleted && selectedProfile != null)
                {
                    return;
                }
                
                // Only auto-start if no wizard is needed
                if (configurator.Settings.StartProfileOnStart) 
                {
                    RunSelectedProfile();
                }
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
                        MsBox.Avalonia.Enums.Icon.Success, ButtonEnum.YesNo, 800.0, 600.0);
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
                    await HandleNoUpdateSelected();
                }
            });
        }

        private async Task HandleNoUpdateSelected()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SetWait(false);
                
                // Check if wizard should be shown first
                if (!configurator.Settings.WizardCompleted && selectedProfile != null)
                {
                    return;
                }
                
                // Only auto-start if no wizard is needed
                if (configurator.Settings.StartProfileOnStart) 
                {
                    RunSelectedProfile();
                }
            });
        }

        private async void Updater_ReleaseDownloadStarted(object? sender, ReleaseEventArgs e)
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
        #endregion

        #region Content Loading and Utility Methods
        private async Task LoadChangelogContent()
        {
            try
            {
                var changelogText = await Helper.AsyncHttpGet("https://raw.githubusercontent.com/lbormann/darts-hub/main/CHANGELOG.md", 4);
                if (string.IsNullOrEmpty(changelogText))
                    changelogText = "Changelog not available. Please try again later.";
       
                ChangelogContent.Text = changelogText;
            }
            catch (Exception ex)
            {
                ChangelogContent.Text = $"Failed to load changelog: {ex.Message}";
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

        private void VisitHelpPage(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _ = RenderMessageBox("Error", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void Save()
        {
            try
            {
                profileManager.StoreApps();
            }
            catch (Exception ex)
            {
                _ = RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
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
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Console && !wait)
            {
                consoleManager.UpdateContent();
            }
        }

        private async Task<ButtonResult> RenderMessageBox(string title, string message, MsBox.Avalonia.Enums.Icon icon, ButtonEnum buttons = ButtonEnum.Ok, double? width = null, double? height = null, int autoCloseDelayInSeconds = 0)
        {
            try
            {
                var messageBoxParams = new MessageBoxStandardParams
                {
                    ContentTitle = title,
                    ContentMessage = message,
                    Icon = icon,
                    ButtonDefinitions = buttons,
                    WindowIcon = Icon,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                if (width.HasValue)
                    messageBoxParams.Width = width.Value;
                if (height.HasValue)
                    messageBoxParams.Height = height.Value;

                var messageBox = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);
                
                if (autoCloseDelayInSeconds > 0)
                {
                    _ = Task.Delay(TimeSpan.FromSeconds(autoCloseDelayInSeconds)).ContinueWith(_ =>
                    {
                        // The MessageBox will auto-close on timeout
                    });
                }

                return await messageBox.ShowWindowDialogAsync(this);
            }
            catch (System.NotSupportedException ex) when (ex.Message.Contains("Markdown.Avalonia") || ex.Message.Contains("StaticBinding"))
            {
                // Fallback to simple Avalonia MessageBox for markdown binding issues
                UpdaterLogger.LogWarning($"MessageBox markdown binding issue, using fallback: {ex.Message}");
                return await ShowFallbackMessageBox(title, message, buttons);
            }
            catch (Exception ex)
            {
                // Log the error and show a simple fallback
                UpdaterLogger.LogError("MessageBox failed completely, using system fallback", ex);
                return await ShowFallbackMessageBox(title, message, buttons);
            }
        }

        private async Task<ButtonResult> ShowFallbackMessageBox(string title, string message, ButtonEnum buttons)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // Determine appropriate size based on content
                    double dialogWidth = 500;
                    double dialogHeight = 400;
                    
                    // For update messages (longer content), use larger dialog
                    if (title.Contains("Update") || message.Length > 500)
                    {
                        dialogWidth = 900;
                        dialogHeight = 700;
                    }
                    
                    // Create a simple custom dialog
                    var dialog = new Window
                    {
                        Title = title,
                        Width = dialogWidth,
                        Height = dialogHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        CanResize = true,
                        ShowInTaskbar = false,
                        MinWidth = 400,
                        MinHeight = 300
                    };

                    var mainGrid = new Grid();
                    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
                    mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                    // Scrollable message content
                    var scrollViewer = new ScrollViewer
                    {
                        Margin = new Thickness(20),
                        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                    };

                    var messageText = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        FontSize = 14
                    };
                    
                    scrollViewer.Content = messageText;
                    Grid.SetRow(scrollViewer, 0);
                    mainGrid.Children.Add(scrollViewer);

                    // Button panel at bottom
                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 15,
                        Margin = new Thickness(20)
                    };
                    
                    Grid.SetRow(buttonPanel, 1);
                    mainGrid.Children.Add(buttonPanel);

                    ButtonResult result = ButtonResult.Ok;

                    if (buttons == ButtonEnum.YesNo)
                    {
                        var yesButton = new Button
                        {
                            Content = "Yes",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        yesButton.Click += (s, e) =>
                        {
                            result = ButtonResult.Yes;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(yesButton);

                        var noButton = new Button
                        {
                            Content = "No",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        noButton.Click += (s, e) =>
                        {
                            result = ButtonResult.No;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(noButton);
                    }
                    else
                    {
                        var okButton = new Button
                        {
                            Content = "OK",
                            Width = 100,
                            Height = 35,
                            FontSize = 14,
                            Margin = new Thickness(5)
                        };
                        okButton.Click += (s, e) =>
                        {
                            result = ButtonResult.Ok;
                            dialog.Close();
                        };
                        buttonPanel.Children.Add(okButton);
                    }

                    dialog.Content = mainGrid;

                    await dialog.ShowDialog(this);
                    return result;
                }
                catch (Exception ex)
                {
                    // Last resort fallback
                    UpdaterLogger.LogError("Even fallback dialog failed", ex);
                    System.Diagnostics.Debug.WriteLine($"MessageBox Error: {title} - {message}");
                    return ButtonResult.Ok;
                }
            });
        }
        #endregion

        #region Wizard Management
        /// <summary>
        /// Checks if the setup wizard should be shown and displays it if needed
        /// </summary>
        private async Task CheckAndShowWizard()
        {
            // Show wizard if it hasn't been completed and we have profiles loaded
            if (!configurator.Settings.WizardCompleted && selectedProfile != null)
            {
                // Check if there are any configurable apps in the profile
                bool hasConfigurableApps = selectedProfile.Apps.Values.Any(appState => 
                    appState.App.IsConfigurable() || 
                    appState.App.CustomName.ToLower().Contains("caller"));

                if (hasConfigurableApps)
                {
                    // Show the wizard and prevent auto-minimizing
                    await ShowSetupWizard();
                    return; // Don't continue with normal startup flow
                }
            }
        }

        /// <summary>
        /// Shows the setup wizard
        /// </summary>
        private async Task ShowSetupWizard()
        {
            try
            {
                if (selectedProfile == null)
                {
                    await RenderMessageBox("Setup Wizard", 
                        "Please select a profile before running the setup wizard.", 
                        MsBox.Avalonia.Enums.Icon.Warning);
                    return;
                }

                var wizardManager = new SetupWizardManager(profileManager, configurator);
                wizardManager.InitializeWizardSteps(selectedProfile);
                
                var result = await wizardManager.ShowWizard(this);
                
                if (result)
                {
                    // Wizard completed successfully
                    await RenderMessageBox("Setup Complete", 
                        "Your darts applications have been configured successfully!", 
                        MsBox.Avalonia.Enums.Icon.Success);
                    
                    // Refresh the UI
                    navigationManager.RenderAppNavigation();
                    
                    // Re-render current app settings if any app is selected
                    if (selectedApp != null)
                    {
                        await RenderAppSettings(selectedApp);
                    }
                }
                else
                {
                    // Wizard was cancelled - ask if they want to run it again later
                    var laterResult = await RenderMessageBox("Setup Wizard", 
                        "Setup wizard was cancelled. You can run it again anytime from the About section.\n\nWould you like to mark the wizard as completed anyway?", 
                        MsBox.Avalonia.Enums.Icon.Question, 
                        ButtonEnum.YesNo);
                    
                    if (laterResult == ButtonResult.Yes)
                    {
                        configurator.Settings.WizardCompleted = true;
                        configurator.SaveSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                await RenderMessageBox("Setup Wizard Error", 
                    $"An error occurred while running the setup wizard:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }
        #endregion

        #region Configuration Error Handling
        private async Task ShowCorruptedConfigHandlingBox(ConfigurationException ex)
        {
            var result = await RenderMessageBox("Configuration Error", 
                $"Configuration file is corrupted:\n{ex.Message}\n\nWould you like to reset to default settings?",
                MsBox.Avalonia.Enums.Icon.Error, 
                ButtonEnum.YesNo);

            if (result == ButtonResult.Yes)
            {
                try
                {
                    configurator = new Configurator("config.json");
                    await RenderMessageBox("", "Configuration has been reset to defaults.", MsBox.Avalonia.Enums.Icon.Info);
                }
                catch (Exception resetEx)
                {
                    await RenderMessageBox("", "Failed to reset configuration: " + resetEx.Message, MsBox.Avalonia.Enums.Icon.Error);
                    Environment.Exit(1);
                }
            }
            else
            {
                Environment.Exit(1);
            }
        }
        #endregion
    }
}
