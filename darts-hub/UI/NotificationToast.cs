using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using darts_hub.model;
using System;

namespace darts_hub.UI
{
    /// <summary>
    /// Small top-right overlay that previews a freshly arrived notification.
    /// Auto-closes after <see cref="LifetimeSeconds"/> with a progress bar countdown,
    /// can be dismissed manually, or clicked to open the full panel.
    /// </summary>
    public class NotificationToast : Border
    {
        public const double LifetimeSeconds = 10.0;
        private const int TickIntervalMs = 50;

        private readonly DispatcherTimer timer;
        private readonly Border progressBar;
        private readonly Border progressTrack;
        private DateTime startedAt;
        private bool closed;

        public event EventHandler? Clicked;
        public event EventHandler? Dismissed;

        public Notification Notification { get; }

        public NotificationToast(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            Notification = notification;

            Width = 360;
            Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x22, 0x22, 0x28));
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x52));
            BorderThickness = new Thickness(1);
            CornerRadius = new CornerRadius(10);
            BoxShadow = BoxShadows.Parse("0 6 24 0 #99000000");
            Cursor = new Cursor(StandardCursorType.Hand);

            var (accent, icon) = SeverityVisual(notification);

            var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };

            var top = new Grid
            {
                Margin = new Thickness(12, 10, 8, 10),
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            // Accent bar on the left
            var accentBar = new Border
            {
                Width = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(accent),
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(accentBar, 0);
            top.Children.Add(accentBar);

            var content = new StackPanel { Spacing = 2 };
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            titleRow.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 14,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleRow.Children.Add(new TextBlock
            {
                Text = notification.Title,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(titleRow);

            content.Children.Add(new TextBlock
            {
                Text = ExtractPreview(notification),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 195)),
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 3,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            Grid.SetColumn(content, 1);
            top.Children.Add(content);

            // Close button
            var closeBtn = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 165)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(closeBtn, "Dismiss");
            closeBtn.Click += (_, e) =>
            {
                e.Handled = true;
                Close(true);
            };
            Grid.SetColumn(closeBtn, 2);
            top.Children.Add(closeBtn);

            Grid.SetRow(top, 0);
            root.Children.Add(top);

            // Progress bar at the bottom
            progressTrack = new Border
            {
                Height = 3,
                Background = new SolidColorBrush(Color.FromArgb(0x60, 0xFF, 0xFF, 0xFF)),
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                ClipToBounds = true
            };
            progressBar = new Border
            {
                Height = 3,
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            var progressGrid = new Grid();
            progressGrid.Children.Add(progressTrack);
            progressGrid.Children.Add(progressBar);
            Grid.SetRow(progressGrid, 1);
            root.Children.Add(progressGrid);

            Child = root;

            // Click anywhere on the toast (except the close button) opens the panel
            PointerPressed += (_, _) =>
            {
                Clicked?.Invoke(this, EventArgs.Empty);
                Close(false);
            };

            // Pause the timer while hovered so the user has time to read
            PointerEntered += (_, _) => timer?.Stop();
            PointerExited += (_, _) =>
            {
                if (closed) return;
                // Reset the start so the remaining time restarts from the current ratio
                var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
                startedAt = DateTime.UtcNow - TimeSpan.FromSeconds(elapsed);
                timer?.Start();
            };

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TickIntervalMs) };
            timer.Tick += OnTick;
        }

        public void Start()
        {
            startedAt = DateTime.UtcNow;
            // Fade-in
            Opacity = 0;
            Transitions = new Transitions
            {
                new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(180) }
            };
            Dispatcher.UIThread.Post(() => Opacity = 1, DispatcherPriority.Background);

            timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
            var ratio = Math.Min(1.0, elapsed / LifetimeSeconds);
            var trackWidth = progressTrack.Bounds.Width;
            if (trackWidth > 0)
                progressBar.Width = trackWidth * ratio;

            if (ratio >= 1.0)
                Close(true);
        }

        public void Close(bool dismissed)
        {
            if (closed) return;
            closed = true;
            timer.Stop();
            Opacity = 0;
            // Remove from parent after fade-out
            DispatcherTimer.RunOnce(() =>
            {
                if (Parent is Panel p) p.Children.Remove(this);
                if (dismissed) Dismissed?.Invoke(this, EventArgs.Empty);
            }, TimeSpan.FromMilliseconds(200));
        }

        private static (Color accent, string icon) SeverityVisual(Notification n) => n.Severity switch
        {
            NotificationSeverity.Success => (Color.FromRgb(40, 200, 125), "✅"),
            NotificationSeverity.Warning => (Color.FromRgb(230, 175, 60), "⚠"),
            NotificationSeverity.Critical => (Color.FromRgb(230, 80, 80), "⛔"),
            NotificationSeverity.Announcement => (Color.FromRgb(170, 130, 230), "📢"),
            _ => (Color.FromRgb(100, 160, 230), "ℹ")
        };

        private static string ExtractPreview(Notification n)
        {
            string text;
            if (!string.IsNullOrWhiteSpace(n.BodyHtml))
            {
                var raw = n.BodyHtml;
                text = System.Text.RegularExpressions.Regex.Replace(raw, "<.*?>", " ");
                text = System.Net.WebUtility.HtmlDecode(text);
            }
            else if (!string.IsNullOrWhiteSpace(n.BodyMarkdown))
            {
                text = MarkdownRenderer.ToPlainText(n.BodyMarkdown!);
            }
            else
            {
                text = string.Empty;
            }

            text = System.Text.RegularExpressions.Regex.Replace(text, "\\s+", " ").Trim();
            return text.Length <= 180 ? text : text[..180] + "…";
        }
    }
}
