using autodarts_desktop.control;
using Avalonia.Controls;
using System.Diagnostics;
using System;
using MessageBox.Avalonia;
using Avalonia;
using Avalonia.Interactivity;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace autodarts_desktop
{
    public partial class About : Window
    {
        private const string donationAdress = "bc1qr7wsvmmgaj6dle8gae2dl0dcxu5yh8vqlv34x4";




        public About()
        {
            InitializeComponent();
            appVersion.Content = Updater.version;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
                case "bug":
                    VisitHelpPage("https://github.com/lbormann/autodarts-desktop/issues");
                    break;
                case "donation":
                    Application.Current.Clipboard.SetTextAsync(donationAdress);
                    MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        Icon = MessageBox.Avalonia.Enums.Icon.Success,
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
                    }).ShowDialog(this);
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
                MessageBoxManager.GetMessageBoxStandardWindow("Error", "Error occured: " + ex.Message).Show();
            }
        }

    }
}
