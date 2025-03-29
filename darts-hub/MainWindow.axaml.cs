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
                    SaveBetaTesterStatus(value); // Speichern des Betatester-Status
                   // Updater.CheckNewVersion(); // Trigger the update check when the checkbox is toggled
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
        private System.Collections.ObjectModel.ObservableCollection<Control> selectedProfileElements;

        private double fontSize;
        private int elementWidth;
        private HorizontalAlignment elementHoAl;

        
        private Configurator configurator;
        


        // METHODS

        public MainWindow()
        {
            InitializeComponent();
            ConfigureTitleBar();
            configurator = new Configurator("config.json");
            var viewModel = new UpdaterViewModel
            {
                IsBetaTester = configurator.Settings.IsBetaTester // Laden des Betatester-Status
            };
            DataContext = viewModel;

            WindowHelper.CenterWindowOnScreen(this);

            fontSize = 18.0;
            elementWidth = (int)(Width * 0.80);
            elementHoAl = HorizontalAlignment.Left;

            Comboboxportal.Width = elementWidth;
            Comboboxportal.FontSize = fontSize;
            Comboboxportal.HorizontalAlignment = elementHoAl;

            SelectProfile.FontSize = fontSize - 4;
            selectedProfileElements = new(); 
            CheckBoxStartProfileOnProgramStart.FontSize = fontSize - 6;

            Opened += MainWindow_Opened;
        }
        private void ConfigureTitleBar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CustomTitleBar.IsVisible = true;
                ExtendClientAreaToDecorationsHint = true;
                ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            BeginMoveDrag(e);
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
                    RenderMessageBox("", "Error occured: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
                }

                RenderProfiles();

                Updater.NewReleaseFound += Updater_NewReleaseFound;
                Updater.NoNewReleaseFound += Updater_NoNewReleaseFound;
                Updater.ReleaseInstallInitialized += Updater_ReleaseInstallInitialized;
                Updater.ReleaseDownloadStarted += Updater_ReleaseDownloadStarted;
                Updater.ReleaseDownloadFailed += Updater_ReleaseDownloadFailed;
                Updater.ReleaseDownloadProgressed += Updater_ReleaseDownloadProgressed;
                Updater.CheckNewVersion();
                SetWait(true, "Checking for update ..");
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

        private void CheckForUpdates()
        {
            Updater.CheckNewVersion();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                profileManager.CloseApps();
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Error occured: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
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

        private void Comboboxportal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedProfile != null) selectedProfile.IsTaggedForStart = false;
            selectedProfile = ((ComboBoxItem)Comboboxportal.SelectedItem).Tag as Profile;
            if (selectedProfile == null) return;
            selectedProfile.IsTaggedForStart = true;
            RenderProfile();
            Save();
        }

        private void CheckBoxStartProfileOnProgramStartChanged(object sender, RoutedEventArgs e)
        {
            configurator.Settings.StartProfileOnStart = (bool)CheckBoxStartProfileOnProgramStart.IsChecked;
            configurator.SaveSettings();
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

                var result = await RenderMessageBox("", $"Configuration - file '{ex.File}' not readable ('{ex.Message}'). You can fix it by yourself or let it go to hell and I recreate it for you. Do you want me to reset it ? (All of your settings will be lost)", MsBox.Avalonia.Enums.Icon.Error, ButtonEnum.YesNo);
                if (result == ButtonResult.Yes)
                {
                    try
                    {
                        profileManager.DeleteConfigurationFile(ex.File);
                    }
                    catch (Exception e)
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Error", "Configuration-file-deletion failed. Please delete it by yourself. " + e.Message).ShowWindowAsync();
                    }
                }
                await RenderMessageBox("", "Application will close now. Please restart it.", MsBox.Avalonia.Enums.Icon.Warning);
                Environment.Exit(1);
            });
        }

        private async Task<ButtonResult> RenderMessageBox(string title = "",
                                                  string message = "",
                                                  Icon icon = MsBox.Avalonia.Enums.Icon.None,
                                                  ButtonEnum buttons = ButtonEnum.Ok,
                                                  double width = -1,
                                                  double height = -1,
                                                  int autoCloseDelayInSeconds = 0)
        {
            if (width < 0)
            {
                width = Width / 1.3;
            }
            if (height < 0)
            {
                height = Height / 1.3;
            }

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
                    // Füge hier weitere Fälle hinzu, falls nötig
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
                //((Window)msBoxWindow).Close(ButtonResult.No);
            }
            return result switch
            {
                "Ok" => ButtonResult.Ok,
                "Yes" => ButtonResult.Yes,
                "No" => ButtonResult.No,
                _ => ButtonResult.None
            };
        }




        private async void Updater_NoNewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SetWait(false);
                if (configurator.Settings.StartProfileOnStart) RunSelectedProfile(); 
            });
        }

        private async void Updater_NewReleaseFound(object? sender, ReleaseEventArgs e)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var update = ButtonResult.No;
                if (!configurator.Settings.SkipUpdateConfirmation)
                {
                    update = await RenderMessageBox($"Update available",
                                                    $"New Version '{e.Version}' available!\r\n\r\nDO YOU WANT TO UPDATE?\r\n\r\n\r\n------------------  CHANGELOG  ------------------\r\n\r\n{e.Message}", 
                                                    MsBox.Avalonia.Enums.Icon.Success, 
                                                    ButtonEnum.YesNo, 
                                                    520.0, 720.0);
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
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        SetWait(false);
                        if (configurator.Settings.StartProfileOnStart) RunSelectedProfile();
                    });
                }
            });
        }

        private void Updater_ReleaseDownloadStarted(object? sender, ReleaseEventArgs e)
        {
            SetWait(true, "Downloading " + e.Version + "..");
        }

        private async void Updater_ReleaseDownloadFailed(object? sender, ReleaseEventArgs e)
        {
            await RenderMessageBox("", "Check or update to new version failed: " + e.Message, MsBox.Avalonia.Enums.Icon.Error, autoCloseDelayInSeconds: 5);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
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



        private void ProfileManager_AppDownloadStarted(object? sender, AppEventArgs e)
        {
            SetWait(true, "Downloading " + e.App.Name + "..");
        }

        private void ProfileManager_AppDownloadFinished(object? sender, AppEventArgs e)
        {
            SetWait(false);
            RunSelectedProfile(true);
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

        private void Save()
        {
            try
            {
                profileManager.StoreApps();
            }
            catch (Exception ex)
            {
                RenderMessageBox("", "Error occured: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private async void RunSelectedProfile(bool minimize = true)
        {
            try
            {
                scroller.ScrollToHome();
                SetWait(true, "Starting profile ..");
                if (ProfileManager.RunProfile(selectedProfile) && minimize) WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                await RenderMessageBox("", "An error ocurred: " + ex.Message, MsBox.Avalonia.Enums.Icon.Error);
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
            Comboboxportal.SelectedItem = lastItemTaggedForStart != null ? lastItemTaggedForStart : cbiProfiles[0];

            RenderProfile();
        }

        private void RenderProfile()
        {
            if (selectedProfile == null) return;

            foreach (var e in selectedProfileElements) GridMain.Children.Remove(e);
            selectedProfileElements.Clear();

            var startMargin = Comboboxportal.Margin;
            var top = 30;
            var counter = 1;
            
            

            foreach (var app in selectedProfile.Apps.OrderBy(a => a.Value.App.CustomName)
                                                    .OrderByDescending(a => a.Value.TaggedForStart)
                                                    .OrderByDescending(a => a.Value.IsRequired)
                                                    )
            {
                var marginTop = counter * top + 10;
                selectedProfile.Apps.TryGetValue(app.Key, out ProfileState? appProfile);
                var nextMargin = new Thickness(startMargin.Left, startMargin.Top + marginTop, startMargin.Right, startMargin.Bottom);

                var imageRunState = new Image();
                imageRunState.ZIndex = 99;
                imageRunState.HorizontalAlignment = HorizontalAlignment.Left;
                imageRunState.Width = 30;
                imageRunState.Height = 30;
                imageRunState.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/exit.png")));
                imageRunState.DataContext = app.Value.App;
                imageRunState.Bind(Image.IsVisibleProperty, new Binding("AppRunningState"));

                var buttonRunState = new Button();
                buttonRunState.ZIndex = 99;
                buttonRunState.Margin = new Thickness(nextMargin.Left + 348, nextMargin.Top + 1, nextMargin.Right, nextMargin.Bottom);
                buttonRunState.Content = imageRunState;
                buttonRunState.HorizontalAlignment = HorizontalAlignment.Left;
                buttonRunState.VerticalAlignment = VerticalAlignment.Top;
                buttonRunState.VerticalContentAlignment = VerticalAlignment.Center;
                buttonRunState.FontSize = fontSize;
                buttonRunState.Background = Brushes.Transparent;
                buttonRunState.BorderThickness = new Thickness(0);
                buttonRunState.Click += async (s, e) =>
                {
                    app.Value.App.Close();
                };
                GridMain.Children.Add(buttonRunState);
                selectedProfileElements.Add(buttonRunState);


                var imageMonitor = new Image();
                imageMonitor.ZIndex = 99;
                imageMonitor.HorizontalAlignment = HorizontalAlignment.Left;
                imageMonitor.Width = 24;
                imageMonitor.Height = 24;
                imageMonitor.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/terminal.png")));
                imageMonitor.DataContext = app.Value.App;
                imageMonitor.Bind(Image.IsVisibleProperty, new Binding("AppMonitorAvailable"));

                var buttonMonitor= new Button();
                buttonMonitor.ZIndex = 99;
                buttonMonitor.Margin = new Thickness(nextMargin.Left + 315, nextMargin.Top + 5, nextMargin.Right, nextMargin.Bottom);
                buttonMonitor.Content = imageMonitor;
                buttonMonitor.HorizontalAlignment = HorizontalAlignment.Left;
                buttonMonitor.VerticalAlignment = VerticalAlignment.Top;
                buttonMonitor.VerticalContentAlignment = VerticalAlignment.Center;
                buttonMonitor.FontSize = fontSize;
                buttonMonitor.Background = Brushes.Transparent;
                buttonMonitor.BorderThickness = new Thickness(0);
                buttonMonitor.Click += async (s, e) =>
                {
                    await new MonitorWindow(app.Value.App).ShowDialog(this);
                };
                GridMain.Children.Add(buttonMonitor);
                selectedProfileElements.Add(buttonMonitor);
                
                
                var imageConfiguration = new Image();
                imageConfiguration.ZIndex = 99;
                imageConfiguration.HorizontalAlignment = HorizontalAlignment.Left;
                imageConfiguration.Width = 24;
                imageConfiguration.Height = 24;
                imageConfiguration.Source = new Bitmap(AssetLoader.Open(new Uri("avares://darts-hub/Assets/configuration.png")));

                var buttonConfiguration = new Button();
                buttonConfiguration.ZIndex = 99;
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
                    if (app.Value.App.IsConfigurationChanged()) app.Value.App.ReRun(app.Value.RuntimeArguments);
                    Save();
                    scroller.ScrollToHome();
                    WindowState = WindowState.Normal;

                };
                GridMain.Children.Add(buttonConfiguration);
                selectedProfileElements.Add(buttonConfiguration);

                var checkBoxTagger = new CheckBox();
                checkBoxTagger.Margin = new Thickness(nextMargin.Left + 39, nextMargin.Top + 6, nextMargin.Right, nextMargin.Bottom);
                checkBoxTagger.HorizontalAlignment = HorizontalAlignment.Left;
                checkBoxTagger.VerticalAlignment = VerticalAlignment.Top;
                checkBoxTagger.VerticalContentAlignment = VerticalAlignment.Center;
                checkBoxTagger.FontSize = fontSize;
                checkBoxTagger.Content = appProfile.App.CustomName;
                checkBoxTagger.DataContext = appProfile;
                checkBoxTagger.Bind(CheckBox.IsCheckedProperty, new Binding("TaggedForStart"));
                //checkBoxTagger.IsEnabled = !appProfile.IsRequired;
                checkBoxTagger.Foreground = appProfile.TaggedForStart ? Brushes.White : Brushes.Gray;
                checkBoxTagger.FontWeight = appProfile.TaggedForStart ? FontWeight.Bold : FontWeight.Normal;
                checkBoxTagger.Checked += (s, e) =>
                {
                    checkBoxTagger.Foreground = Brushes.White;
                    checkBoxTagger.FontWeight = FontWeight.Bold;
                    appProfile.TaggedForStart = true;
                    Save();
                };
                checkBoxTagger.Unchecked += (s, e) =>
                {
                    if (!appProfile.IsRequired)
                    {
                        checkBoxTagger.Foreground = Brushes.Gray;
                        checkBoxTagger.FontWeight = FontWeight.Normal;
                        appProfile.TaggedForStart = false;
                        app.Value.App.Close();
                    }
                    else
                    {
                        checkBoxTagger.IsChecked = true;
                        appProfile.TaggedForStart = false;
                    }
                    Save();
                };
                checkBoxTagger.PointerReleased += (s, e) =>
                {
                    var renameTextBox = (checkBoxTagger.Tag as TextBox);
                    GridMain.Children.Add(renameTextBox);
                    //renameTextBox.SelectAll();
                    renameTextBox.CaretIndex = renameTextBox.Text.Length;
                    renameTextBox.Focus();
                    checkBoxTagger.IsVisible = false;
                };

                var textBox = new TextBox();
                textBox.Tag = checkBoxTagger;
                textBox.Margin = new Thickness(nextMargin.Left + 35, nextMargin.Top + 4, nextMargin.Right, nextMargin.Bottom);
                textBox.HorizontalAlignment = elementHoAl;
                textBox.Foreground = Brushes.AliceBlue;
                textBox.VerticalAlignment = VerticalAlignment.Top;
                textBox.FontSize = fontSize;
                textBox.Width = elementWidth - 34;
                textBox.MaxLength = 33;
                textBox.TextAlignment = TextAlignment.Center;
                textBox.ZIndex = 9999;
                textBox.KeyDown += (s, e) =>
                {
                    var parent = (textBox.Tag as CheckBox);
                    if (e.Key == Key.Enter) {
                        GridMain.Children.Remove(s as Control);
                        if(textBox.Text == String.Empty)
                        {
                            textBox.Text = appProfile.App.Name;
                        }   
                        appProfile.App.CustomName = textBox.Text;
                        parent.Content = textBox.Text;
                        parent.IsVisible = true;
                        Save();
                    }
                    else if (e.Key == Key.Escape)
                    {
                        GridMain.Children.Remove(s as Control);
                        parent.IsVisible = true;
                    }
                };
                textBox.LostFocus += (s, e) =>
                {
                    var parent = (textBox.Tag as CheckBox);
                    GridMain.Children.Remove(s as Control);
                    parent.IsVisible = true;
                };
                textBox.Text = appProfile.App.CustomName;
                checkBoxTagger.Tag = textBox;


                if (!String.IsNullOrEmpty(appProfile.App.DescriptionShort))
                {
                    var tt = new ToolTip();
                    tt.Content = appProfile.App.DescriptionShort;
                    tt.FontSize = fontSize + 2.0;
                    tt.FontWeight = FontWeight.Bold;
                    tt.FontStyle = FontStyle.Italic;
                    tt.Foreground = Brushes.GhostWhite;
                    tt.BorderBrush = Brushes.White;
                    tt.Background = Brushes.RoyalBlue;
                    ToolTip.SetPlacement(checkBoxTagger, PlacementMode.Pointer);
                    ToolTip.SetTip(checkBoxTagger, tt);
                    ToolTip.SetShowDelay(checkBoxTagger, 750);
                }

                GridMain.Children.Add(checkBoxTagger);
                selectedProfileElements.Add(checkBoxTagger);
                selectedProfileElements.Add(textBox);

                counter += 1;
            }


        }



    }
    
}
