using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace darts_hub.UI
{
    /// <summary>
    /// A polished "stay tuned" dialog for extensions that are visible in the UI
    /// but not yet available for configuration or download.
    /// </summary>
    public partial class ComingSoonDialog : Window
    {
        private readonly string? helpUrl;

        public ComingSoonDialog()
            : this(title: "Extension", iconText: "🟦", teaser: null, helpUrl: null)
        {
        }

        public ComingSoonDialog(string title, string iconText, string? teaser, string? helpUrl)
        {
            InitializeComponent();

            this.helpUrl = string.IsNullOrWhiteSpace(helpUrl) ? null : helpUrl;

            var titleBlock = this.FindControl<TextBlock>("TitleText");
            if (titleBlock != null && !string.IsNullOrWhiteSpace(title))
            {
                titleBlock.Text = title;
            }

            var iconBlock = this.FindControl<TextBlock>("IconText");
            if (iconBlock != null && !string.IsNullOrWhiteSpace(iconText))
            {
                iconBlock.Text = iconText;
            }

            var teaserBlock = this.FindControl<TextBlock>("TeaserText");
            if (teaserBlock != null && !string.IsNullOrWhiteSpace(teaser))
            {
                teaserBlock.Text = teaser;
            }

            var helpUrlText = this.FindControl<TextBlock>("HelpUrlText");
            var helpUrlPanel = this.FindControl<Border>("HelpUrlPanel");
            var openHelpButton = this.FindControl<Button>("OpenHelpButton");

            if (this.helpUrl != null)
            {
                if (helpUrlText != null) helpUrlText.Text = this.helpUrl;
                if (helpUrlPanel != null) helpUrlPanel.IsVisible = true;
                if (openHelpButton != null) openHelpButton.IsVisible = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void GotItButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenHelpButton_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(helpUrl)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = helpUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ComingSoonDialog] Failed to open help URL '{helpUrl}': {ex.Message}");
            }
        }
    }
}
