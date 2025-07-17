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
        private const string donationAddress = "bc1qr7wsvmmgaj6dle8gae2dl0dcxu5yh8vqlv34x4";
        private Configurator configurator;
        private string changelogText;

        // METHODS
        public AboutWindow()
        {
            InitializeComponent();
            WindowHelper.CenterWindowOnScreen(this);
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
                    VisitHelpPage("https://www.paypal.com/paypalme/wusaaa");
                    break;
                case "donation":
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(donationAddress);
                        await MessageBoxManager.GetMessageBoxStandard("Bitcoin donation address copied", 
                            $"Address copied to clipboard:\n{donationAddress}").ShowWindowAsync();
                    }
                    break;
                case "bug":
                    VisitHelpPage("https://github.com/lbormann/darts-hub/issues");
                    break;
                case "changelog":
                    if (string.IsNullOrEmpty(changelogText))
                    {
                        changelogText = await Helper.AsyncHttpGet("https://raw.githubusercontent.com/lbormann/darts-hub/main/CHANGELOG.md", 4);
                        if (string.IsNullOrEmpty(changelogText)) 
                            changelogText = "Changelog not available. Please try again later.";
                    }

                    await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        Icon = MsBox.Avalonia.Enums.Icon.None,
                        ContentTitle = "Changelog",
                        WindowIcon = Icon,
                        Width = 600,
                        Height = 500,
                        CanResize = true,
                        SystemDecorations = SystemDecorations.Full,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
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
                MessageBoxManager.GetMessageBoxStandard("Error", "Error occurred: " + ex.Message).ShowWindowAsync();
            }
        }
    }
}
