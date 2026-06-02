using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading;

namespace darts_hub.UI
{
    /// <summary>
    /// Modal dialog shown when darts-caller asks the user to approve
    /// a new Autodarts device link. Closes itself five seconds after
    /// a successful connection is reported.
    /// </summary>
    public partial class CallerAuthDialog : Window
    {
        private const int AutoCloseSeconds = 5;

        private string? directUrl;
        private string? code;
        private string? webCallerUrl;
        private DispatcherTimer? countdownTimer;
        private int remainingSeconds;

        public CallerAuthDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void SetPrompt(string code, string directUrl, string? webCallerUrl)
        {
            this.code = code;
            this.directUrl = directUrl;
            this.webCallerUrl = webCallerUrl;

            if (this.FindControl<TextBlock>("CodeText") is { } codeBlock)
                codeBlock.Text = string.IsNullOrEmpty(code) ? "----" : code;

            if (this.FindControl<TextBlock>("DirectUrlText") is { } urlBlock)
                urlBlock.Text = directUrl;

            if (!string.IsNullOrEmpty(webCallerUrl))
            {
                if (this.FindControl<Border>("WebCallerBox") is { } box)
                    box.IsVisible = true;
                if (this.FindControl<TextBlock>("WebCallerUrlText") is { } webBlock)
                    webBlock.Text = webCallerUrl;
            }
        }

        public void NotifySuccess(string? userInfo)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (this.FindControl<Border>("SuccessBox") is { } box)
                    box.IsVisible = true;
                if (this.FindControl<TextBlock>("SuccessInfoText") is { } info)
                    info.Text = string.IsNullOrWhiteSpace(userInfo) ? "Authentication completed." : userInfo;

                StartAutoCloseCountdown();
            });
        }

        private void StartAutoCloseCountdown()
        {
            if (countdownTimer != null) return;

            remainingSeconds = AutoCloseSeconds;
            UpdateCountdownText();

            countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            countdownTimer.Tick += (_, _) =>
            {
                remainingSeconds--;
                if (remainingSeconds <= 0)
                {
                    countdownTimer?.Stop();
                    try { Close(); } catch { /* ignore */ }
                }
                else
                {
                    UpdateCountdownText();
                }
            };
            countdownTimer.Start();
        }

        private void UpdateCountdownText()
        {
            if (this.FindControl<TextBlock>("SuccessCountdownText") is { } cd)
                cd.Text = $"This window closes automatically in {remainingSeconds} second{(remainingSeconds == 1 ? string.Empty : "s")}.";
        }

        private void OpenDirectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(directUrl)) TryOpenUrl(directUrl!);
        }

        private async void CopyDirectButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(directUrl)) await CopyToClipboard(directUrl!);
        }

        private async void CopyCodeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(code)) await CopyToClipboard(code!);
        }

        private async void CopyWebCallerButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(webCallerUrl)) await CopyToClipboard(webCallerUrl!);
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            try { Close(); } catch { /* ignore */ }
        }

        protected override void OnClosed(EventArgs e)
        {
            countdownTimer?.Stop();
            countdownTimer = null;
            base.OnClosed(e);
        }

        private async System.Threading.Tasks.Task CopyToClipboard(string text)
        {
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallerAuthDialog] Clipboard copy failed: {ex.Message}");
            }
        }

        private static void TryOpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CallerAuthDialog] Failed to open url '{url}': {ex.Message}");
            }
        }
    }
}
