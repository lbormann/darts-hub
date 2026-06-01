using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.control;
using darts_hub.model;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Large modal alert window for Critical / Warning notifications.
    /// Plays an OS warning sound on open.
    /// </summary>
    public class NotificationAlertWindow : Window
    {
        private readonly Notification notification;
        private readonly NotificationManager? manager;

        public NotificationAlertWindow(Notification notification, NotificationManager? manager)
        {
            ArgumentNullException.ThrowIfNull(notification);
            this.notification = notification;
            this.manager = manager;

            Title = $"{SeverityLabel(notification.Severity)}: {notification.Title}";
            Width = 720;
            Height = 520;
            MinWidth = 500;
            MinHeight = 360;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = true;
            ShowInTaskbar = true;
            SystemDecorations = SystemDecorations.Full;
            Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24));

            BuildContent();

            Opened += (_, _) => PlayAlertSound(notification.Severity);
        }

        private void BuildContent()
        {
            var (accent, icon, label) = SeverityVisual(notification.Severity);

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            // Header band
            var headerBg = new SolidColorBrush(Color.FromArgb(0x35, accent.R, accent.G, accent.B));
            var header = new Border
            {
                Background = headerBg,
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(24, 18)
            };
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
            headerStack.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 48,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center
            });
            var headerText = new StackPanel { Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
            headerText.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(accent),
                LetterSpacing = 2
            });
            headerText.Children.Add(new TextBlock
            {
                Text = notification.Title,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });
            headerStack.Children.Add(headerText);
            header.Child = headerStack;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Body
            var scroll = new ScrollViewer
            {
                Padding = new Thickness(24, 16, 24, 16),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Control bodyContent;
            if (!string.IsNullOrWhiteSpace(notification.BodyMarkdown))
            {
                bodyContent = MarkdownRenderer.Render(notification.BodyMarkdown!, baseFontSize: 14);
            }
            else
            {
                bodyContent = new SelectableTextBlock
                {
                    Text = HtmlToPlainText(notification.BodyHtml ?? string.Empty),
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 230)),
                    TextWrapping = TextWrapping.Wrap
                };
            }
            scroll.Content = bodyContent;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Footer (actions)
            var footer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1C)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x46)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 12)
            };
            var footerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            var openPanelBtn = new Button
            {
                Content = "Open notifications",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 185)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 86)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 6),
                CornerRadius = new CornerRadius(4),
                FontSize = 13,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            openPanelBtn.Click += (_, _) =>
            {
                if (Owner is MainWindow mw) mw.OpenNotificationPanelExternally();
                Close();
            };
            footerPanel.Children.Add(openPanelBtn);

            if (notification.RequiresAck && notification.State.AcknowledgedAt == null)
            {
                var ackBtn = ActionButton("Acknowledge", Color.FromRgb(40, 167, 69));
                ackBtn.Click += async (_, _) =>
                {
                    if (manager != null) await manager.AcknowledgeAsync(notification.Id);
                    Close();
                };
                footerPanel.Children.Add(ackBtn);
            }

            var dismissBtn = ActionButton("Dismiss", accent);
            dismissBtn.Click += async (_, _) =>
            {
                if (manager != null) await manager.MarkReadAsync(notification.Id);
                Close();
            };
            footerPanel.Children.Add(dismissBtn);

            footer.Child = footerPanel;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);

            Content = root;
        }

        private static Button ActionButton(string text, Color background) => new()
        {
            Content = text,
            Background = new SolidColorBrush(background),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(18, 7),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        private static (Color accent, string icon, string label) SeverityVisual(NotificationSeverity sev) => sev switch
        {
            NotificationSeverity.Critical => (Color.FromRgb(230, 80, 80), "⛔", "CRITICAL"),
            NotificationSeverity.Warning => (Color.FromRgb(230, 175, 60), "⚠", "WARNING"),
            NotificationSeverity.Success => (Color.FromRgb(40, 200, 125), "✅", "SUCCESS"),
            NotificationSeverity.Announcement => (Color.FromRgb(170, 130, 230), "📢", "ANNOUNCEMENT"),
            _ => (Color.FromRgb(100, 160, 230), "ℹ", "INFO")
        };

        private static string SeverityLabel(NotificationSeverity sev) => sev switch
        {
            NotificationSeverity.Critical => "Critical",
            NotificationSeverity.Warning => "Warning",
            _ => sev.ToString()
        };

        private static readonly Regex ScriptRegex = new("<script.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StyleRegex = new("<style.*?</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex BreakRegex = new("<(br|/p|/div|/li|/h[1-6])\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TagRegex = new("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex WsRegex = new("[ \\t]+", RegexOptions.Compiled);

        private static string HtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            var s = ScriptRegex.Replace(html, string.Empty);
            s = StyleRegex.Replace(s, string.Empty);
            s = BreakRegex.Replace(s, "\n");
            s = TagRegex.Replace(s, string.Empty);
            s = System.Net.WebUtility.HtmlDecode(s);
            s = WsRegex.Replace(s, " ");
            return s.Trim();
        }

        /// <summary>
        /// Plays an OS-appropriate alert sound. Best-effort, swallows failures.
        /// </summary>
        private static void PlayAlertSound(NotificationSeverity severity)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // MB_ICONHAND = 0x10 (critical), MB_ICONEXCLAMATION = 0x30 (warning)
                    uint type = severity == NotificationSeverity.Critical ? 0x10u : 0x30u;
                    MessageBeep(type);
                    return;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var sound = severity == NotificationSeverity.Critical ? "Sosumi" : "Funk";
                    Process.Start(new ProcessStartInfo("afplay", $"/System/Library/Sounds/{sound}.aiff") { UseShellExecute = false, CreateNoWindow = true });
                    return;
                }

                // Linux / other – try paplay then aplay then terminal bell as fallback
                var candidates = new[]
                {
                    ("paplay", "/usr/share/sounds/freedesktop/stereo/dialog-warning.oga"),
                    ("aplay",  "/usr/share/sounds/alsa/Front_Center.wav")
                };
                foreach (var (cmd, file) in candidates)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(cmd, file) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
                        return;
                    }
                    catch { /* try next */ }
                }
                Console.Beep();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationAlertWindow] PlayAlertSound failed: {ex.Message}");
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MessageBeep(uint uType);
    }
}
