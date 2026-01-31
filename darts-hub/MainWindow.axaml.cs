using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Media;
using darts_hub.control;
using darts_hub.control.wizard;
using darts_hub.model;
using darts_hub.UI;
using darts_hub.ViewModels;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub
{
    public partial class MainWindow : Window
    {
        #region Constants and Fields
        private const string ConfigPath = "config.json";

        // Core managers
        private readonly ConsoleManager consoleManager;
        private readonly NavigationManager navigationManager;
        private readonly ContentModeManager contentModeManager;
        private ButtonEventManager buttonEventManager;
        private InitializationManager initializationManager;
        
        // Core components
        private ProfileManager profileManager;
        private Profile? selectedProfile;
        private AppBase? selectedApp;
        private Configurator configurator;
        #endregion

        #region Properties
        public Profile? SelectedProfile => selectedProfile;
        public AppBase? SelectedApp 
        { 
            get => selectedApp; 
            set => selectedApp = value; 
        }
        #endregion

        #region Constructor and Initialization
        public MainWindow()
        {
            InitializeComponent();
            
            configurator = new Configurator("config.json");
            
            if (configurator.RequiresRestart)
            {
                RestartApplication();
                return;
            }
            
            // Initialize managers
            consoleManager = new ConsoleManager();
            navigationManager = new NavigationManager();
            contentModeManager = new ContentModeManager(configurator);
            
            InitializeComponents();
            SetupEventHandlers();
        }

        private void InitializeComponents()
        {
            // Initialize profile manager first
            profileManager = new ProfileManager();
            
            initializationManager = new InitializationManager(this, configurator);
            
            initializationManager.InitializeViewModel();
            initializationManager.InitializeWindowSettings();
            InitializeManagers();
            
            // Initialize button event manager
            buttonEventManager = new ButtonEventManager(
                this,
                consoleManager,
                contentModeManager,
                configurator,
                profileManager,
                (title, message, icon, buttons, width, height, autoCloseDelay) => RenderMessageBox(title, message, icon, buttons, width, height, autoCloseDelay),
                LoadChangelogContent,
                SetWait,
                RunSelectedProfile,
                Save,
                ShowSetupWizard
            );
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
            SettingsScrollViewer.ScrollChanged += (s, e) => UpdateToTopButtonVisibility(s as ScrollViewer);
            NewSettingsScrollViewer.ScrollChanged += (s, e) => UpdateToTopButtonVisibility(s as ScrollViewer);
        }
        #endregion

        #region Public Methods for Manager Access
        public ContentModeManager GetContentModeManager() => contentModeManager;
        public Configurator GetConfigurator() => configurator;
        public ProfileManager GetProfileManager() => profileManager;

        public void SetWait(bool wait, string waitingText = "")
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

        public void Save()
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

        public async Task<ButtonResult> RenderMessageBox(string title, string message, MsBox.Avalonia.Enums.Icon icon, ButtonEnum buttons = ButtonEnum.Ok, double? width = null, double? height = null, int autoCloseDelayInSeconds = 0, bool isMarkdown = false)
        {
            return await MessageBoxHelper.ShowMessageBox(this, title, message, icon, buttons, width, height, autoCloseDelayInSeconds, isMarkdown);
        }

        public async Task<bool> RunSelectedProfile(bool minimize = true)
        {
            try
            {
                SetWait(true, "Starting profile...");
                if (ProfileManager.RunProfile(selectedProfile) && minimize) 
                    WindowState = WindowState.Minimized;
                return true;
            }
            catch (Exception ex)
            {
                await RenderMessageBox("", "An error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                return false;
            }
            finally
            {
                SetWait(false);
            }
        }

        public async Task ShowSetupWizard()
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
                        var appSettingsRenderer = new AppSettingsRenderer(this, configurator);
                        await appSettingsRenderer.RenderAppSettings(selectedApp);
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

        #region Window Event Handlers
        private async void MainWindow_Opened(object sender, EventArgs e)
        {
            try
            {
                await initializationManager.InitializeApplication();
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                initializationManager?.Dispose();
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

        #region Button Event Handlers (delegated to ButtonEventManager)
        private void Buttonstart_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleStartClick(sender, e);
        private async void Buttonabout_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleAboutClick(sender, e);
        private void ButtonConsole_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleClick(sender, e);
        private async void ButtonChangelog_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleChangelogClick(sender, e);
        private void CheckBoxStartProfileOnProgramStartChanged(object sender, RoutedEventArgs e) => buttonEventManager.HandleStartProfileOnProgramStartChanged(sender, e);
        private async void AboutButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleAboutButtonClick(sender, e);
        private void AboutCheckBoxSkipUpdateConfirmationChanged(object sender, RoutedEventArgs e) => buttonEventManager.HandleSkipUpdateConfirmationChanged(sender, e);
        private void AboutCheckBoxNewSettingsModeChanged(object sender, RoutedEventArgs e) => buttonEventManager.HandleNewSettingsModeChanged(sender, e);
        private void NewSettingsBackButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleNewSettingsBackButton(sender, e);
        private void ToTopButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleToTopButton(sender, e);
        private async void SetupWizardButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleSetupWizardButton(sender, e);
        private void ConsoleClearButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleClearButton(sender, e);
        private void ConsoleClearCurrentButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleClearCurrentButton(sender, e);
        private async void ConsoleExportButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleExportButton(sender, e);
        private async void ConsoleTestUpdaterButton_Click(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleTestUpdaterButton(sender, e);
        private void ConsoleAutoScrollCheckBox_Changed(object sender, RoutedEventArgs e) => buttonEventManager.HandleConsoleAutoScrollCheckBox(sender, e);
        private async void Robbel3DConfigButton_Click(object sender, RoutedEventArgs e) => await ShowRobbel3DConfiguration();
        #endregion

        #region Profile and App Management
        public async void RenderProfiles()
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
            var appSettingsRenderer = new AppSettingsRenderer(this, configurator);
            await appSettingsRenderer.RenderAppSettings(app);
        }

        private void RefreshCurrentAppSettings()
        {
            if (selectedApp != null && contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Settings)
            {
                _ = Task.Run(async () =>
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var appSettingsRenderer = new AppSettingsRenderer(this, configurator);
                        await appSettingsRenderer.RenderAppSettings(selectedApp);
                    });
                });
            }
        }
        #endregion

        #region Utility Methods
        private async Task LoadChangelogContent()
        {
            try
            {
                var changelogText = await Helper.AsyncHttpGet("https://raw.githubusercontent.com/lbormann/darts-hub/main/CHANGELOG.md", 4);
                if (string.IsNullOrEmpty(changelogText))
                    changelogText = "Changelog not available. Please try again later.";
       
                var host = this.FindControl<ContentControl>("ChangelogHost");
                if (host != null)
                {
                    host.Content = BuildSimpleMarkdownView(changelogText);
                }
            }
            catch (Exception ex)
            {
                var host = this.FindControl<ContentControl>("ChangelogHost");
                if (host != null)
                {
                    host.Content = BuildSimpleMarkdownView($"Failed to load changelog: {ex.Message}");
                }
            }
        }

        private Control BuildSimpleMarkdownView(string markdown)
        {
            var stack = new StackPanel { Spacing = 8, Margin = new Thickness(12) };
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var buffer = new List<string>();

            void FlushParagraph()
            {
                if (buffer.Count == 0) return;
                var text = string.Join(" ", buffer).Trim();
                if (text.Length == 0)
                {
                    buffer.Clear();
                    return;
                }

                stack.Children.Add(new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
                buffer.Clear();
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock { Text = "", Height = 4 });
                    continue;
                }

                if (line.StartsWith("### "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[4..].Trim(),
                        FontSize = 16,
                        FontWeight = Avalonia.Media.FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[3..].Trim(),
                        FontSize = 18,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("# "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[2..].Trim(),
                        FontSize = 22,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    FlushParagraph();
                    var bullet = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                    bullet.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 14,
                        FontWeight = Avalonia.Media.FontWeight.Bold
                    });
                    bullet.Children.Add(new TextBlock
                    {
                        Text = line[2..].Trim(),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14
                    });
                    stack.Children.Add(bullet);
                    continue;
                }

                buffer.Add(line.Trim());
            }

            FlushParagraph();

            if (stack.Children.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = markdown,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
            }

            return stack;
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

        private async Task ShowRobbel3DConfiguration()
        {
            try
            {
                var robbel3DWindow = new Robbel3DConfigWindow(profileManager);
                var result = await robbel3DWindow.ShowDialog<bool?>(this);
                
                if (result == true)
                {
                    // Configuration was applied successfully
                    // Refresh the UI to reflect any updated settings
                    navigationManager.RenderAppNavigation();
                    
                    // Re-render current app settings if any app is selected
                    if (selectedApp != null)
                    {
                        var appSettingsRenderer = new AppSettingsRenderer(this, configurator);
                        await appSettingsRenderer.RenderAppSettings(selectedApp);
                    }
                    
                    await RenderMessageBox("Robbel3D Configuration", 
                        "Your WLED device and darts applications have been configured successfully with Robbel3D presets!", 
                        MsBox.Avalonia.Enums.Icon.Success);
                }
            }
            catch (Exception ex)
            {
                await RenderMessageBox("Robbel3D Configuration Error", 
                    $"An error occurred while opening the Robbel3D configuration:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void RestartApplication()
        {
            try
            {
                var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(currentExe))
                {
                    var args = Environment.GetCommandLineArgs()
                        .Skip(1)
                        .Select(a => a.Contains(' ') ? $"\"{a}\"" : a);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = currentExe,
                        Arguments = string.Join(" ", args),
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Failed to restart application: {ex.Message}");
            }

            Environment.Exit(0);
        }
    }
}
