using Avalonia.Controls;
using Avalonia.Threading;
using darts_hub.control;
using darts_hub.control.wizard;
using darts_hub.model;
using darts_hub.ViewModels;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages application initialization process
    /// </summary>
    public class InitializationManager
    {
        private readonly MainWindow mainWindow;
        private readonly Configurator configurator;

        public InitializationManager(MainWindow mainWindow, Configurator configurator)
        {
            this.mainWindow = mainWindow;
            this.configurator = configurator;
        }

        public async Task InitializeApplication()
        {
            // Initialize About content with app version and settings
            InitializeAboutContent();

            await InitializeProfileManager();
            await InitializeUpdater();
            
            // Check if wizard should be shown
            await CheckAndShowWizard();
        }

        public void InitializeViewModel()
        {
            var viewModel = new UpdaterViewModel
            {
                IsBetaTester = configurator.Settings.IsBetaTester
            };
            mainWindow.DataContext = viewModel;
        }

        public void InitializeWindowSettings()
        {
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowResizeHelper.SetupProportionalResize(mainWindow, 1004.0 / 800.0, true, false);
        }

        private void InitializeAboutContent()
        {
            try
            {
                // Set the app version
                var aboutAppVersion = mainWindow.FindControl<ContentControl>("AboutAppVersion");
                if (aboutAppVersion != null)
                {
                    aboutAppVersion.Content = Updater.version;
                }
                
                // Set the skip update confirmation checkbox
                var aboutCheckBoxSkipUpdateConfirmation = mainWindow.FindControl<CheckBox>("AboutCheckBoxSkipUpdateConfirmation");
                if (aboutCheckBoxSkipUpdateConfirmation != null)
                {
                    aboutCheckBoxSkipUpdateConfirmation.IsChecked = configurator.Settings.SkipUpdateConfirmation;
                }
                
                // Set the new settings mode checkbox
                var aboutCheckBoxNewSettingsMode = mainWindow.FindControl<CheckBox>("AboutCheckBoxNewSettingsMode");
                if (aboutCheckBoxNewSettingsMode != null)
                {
                    aboutCheckBoxNewSettingsMode.IsChecked = configurator.Settings.NewSettingsMode;
                }
                
                // Set Robbel3D Configuration Button visibility
                var robbel3DConfigButton = mainWindow.FindControl<Button>("Robbel3DConfigButton");
                if (robbel3DConfigButton != null)
                {
                    robbel3DConfigButton.IsVisible = configurator.Settings.ShowRobbel3DSetup;
                }
                
                // Show the About content by default
                var contentModeManager = mainWindow.GetContentModeManager();
                contentModeManager.ShowAboutMode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing About content: {ex.Message}");
            }
        }

        private async Task InitializeProfileManager()
        {
            var profileManager = mainWindow.GetProfileManager();
            SetupProfileManagerEvents(profileManager);

            profileManager.LoadAppsAndProfiles();

            try
            {
                profileManager.CloseApps();
            }
            catch (Exception ex)
            {
                await mainWindow.RenderMessageBox("", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }

            mainWindow.RenderProfiles();
        }

        private async Task InitializeUpdater()
        {
            SetupUpdaterEvents();
            SetupRetryProgressEvents();
            Updater.CheckNewVersion();
            mainWindow.SetWait(true, "Checking for update...");
        }

        private void SetupProfileManagerEvents(ProfileManager profileManager)
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

        // Event Handlers
        private async void ProfileManager_AppDownloadStarted(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(true, "Downloading " + e.App.Name + "...");
        }

        private void ProfileManager_AppDownloadFinished(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(false, "");
            _ = mainWindow.RunSelectedProfile(true);
        }

        private void ProfileManager_AppDownloadFailed(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(false, "Download " + e.App.Name + " failed. Please check your internet connection and try again. " + e.Message);
        }

        private void ProfileManager_AppDownloadProgressed(object? sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            mainWindow.SetWait(true, "");
        }

        private void ProfileManager_AppInstallStarted(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(true, "Installing " + e.App.Name + "...");
        }

        private void ProfileManager_AppInstallFinished(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(false, "");
        }

        private void ProfileManager_AppInstallFailed(object? sender, AppEventArgs e)
        {
            mainWindow.SetWait(false, "Install " + e.App.Name + " failed. " + e.Message);
        }

        private async void ProfileManager_AppConfigurationRequired(object? sender, AppEventArgs e)
        {
            mainWindow.SelectedApp = e.App;
            var appSettingsRenderer = new AppSettingsRenderer(mainWindow, configurator);
            await appSettingsRenderer.RenderAppSettings(e.App);
        }

        private async void RetryHelper_ProgressChanged(object? sender, RetryProgressEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                mainWindow.SetWait(true, e.Message);
            });
        }

        private async void Updater_NoNewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                mainWindow.SetWait(false, "");
                
                // Check if wizard should be shown first
                if (!configurator.Settings.WizardCompleted && mainWindow.SelectedProfile != null)
                {
                    return;
                }
                
                // Only auto-start if no wizard is needed
                if (configurator.Settings.StartProfileOnStart) 
                {
                    _ = mainWindow.RunSelectedProfile();
                }
            });
        }

        private async void Updater_NewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (configurator.Settings.SkipUpdateConfirmation)
                {
                    try
                    {
                        Updater.UpdateToNewVersion();
                    }
                    catch (Exception ex)
                    {
                        await mainWindow.RenderMessageBox("", "Update to new version failed: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                    }
                    return;
                }

                var changelogMarkdown = NormalizeChangelogIndentation(e.Message);
                var dialog = new UpdateDialog();
                dialog.SetData(e.Version, changelogMarkdown);

                bool? update = await dialog.ShowDialog<bool?>(mainWindow);

                if (update == true)
                {
                    try
                    {
                        Updater.UpdateToNewVersion();
                    }
                    catch (Exception ex)
                    {
                        await mainWindow.RenderMessageBox("", "Update to new version failed: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                    }
                }
                else
                {
                    await HandleNoUpdateSelected();
                }
            });
        }

        private static string NormalizeChangelogIndentation(string changelog)
        {
            var lines = (changelog ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("    "))
                {
                    lines[i] = lines[i].Substring(4);
                }
            }

            return string.Join(Environment.NewLine, lines).Trim();
        }

        private async Task HandleNoUpdateSelected()
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                mainWindow.SetWait(false, "");
                
                // Check if wizard should be shown first
                if (!configurator.Settings.WizardCompleted && mainWindow.SelectedProfile != null)
                {
                    return;
                }
                
                // Only auto-start if no wizard is needed
                if (configurator.Settings.StartProfileOnStart) 
                {
                    _ = mainWindow.RunSelectedProfile();
                }
            });
        }

        private async void Updater_ReleaseDownloadStarted(object? sender, ReleaseEventArgs e)
        {
            mainWindow.SetWait(true, "Downloading " + e.Version + "...");
        }

        private async void Updater_ReleaseDownloadFailed(object? sender, ReleaseEventArgs e)
        {
            await mainWindow.RenderMessageBox("", "Check or update to new version failed: " + e.Message, MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.Ok, null, null, 5);
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                mainWindow.SetWait(false, "");
                if (configurator.Settings.StartProfileOnStart) 
                    _ = mainWindow.RunSelectedProfile();
            });
        }

        private void Updater_ReleaseDownloadProgressed(object? sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            mainWindow.SetWait(true, "");
        }

        private void Updater_ReleaseInstallInitialized(object? sender, ReleaseEventArgs e)
        {
            mainWindow.Close();
        }

        /// <summary>
        /// Checks if the setup wizard should be shown and displays it if needed
        /// </summary>
        private async Task CheckAndShowWizard()
        {
            // Show wizard if it hasn't been completed and we have profiles loaded
            if (!configurator.Settings.WizardCompleted && mainWindow.SelectedProfile != null)
            {
                // Check if there are any configurable apps in the profile
                bool hasConfigurableApps = mainWindow.SelectedProfile.Apps.Values.Any(appState => 
                    appState.App.IsConfigurable() || 
                    appState.App.CustomName.ToLower().Contains("caller"));

                if (hasConfigurableApps)
                {
                    // Show the wizard and prevent auto-minimizing
                    await mainWindow.ShowSetupWizard();
                    return; // Don't continue with normal startup flow
                }
            }
        }

        public void Dispose()
        {
            var profileManager = mainWindow.GetProfileManager();
            
            // Unsubscribe from events
            RetryHelper.RetryProgressChanged -= RetryHelper_ProgressChanged;
            
            if (profileManager != null)
            {
                profileManager.AppDownloadStarted -= ProfileManager_AppDownloadStarted;
                profileManager.AppDownloadFinished -= ProfileManager_AppDownloadFinished;
                profileManager.AppDownloadFailed -= ProfileManager_AppDownloadFailed;
                profileManager.AppDownloadProgressed -= ProfileManager_AppDownloadProgressed;
                profileManager.AppInstallStarted -= ProfileManager_AppInstallStarted;
                profileManager.AppInstallFinished -= ProfileManager_AppInstallFinished;
                profileManager.AppInstallFailed -= ProfileManager_AppInstallFailed;
                profileManager.AppConfigurationRequired -= ProfileManager_AppConfigurationRequired;
            }

            Updater.NewReleaseFound -= Updater_NewReleaseFound;
            Updater.NoNewReleaseFound -= Updater_NoNewReleaseFound;
            Updater.ReleaseInstallInitialized -= Updater_ReleaseInstallInitialized;
            Updater.ReleaseDownloadStarted -= Updater_ReleaseDownloadStarted;
            Updater.ReleaseDownloadFailed -= Updater_ReleaseDownloadFailed;
            Updater.ReleaseDownloadProgressed -= Updater_ReleaseDownloadProgressed;
        }
    }
}