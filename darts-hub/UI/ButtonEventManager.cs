using Avalonia.Controls;
using Avalonia.Interactivity;
using darts_hub.control;
using darts_hub.control.wizard;
using darts_hub.model;
using MsBox.Avalonia.Enums;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages all button event handlers for the MainWindow
    /// </summary>
    public class ButtonEventManager
    {
        private readonly MainWindow mainWindow;
        private readonly ConsoleManager consoleManager;
        private readonly ContentModeManager contentModeManager;
        private readonly Configurator configurator;
        private readonly ProfileManager profileManager;
        private readonly Func<string, string, MsBox.Avalonia.Enums.Icon, ButtonEnum, double?, double?, int, Task<ButtonResult>> renderMessageBox;
        private readonly Func<Task> loadChangelogContent;
        private readonly Action<bool, string> setWait;
        private readonly Func<bool, Task<bool>> runSelectedProfile;
        private readonly Action save;
        private readonly Func<Task> showSetupWizard;

        public ButtonEventManager(
            MainWindow mainWindow,
            ConsoleManager consoleManager, 
            ContentModeManager contentModeManager,
            Configurator configurator,
            ProfileManager profileManager,
            Func<string, string, MsBox.Avalonia.Enums.Icon, ButtonEnum, double?, double?, int, Task<ButtonResult>> renderMessageBox,
            Func<Task> loadChangelogContent,
            Action<bool, string> setWait,
            Func<bool, Task<bool>> runSelectedProfile,
            Action save,
            Func<Task> showSetupWizard)
        {
            this.mainWindow = mainWindow;
            this.consoleManager = consoleManager;
            this.contentModeManager = contentModeManager;
            this.configurator = configurator;
            this.profileManager = profileManager;
            this.renderMessageBox = renderMessageBox;
            this.loadChangelogContent = loadChangelogContent;
            this.setWait = setWait;
            this.runSelectedProfile = runSelectedProfile;
            this.save = save;
            this.showSetupWizard = showSetupWizard;
        }

        public void HandleStartClick(object sender, RoutedEventArgs e)
        {
            _ = runSelectedProfile(true);
        }

        public async void HandleAboutClick(object sender, RoutedEventArgs e)
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

        public void HandleConsoleClick(object sender, RoutedEventArgs e)
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
                consoleManager.SetSelectedProfile(mainWindow.SelectedProfile);
            }
        }

        public async void HandleChangelogClick(object sender, RoutedEventArgs e)
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
                await loadChangelogContent();
            }
        }

        public void HandleStartProfileOnProgramStartChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.IsChecked.HasValue == true)
            {
                configurator.Settings.StartProfileOnStart = checkBox.IsChecked.Value;
                configurator.SaveSettings();
            }
        }

        public async void HandleAboutButtonClick(object sender, RoutedEventArgs e)
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
                case "AboutDiscord":
                    VisitHelpPage("https://discord.gg/xt5GHJ5Z3j");
                    break;
                case "AboutBug":
                    VisitHelpPage("https://github.com/lbormann/darts-hub/issues");
                    break;
                case "AboutChangelog":
                    contentModeManager.ShowChangelogMode();
                    consoleManager.Stop();
                    await loadChangelogContent();
                    break;
            }
        }

        public void HandleSkipUpdateConfirmationChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                configurator.Settings.SkipUpdateConfirmation = checkBox.IsChecked == true;
                configurator.SaveSettings();
            }
        }

        public void HandleNewSettingsModeChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                configurator.Settings.NewSettingsMode = checkBox.IsChecked == true;
                configurator.SaveSettings();
            }
        }

        public void HandleNewSettingsBackButton(object sender, RoutedEventArgs e)
        {
            // Switch back to classic mode
            configurator.Settings.NewSettingsMode = false;
            configurator.SaveSettings();
            
            // Update the checkbox in About section
            var aboutCheckBox = mainWindow.FindControl<CheckBox>("AboutCheckBoxNewSettingsMode");
            if (aboutCheckBox != null)
            {
                aboutCheckBox.IsChecked = false;
            }
            
            // Re-render current app settings if any app is selected
            if (mainWindow.SelectedApp != null)
            {
                _ = Task.Run(async () =>
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var appSettingsRenderer = new AppSettingsRenderer(mainWindow, configurator);
                        await appSettingsRenderer.RenderAppSettings(mainWindow.SelectedApp);
                    });
                });
            }
            else
            {
                // Just switch modes
                contentModeManager.ShowClassicSettingsMode();
            }
        }

        public void HandleToTopButton(object sender, RoutedEventArgs e)
        {
            if (contentModeManager.CurrentContentMode == ContentModeManager.ContentMode.Settings)
            {
                ScrollViewer scrollViewer;
                if (configurator.Settings.NewSettingsMode)
                {
                    scrollViewer = mainWindow.FindControl<ScrollViewer>("NewSettingsScrollViewer");
                }
                else
                {
                    scrollViewer = mainWindow.FindControl<ScrollViewer>("SettingsScrollViewer");
                }
                
                if (scrollViewer == null) return;
                
                AnimateScrollToTop(scrollViewer);
            }
        }

        public async void HandleSetupWizardButton(object sender, RoutedEventArgs e)
        {
            await showSetupWizard();
        }

        public void HandleConsoleClearButton(object sender, RoutedEventArgs e)
        {
            consoleManager.ClearAllConsoles();
        }

        public void HandleConsoleClearCurrentButton(object sender, RoutedEventArgs e)
        {
            consoleManager.ClearCurrentConsole();
        }

        public async void HandleConsoleExportButton(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = consoleManager.ExportCurrentConsole();
                if (filePath != null)
                {
                    await renderMessageBox("Export Success", 
                        $"Console log exported successfully to:\n{filePath}", 
                        MsBox.Avalonia.Enums.Icon.Success, ButtonEnum.Ok, null, null, 0);
                }
                else
                {
                    await renderMessageBox("Export Error", "No console tab selected or profile active.", 
                        MsBox.Avalonia.Enums.Icon.Warning, ButtonEnum.Ok, null, null, 0);
                }
            }
            catch (Exception ex)
            {
                await renderMessageBox("Export Error", 
                    $"Failed to export console log:\n{ex.Message}", 
                    MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.Ok, null, null, 0);
            }
        }

        public async void HandleConsoleTestUpdaterButton(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdaterLogger.LogInfo("Opening updater test window from console");
                
                var testWindow = new UpdaterTestWindow()
                {
                    WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterOwner
                };
                
                await testWindow.ShowDialog(mainWindow);
                
                UpdaterLogger.LogInfo("Updater test window closed successfully");
            }
            catch (System.NotSupportedException ex) when (ex.Message.Contains("Markdown.Avalonia"))
            {
                UpdaterLogger.LogError("Markdown binding issue detected, running quick test instead", ex);
                
                setWait(true, "Running quick updater test...");
                
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
                    
                    setWait(false, "");
                    
                    await renderMessageBox("Updater Quick Test", combinedResults, 
                        MsBox.Avalonia.Enums.Icon.Info, ButtonEnum.Ok, 500, 400, 0);
                }
                catch (Exception quickTestEx)
                {
                    setWait(false, "");
                    UpdaterLogger.LogError("Quick test also failed", quickTestEx);
                    
                    await renderMessageBox("Test Interface Error", 
                        "The test interface could not be opened and the quick test also failed.\n\n" +
                        "Please use command line tests:\n\n" +
                        "• dotnet run -- --full (Full Test)\n" +
                        "• dotnet run -- --version (Version Check)\n" +
                        "• dotnet run -- --retry (Retry Test)",
                        MsBox.Avalonia.Enums.Icon.Warning, ButtonEnum.Ok, null, null, 0);
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Failed to open updater test window", ex);
                
                setWait(true, "Running fallback quick test...");
                
                try
                {
                    var quickResults = await UpdaterTestRunner.RunQuickVersionTest();
                    
                    setWait(false, "");
                    
                    await renderMessageBox("Updater Quick Test (Fallback)", 
                        "The test interface could not be opened.\n" +
                        "Here are the results of a quick test:\n\n" +
                        quickResults + "\n" +
                        "For more detailed tests use command line:\n" +
                        "• dotnet run -- --full\n" +
                        "• dotnet run -- --version\n" +
                        "• dotnet run -- --retry", 
                        MsBox.Avalonia.Enums.Icon.Info, ButtonEnum.Ok, null, null, 0);
                }
                catch (Exception fallbackEx)
                {
                    setWait(false, "");
                    
                    await renderMessageBox("Test Error", 
                        $"Error opening test interface:\n{ex.Message}\n\n" +
                        $"Fallback test also failed:\n{fallbackEx.Message}\n\n" +
                        "Please use command line tests:\n" +
                        "• dotnet run -- --full\n" +
                        "• dotnet run -- --version\n" +
                        "• dotnet run -- --retry", 
                        MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.Ok, null, null, 0);
                }
            }
        }

        public void HandleConsoleAutoScrollCheckBox(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            consoleManager.IsAutoScrollEnabled = checkBox?.IsChecked == true;
        }

        private async Task HandleBitcoinDonation()
        {
            var clipboard = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow?.Clipboard
                : null;
                
            if (clipboard != null)
            {
                const string donationAddress = "bc1qr7wsvmmgaj6dle8gae2dl0dcxu5yh8vqlv34x4";
                await clipboard.SetTextAsync(donationAddress);
                await renderMessageBox("Bitcoin donation address copied", 
                    $"Address copied to clipboard:\n{donationAddress}", 
                    MsBox.Avalonia.Enums.Icon.Info, ButtonEnum.Ok, null, null, 0);
            }
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
                _ = renderMessageBox("Error", "Error occurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.Ok, null, null, 0);
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
                
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
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
    }
}