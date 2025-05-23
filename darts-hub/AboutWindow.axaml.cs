using darts_hub.control;
using Avalonia.Controls;
using System.Diagnostics;
using System;
using MsBox.Avalonia;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using Avalonia.Input.Platform;
using Avalonia.Input;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace darts_hub
{
    public partial class AboutWindow : Window
    {
        // ATTRIBUTES
        private const string donationAdress = "bc1qr7wsvmmgaj6dle8gae2dl0dcxu5yh8vqlv34x4";
        private Configurator configurator;
        private string changelogText;

        // METHODES
        public AboutWindow()
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);
            ConfigureTitleBarAbout();
        }
        private void ConfigureTitleBarAbout()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CustomTitleBarAbout.IsVisible = true;
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
        public AboutWindow(Configurator configurator)
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);
            this.configurator = configurator;
            appVersion.Content = Updater.version;

            Opened += AboutWindow_Opened;
        }

        private async void AboutWindow_Opened(object sender, EventArgs e)
        {
            ConfigureTitleBarAbout();
            CheckBoxSkipUpdateConfirmation.IsChecked = configurator.Settings.SkipUpdateConfirmation;
        }

        private void CheckBoxSkipUpdateConfirmationChanged(object sender, RoutedEventArgs e)
        {
            configurator.Settings.SkipUpdateConfirmation = (bool)CheckBoxSkipUpdateConfirmation.IsChecked;
            configurator.SaveSettings();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Button helpButton = sender as Button;

            switch (helpButton.Name)
            {
                case "contact1":
                    VisitHelpPage("https://discordapp.com/users/Reepa86#1149");
                    break;
                case "contact2":
                    VisitHelpPage("https://discordapp.com/users/wusaaa#0578");
                    break;
                case "contact3":
                    VisitHelpPage("https://discordapp.com/users/366537096414101504");
                    break;
                case "paypal":
                    VisitHelpPage("https://paypal.me/I3ull3t");
                    break;
                case "bug":
                    VisitHelpPage("https://github.com/lbormann/darts-hub/issues");
                    break;
                case "donation":
                    var clipboard = this.Clipboard;
                    await clipboard.SetTextAsync(donationAdress);
                    MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        Icon = MsBox.Avalonia.Enums.Icon.Success,
                        ContentTitle = "Donation",
                        WindowIcon = Icon,
                        Width = Width / 1.3,
                        Height = Height / 1.3,
                        MaxWidth = MaxWidth / 1.3,
                        MaxHeight = MaxHeight / 1.3,
                        CanResize = false,
                        EscDefaultButton = ClickEnum.No,
                        EnterDefaultButton = ClickEnum.Yes,
                        SystemDecorations = SystemDecorations.Full,
                        WindowStartupLocation = WindowStartupLocation,
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = $"{donationAdress} copied to clipboard - Thank you!"
                    }).ShowWindowAsync();
                    break;
                    
                case "changelog":
                    changelogText = await Helper.AsyncHttpGet(Updater.appSourceUrlChangelog, 4);
                    if (String.IsNullOrEmpty(changelogText)) changelogText = "Changelog not available. Please try again later.";
           
                    double width = Width + Width / 2;
                    double height = Height * 2;
                    MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        Icon = MsBox.Avalonia.Enums.Icon.None,
                        ContentTitle = "Changelog",
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
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentMessage = changelogText
                    }).ShowWindowAsync();

                    break;
            }
        }

        private void VisitHelpPage(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandard("Error", "Error occured: " + ex.Message).ShowAsync();
            }
        }

    }
}
