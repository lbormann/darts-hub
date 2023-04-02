using autodarts_desktop.control;
using autodarts_desktop.model;
using autodarts_desktop.Properties;
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
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Threading.Tasks;


namespace autodarts_desktop
{
    public partial class MainWindow : Window
    {

        // ATTRIBUTES

        private ProfileManager profileManager;
        private Profile? selectedProfile;
        private List<Control> selectedProfileElements;

        private double fontSize;
        private int elementWidth;
        private HorizontalAlignment elementHoAl;



        // METHODES

        public MainWindow()
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);

            fontSize = 18.0;
            elementWidth = (int)(Width * 0.80);
            elementHoAl = HorizontalAlignment.Left;

            Comboboxportal.Width = elementWidth;
            Comboboxportal.FontSize = fontSize;
            Comboboxportal.HorizontalAlignment = elementHoAl;

            SelectProfile.FontSize = fontSize - 4;

            selectedProfileElements = new();
            CheckBoxStartProfileOnProgramStart.IsChecked = Settings.Default.start_profile_on_start;
            CheckBoxStartProfileOnProgramStart.FontSize = fontSize - 6;

            try
            {
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

                RenderProfiles();

                Updater.NewReleaseFound += Updater_NewReleaseFound;
                Updater.ReleaseInstallInitialized += Updater_ReleaseInstallInitialized;
                Updater.ReleaseDownloadStarted += Updater_ReleaseDownloadStarted;
                Updater.ReleaseDownloadFailed += Updater_ReleaseDownloadFailed;
                Updater.ReleaseDownloadProgressed += Updater_ReleaseDownloadProgressed;
                Updater.CheckNewVersion();
            }
            catch (ConfigurationException ex)
            {
                ShowCorruptedConfigHandlingBox(ex);
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Something went wrong: " + ex.Message, MessageBox.Avalonia.Enums.Icon.Error);
                Environment.Exit(1);
            }
        }

      


        private void Buttonstart_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedProfile();
        }

        private async void Buttonabout_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            await new About().ShowDialog(this);
            WindowState = WindowState.Normal;
        }

        private void Comboboxportal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedProfile != null) selectedProfile.IsTaggedForStart = false;
            selectedProfile = ((ComboBoxItem)Comboboxportal.SelectedItem).Tag as Profile;
            if (selectedProfile == null) return;
            selectedProfile.IsTaggedForStart = true;
            RenderProfile();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.start_profile_on_start = (bool)CheckBoxStartProfileOnProgramStart.IsChecked;
            Settings.Default.Save();

            try
            {
                profileManager.StoreApps();
                profileManager.CloseApps();
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Error occured: " + ex.Message, MessageBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void WaitingText_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            WaitingText.IsVisible = false;
        }




        private async void ShowCorruptedConfigHandlingBox(ConfigurationException ex)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                IsEnabled = false;
                Opacity = 0.25;

                var result = await RenderMessageBox("", $"Configuration - file '{ex.File}' not readable ('{ex.Message}'). You can fix it by yourself or let it go to hell and I recreate it for you. Do you want me to reset it ? (All of your settings will be lost)", MessageBox.Avalonia.Enums.Icon.Error, ButtonEnum.YesNo);
                if (result == ButtonResult.Yes)
                {
                    try
                    {
                        profileManager.DeleteConfigurationFile(ex.File);
                    }
                    catch (Exception e)
                    {
                        await MessageBoxManager.GetMessageBoxStandardWindow("Error", "Configuration-file-deletion failed. Please delete it by yourself. " + e.Message).Show();
                    }
                }
                await RenderMessageBox("", "Application will close now. Please restart it.", MessageBox.Avalonia.Enums.Icon.Warning);
                Environment.Exit(1);
            });
        }


        private Task<ButtonResult> RenderMessageBox(string title = "", 
                                                    string message = "",
                                                    Icon icon = MessageBox.Avalonia.Enums.Icon.None, 
                                                    ButtonEnum buttons = ButtonEnum.Ok,
                                                    double width = -1,
                                                    double height = -1)
        {
            if (width < 0)
            {
                width = Width / 1.3;
            }
            if (height < 0)
            {
                height = Height / 1.3;
            }
           
            return MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            {
                Icon = icon,
                WindowIcon = Icon,
                Width = width,
                Height = height,
                MaxWidth = width,
                MaxHeight = height,
                CanResize = false,
                EscDefaultButton = ClickEnum.No,
                EnterDefaultButton = ClickEnum.Yes,
                SystemDecorations = SystemDecorations.Full,
                WindowStartupLocation = WindowStartupLocation,
                ButtonDefinitions = buttons,
                ContentTitle = title,
                ContentMessage = message
            }).Show(this);
        }


        private async void Updater_NewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var result = await RenderMessageBox("", $"New Version '{e.Version}' available! Do you want to update?", MessageBox.Avalonia.Enums.Icon.Warning, ButtonEnum.YesNo);

                if (result == ButtonResult.Yes)
                {
                    try
                    {
                        Updater.UpdateToNewVersion();
                    }
                    catch (Exception ex)
                    {
                        await RenderMessageBox("", "Update to new version failed: " + ex.Message, MessageBox.Avalonia.Enums.Icon.Error);
                    }
                }
            });
        }

        private void Updater_ReleaseDownloadStarted(object? sender, ReleaseEventArgs e)
        {
            SetWait(true, "Downloading " + e.Version + "..");
        }

        private async void Updater_ReleaseDownloadFailed(object? sender, ReleaseEventArgs e)
        {
            await RenderMessageBox("", "Checking for new release failed! Please check your internet-connection and try again. " + e.Message, MessageBox.Avalonia.Enums.Icon.Error);
            SetWait(false);
        }

        private void Updater_ReleaseDownloadProgressed(object? sender, DownloadProgressChangedEventArgs e)
        {
            SetWait(true);
        }

        private void Updater_ReleaseInstallInitialized(object? sender, ReleaseEventArgs e)
        {
            Close();
        }



        private void ProfileManager_AppDownloadStarted(object? sender, AppEventArgs e)
        {
            SetWait(true, "Downloading " + e.App.Name + "..");
        }

        private void ProfileManager_AppDownloadFinished(object? sender, AppEventArgs e)
        {
            SetWait(false);
        }

        private void ProfileManager_AppDownloadFailed(object? sender, AppEventArgs e)
        {
            SetWait(false, "Download " + e.App.Name + " failed. Please check your internet-connection and try again. " + e.Message);
        }

        private void ProfileManager_AppDownloadProgressed(object? sender, DownloadProgressChangedEventArgs e)
        {
            SetWait(true);
        }

        private void ProfileManager_AppInstallStarted(object? sender, AppEventArgs e)
        {
            SetWait(true, "Installing " + e.App.Name + "..");
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
            WindowState = WindowState.Minimized;
            await new SettingsWindow(profileManager, e.App).ShowDialog(this);
            scroller.ScrollToHome();
            WindowState = WindowState.Normal;
        }


        private async void RunSelectedProfile()
        {
            try
            {
                scroller.ScrollToHome();
                SetWait(true, "Starting profile ..");
                if (ProfileManager.RunProfile(selectedProfile)) WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                await RenderMessageBox("", "An error ocurred: " + ex.Message, MessageBox.Avalonia.Enums.Icon.Error);
            }
            finally
            {
                SetWait(false);
            }
        }

        private void SetWait(bool wait, string waitingText = "")
        {
            string waitingMessage = String.IsNullOrEmpty(waitingText) ? WaitingText.Text : waitingText;
            if (wait)
            {
                GridMain.IsEnabled = false;
                foreach(var c in GridMain.Children)
                {
                    if(c.Name != "WaitingText") c.IsVisible = false;
                }
                WaitingText.IsVisible = true;
            }
            else
            {
                GridMain.IsEnabled = true;
                foreach (var c in GridMain.Children)
                {
                    c.IsVisible = true;
                }
                WaitingText.IsVisible = false;
            }
            WaitingText.Text = waitingMessage;
        }

        private async void RenderProfiles()
        {
            ComboBoxItem? lastItemTaggedForStart = null;
            var profiles = profileManager.GetProfiles();
            if (profiles.Count == 0)
            {
                await RenderMessageBox("", "No profiles available.", MessageBox.Avalonia.Enums.Icon.Warning);
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
            Comboboxportal.Items = cbiProfiles;
            Comboboxportal.SelectedItem = lastItemTaggedForStart != null ? lastItemTaggedForStart : cbiProfiles[0];


            RenderProfile();

            if (Settings.Default.start_profile_on_start) RunSelectedProfile();
        }

        private void RenderProfile()
        {
            if (selectedProfile == null) return;

            foreach (var e in selectedProfileElements) GridMain.Children.Remove(e);
            selectedProfileElements.Clear();

            var startMargin = Comboboxportal.Margin;
            var top = 30;
            var counter = 1;
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            

            foreach (var app in selectedProfile.Apps.OrderByDescending(a => a.Value.TaggedForStart).OrderByDescending(a => a.Value.IsRequired))
            {
                var marginTop = counter * top + 10;
                selectedProfile.Apps.TryGetValue(app.Key, out ProfileState? appProfile);
                var nextMargin = new Thickness(startMargin.Left, startMargin.Top + marginTop, startMargin.Right, startMargin.Bottom);

                
                var imageConsole = new Image();
                imageConsole.HorizontalAlignment = HorizontalAlignment.Left;
                imageConsole.Width = 24;
                imageConsole.Height = 24;
                imageConsole.Source = new Bitmap(assets.Open(new Uri("avares://autodarts-desktop/Assets/terminal.png")));

                var buttonConsole= new Button();
                buttonConsole.Margin = new Thickness(nextMargin.Left + 300, nextMargin.Top + 5, nextMargin.Right, nextMargin.Bottom);
                buttonConsole.Content = imageConsole;
                buttonConsole.HorizontalAlignment = HorizontalAlignment.Left;
                buttonConsole.VerticalAlignment = VerticalAlignment.Top;
                buttonConsole.VerticalContentAlignment = VerticalAlignment.Center;
                buttonConsole.FontSize = fontSize;
                buttonConsole.Background = Brushes.Transparent;
                buttonConsole.BorderThickness = new Thickness(0);
                buttonConsole.DataContext = app.Value.App;
                // appProfile.App.IsRunning()
                // buttonConsole.Bind(Button.IsEnabledProperty, new Binding("IsRunning()"));
                // buttonConsole.IsEnabled = true;

                buttonConsole.Click += async (s, e) =>
                {
                    await RenderMessageBox($"Console for {app.Value.App.Name}", 
                        app.Value.App.AppConsoleStdOutput + app.Value.App.AppConsoleStdError,
                        MessageBox.Avalonia.Enums.Icon.None,
                        ButtonEnum.Ok,
                        Width,
                        Height
                        );
                };
                GridMain.Children.Add(buttonConsole);
                selectedProfileElements.Add(buttonConsole);
                
                
                var imageConfiguration = new Image();
                imageConfiguration.HorizontalAlignment = HorizontalAlignment.Left;
                imageConfiguration.Width = 24;
                imageConfiguration.Height = 24;
                imageConfiguration.Source = new Bitmap(assets.Open(new Uri("avares://autodarts-desktop/Assets/configuration.png")));

                var buttonConfiguration = new Button();
                buttonConfiguration.Margin = new Thickness(nextMargin.Left, nextMargin.Top + 5, nextMargin.Right, nextMargin.Bottom);
                buttonConfiguration.Content = imageConfiguration;
                buttonConfiguration.HorizontalAlignment = HorizontalAlignment.Left;
                buttonConfiguration.VerticalAlignment = VerticalAlignment.Top;
                buttonConfiguration.VerticalContentAlignment = VerticalAlignment.Center;
                buttonConfiguration.FontSize = fontSize;
                buttonConfiguration.Background = Brushes.Transparent;
                buttonConfiguration.BorderThickness = new Thickness(0);
                buttonConfiguration.IsEnabled = appProfile.App.IsConfigurable() || appProfile.App.IsInstallable();

                buttonConfiguration.Click += async (s, e) =>
                {
                    WindowState = WindowState.Minimized;
                    await new SettingsWindow(profileManager, app.Value.App).ShowDialog(this);
                    scroller.ScrollToHome();
                    WindowState = WindowState.Normal;
                };
                GridMain.Children.Add(buttonConfiguration);
                selectedProfileElements.Add(buttonConfiguration);

                var checkBoxTagger = new CheckBox();
                checkBoxTagger.Margin = new Thickness(nextMargin.Left + 39, nextMargin.Top + 6, nextMargin.Right, nextMargin.Bottom);
                checkBoxTagger.Content = appProfile.App.Name;
                checkBoxTagger.HorizontalAlignment = HorizontalAlignment.Left;
                checkBoxTagger.VerticalAlignment = VerticalAlignment.Top;
                checkBoxTagger.VerticalContentAlignment = VerticalAlignment.Center;
                checkBoxTagger.DataContext = appProfile;
                checkBoxTagger.FontSize = fontSize;
                checkBoxTagger.Bind(CheckBox.IsCheckedProperty, new Binding("TaggedForStart"));
                checkBoxTagger.IsEnabled = !appProfile.IsRequired;
                checkBoxTagger.Foreground = appProfile.TaggedForStart ? Brushes.White : Brushes.Gray;
                checkBoxTagger.FontWeight = appProfile.TaggedForStart ? FontWeight.Bold : FontWeight.Normal;
                checkBoxTagger.Checked += (s, e) =>
                {
                    checkBoxTagger.Foreground = Brushes.White;
                    checkBoxTagger.FontWeight = FontWeight.Bold;
                };
                checkBoxTagger.Unchecked += (s, e) =>
                {
                    checkBoxTagger.Foreground = Brushes.Gray;
                    checkBoxTagger.FontWeight = FontWeight.Normal;
                };
                    
                // TODO
                //if (!String.IsNullOrEmpty(appProfile.App.DescriptionShort))
                //{
                //    var tt = new ToolTip();
                //    tt.Content = appProfile.App.DescriptionShort;
                //    tt.DataContext = checkBoxTagger;
                //}
                
                GridMain.Children.Add(checkBoxTagger);
                selectedProfileElements.Add(checkBoxTagger);

                counter += 1;
            }


        }



    }
}
