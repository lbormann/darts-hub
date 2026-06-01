using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using darts_hub.control;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Notifications inbox – list and detail (with polls) views.
    /// </summary>
    public partial class NotificationsPanel : UserControl
    {
        private NotificationManager? manager;
        private Notification? selected;

        public event EventHandler? CloseRequested;

        public NotificationsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Initialize(NotificationManager manager)
        {
            ArgumentNullException.ThrowIfNull(manager);
            this.manager = manager;
            this.manager.Changed += OnNotificationsChanged;
            Render();
        }

        public void Detach()
        {
            if (manager != null)
                manager.Changed -= OnNotificationsChanged;
        }

        private void OnNotificationsChanged(object? sender, NotificationsChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(Render);
        }

        private void Render()
        {
            if (manager == null) return;
            var items = manager.Notifications;

            var unread = manager.UnreadCount;
            var headerBadge = this.FindControl<Border>("HeaderUnreadBadge");
            var headerText = this.FindControl<TextBlock>("HeaderUnreadText");
            if (headerBadge != null && headerText != null)
            {
                headerBadge.IsVisible = unread > 0;
                headerText.Text = unread > 99 ? "99+" : unread.ToString();
            }

            var footer = this.FindControl<TextBlock>("FooterStatus");
            if (footer != null)
                footer.Text = items.Count == 0
                    ? "No notifications"
                    : $"{items.Count} notification{(items.Count == 1 ? "" : "s")} • {unread} unread";

            if (selected != null && items.All(n => n.Id != selected.Id))
                selected = null;

            if (selected != null)
                RenderDetail(selected);
            else
                RenderList(items);
        }

        private void RenderList(IReadOnlyList<Notification> items)
        {
            var listScroll = this.FindControl<ScrollViewer>("ListScrollViewer");
            var detailScroll = this.FindControl<ScrollViewer>("DetailScrollViewer");
            var listPanel = this.FindControl<StackPanel>("ListPanel");
            var emptyState = this.FindControl<StackPanel>("EmptyState");
            if (listPanel == null || listScroll == null || detailScroll == null) return;

            detailScroll.IsVisible = false;
            listScroll.IsVisible = true;

            listPanel.Children.Clear();
            if (items.Count == 0)
            {
                if (emptyState != null) emptyState.IsVisible = true;
                return;
            }
            if (emptyState != null) emptyState.IsVisible = false;

            foreach (var n in items)
                listPanel.Children.Add(BuildListItem(n));
        }

        private Control BuildListItem(Notification n)
        {
            var (accent, icon) = SeverityVisual(n.Severity);

            var titleText = new TextBlock
            {
                Text = n.Title,
                FontSize = 14,
                FontWeight = n.IsUnread ? FontWeight.Bold : FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            var preview = new TextBlock
            {
                Text = ExtractPreview(n),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 175)),
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            if (n.IsPinned)
                meta.Children.Add(BadgePill("📌 Pinned", Color.FromRgb(200, 150, 60)));
            if (n.RequiresAck && n.State.AcknowledgedAt == null)
                meta.Children.Add(BadgePill("Acknowledge required", Color.FromRgb(220, 80, 80)));
            if (n.Polls.Count > 0)
                meta.Children.Add(BadgePill("Poll", Color.FromRgb(80, 130, 220)));

            var content = new StackPanel { Spacing = 2 };
            content.Children.Add(titleText);
            content.Children.Add(preview);
            if (meta.Children.Count > 0) content.Children.Add(meta);

            var unreadDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 6, 0, 0),
                IsVisible = n.IsUnread
            };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 18,
                Foreground = new SolidColorBrush(accent),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 0, 0)
            };

            var grid = new Grid { Margin = new Thickness(12, 10, 12, 10), ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };
            grid.Children.Add(unreadDot); Grid.SetColumn(unreadDot, 0);
            grid.Children.Add(iconText); Grid.SetColumn(iconText, 1); iconText.Margin = new Thickness(10, 2, 10, 0);
            grid.Children.Add(content); Grid.SetColumn(content, 2);

            var dismiss = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 2),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Top
            };
            ToolTip.SetTip(dismiss, "Dismiss");
            dismiss.Click += async (_, _) =>
            {
                if (manager != null) await manager.DismissAsync(n.Id);
            };
            grid.Children.Add(dismiss); Grid.SetColumn(dismiss, 3);

            var border = new Border
            {
                Background = n.IsUnread
                    ? new SolidColorBrush(Color.FromRgb(38, 38, 46))
                    : new SolidColorBrush(Color.FromRgb(32, 32, 36)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 64)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid,
                Cursor = new Cursor(StandardCursorType.Hand)
            };

            border.PointerPressed += async (_, _) => await OpenDetailAsync(n);
            return border;
        }

        private async Task OpenDetailAsync(Notification n)
        {
            selected = n;
            if (manager != null && n.State.ReadAt == null)
                _ = manager.MarkReadAsync(n.Id);
            RenderDetail(n);
            await Task.CompletedTask;
        }

        private void RenderDetail(Notification n)
        {
            var listScroll = this.FindControl<ScrollViewer>("ListScrollViewer");
            var detailScroll = this.FindControl<ScrollViewer>("DetailScrollViewer");
            var detailPanel = this.FindControl<StackPanel>("DetailPanel");
            var emptyState = this.FindControl<StackPanel>("EmptyState");
            if (detailPanel == null || listScroll == null || detailScroll == null) return;

            listScroll.IsVisible = false;
            if (emptyState != null) emptyState.IsVisible = false;
            detailScroll.IsVisible = true;
            detailPanel.Children.Clear();

            // Back button
            var back = new Button
            {
                Content = "← Back to all",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(140, 175, 230)),
                FontSize = 12,
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            back.Click += (_, _) => { selected = null; Render(); };
            detailPanel.Children.Add(back);

            var (accent, icon) = SeverityVisual(n.Severity);

            // Title + severity
            var head = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            head.Children.Add(new TextBlock { Text = icon, FontSize = 22, Foreground = new SolidColorBrush(accent), VerticalAlignment = VerticalAlignment.Center });
            head.Children.Add(new TextBlock
            {
                Text = n.Title,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            detailPanel.Children.Add(head);

            if (!string.IsNullOrWhiteSpace(n.PublishAt))
            {
                detailPanel.Children.Add(new TextBlock
                {
                    Text = n.PublishAt,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140))
                });
            }

            // Body – render Markdown if available, otherwise HTML→plain
            Control? bodyBlock = null;
            if (!string.IsNullOrWhiteSpace(n.BodyMarkdown))
            {
                bodyBlock = MarkdownRenderer.Render(n.BodyMarkdown!, baseFontSize: 13);
            }
            else
            {
                var body = HtmlToPlainText(n.BodyHtml ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    bodyBlock = new SelectableTextBlock
                    {
                        Text = body,
                        FontSize = 13,
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 225)),
                        TextWrapping = TextWrapping.Wrap
                    };
                }
            }

            if (bodyBlock != null)
            {
                detailPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12),
                    Child = bodyBlock
                });
            }

            // Links
            if (n.Links.Count > 0)
            {
                detailPanel.Children.Add(SectionHeader("Links"));
                var linksPanel = new StackPanel { Spacing = 4 };
                foreach (var l in n.Links)
                {
                    var btn = LinkButton(string.IsNullOrWhiteSpace(l.Label) ? l.Url : l.Label, l.Url);
                    linksPanel.Children.Add(btn);
                }
                detailPanel.Children.Add(linksPanel);
            }

            // Attachments
            if (n.Attachments.Count > 0)
            {
                detailPanel.Children.Add(SectionHeader("Attachments"));
                var attPanel = new StackPanel { Spacing = 4 };
                foreach (var a in n.Attachments)
                {
                    var label = $"{a.Name} ({FormatBytes(a.SizeBytes)})";
                    var url = manager?.ResolveUrl(a.Url) ?? a.Url;
                    attPanel.Children.Add(LinkButton(label, url));
                }
                detailPanel.Children.Add(attPanel);
            }

            // Polls
            foreach (var p in n.Polls)
                detailPanel.Children.Add(BuildPollControl(n, p));

            // Acknowledge / Dismiss buttons
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            if (n.RequiresAck && n.State.AcknowledgedAt == null)
            {
                var ack = ActionButton("Acknowledge", Color.FromRgb(40, 167, 69));
                ack.Click += async (_, _) =>
                {
                    if (manager != null) await manager.AcknowledgeAsync(n.Id);
                };
                actions.Children.Add(ack);
            }
            var dismiss = ActionButton("Dismiss", Color.FromRgb(120, 120, 120));
            dismiss.Click += async (_, _) =>
            {
                if (manager == null) return;
                await manager.DismissAsync(n.Id);
                selected = null;
                Render();
            };
            actions.Children.Add(dismiss);
            detailPanel.Children.Add(actions);
        }

        private Control BuildPollControl(Notification n, NotificationPoll poll)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(36, 36, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var stack = new StackPanel { Spacing = 8 };
            container.Child = stack;

            stack.Children.Add(new TextBlock
            {
                Text = poll.Question,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            var closed = !string.IsNullOrWhiteSpace(poll.ClosesAt)
                && DateTime.TryParse(poll.ClosesAt, out var closeAt)
                && closeAt < DateTime.UtcNow;
            var alreadyVoted = poll.Voted;

            if (alreadyVoted || closed)
            {
                RenderPollResults(stack, poll, closed);
                return container;
            }

            var selectedIds = new HashSet<long>();
            var optionControls = new List<Control>();
            foreach (var opt in poll.Options)
            {
                Control input;
                if (poll.IsMultiSelect)
                {
                    var cb = new CheckBox { Content = opt.Label, Foreground = Brushes.White, FontSize = 13 };
                    cb.IsCheckedChanged += (_, _) =>
                    {
                        if (cb.IsChecked == true) selectedIds.Add(opt.Id);
                        else selectedIds.Remove(opt.Id);
                    };
                    input = cb;
                }
                else
                {
                    var rb = new RadioButton { Content = opt.Label, Foreground = Brushes.White, FontSize = 13, GroupName = $"poll_{poll.Id}" };
                    rb.IsCheckedChanged += (_, _) =>
                    {
                        if (rb.IsChecked == true) { selectedIds.Clear(); selectedIds.Add(opt.Id); }
                    };
                    input = rb;
                }
                optionControls.Add(input);
                stack.Children.Add(input);
            }

            var voteButton = ActionButton("Submit vote", Color.FromRgb(0x28, 0x7D, 0xC8));
            voteButton.HorizontalAlignment = HorizontalAlignment.Left;
            var status = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)) };
            voteButton.Click += async (_, _) =>
            {
                if (manager == null || selectedIds.Count == 0)
                {
                    status.Text = "Please select an option.";
                    return;
                }
                voteButton.IsEnabled = false;
                status.Text = "Submitting…";
                var result = await manager.VoteAsync(n.Id, poll.Id, selectedIds.ToList(), CancellationToken.None);
                if (!result.Success)
                {
                    voteButton.IsEnabled = true;
                    status.Text = $"Vote failed: {result.Message}";
                    return;
                }
                // Re-render whole detail
                Render();
            };

            var actionsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            actionsRow.Children.Add(voteButton);
            actionsRow.Children.Add(status);
            stack.Children.Add(actionsRow);

            return container;
        }

        private static void RenderPollResults(StackPanel stack, NotificationPoll poll, bool closed)
        {
            var totalVotes = poll.Options.Sum(o => o.Votes ?? 0);
            foreach (var opt in poll.Options)
            {
                var votes = opt.Votes ?? 0;
                var pct = opt.Percentage ?? (totalVotes > 0 ? (votes * 100.0 / totalVotes) : 0);
                var isMine = poll.VotedOptionIds.Contains(opt.Id);

                var label = new TextBlock
                {
                    Text = isMine ? $"✓ {opt.Label}" : opt.Label,
                    Foreground = isMine ? Brushes.White : new SolidColorBrush(Color.FromRgb(210, 210, 215)),
                    FontWeight = isMine ? FontWeight.SemiBold : FontWeight.Normal,
                    FontSize = 13
                };
                var pctText = new TextBlock
                {
                    Text = $"{pct:0.#}%  ({votes})",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 175)),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                var labelRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                labelRow.Children.Add(label); Grid.SetColumn(label, 0);
                labelRow.Children.Add(pctText); Grid.SetColumn(pctText, 1);

                var barBg = new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromRgb(50, 50, 56)),
                    Margin = new Thickness(0, 2, 0, 6)
                };
                var bar = new Border
                {
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(isMine ? Color.FromRgb(0x28, 0xC8, 0x7D) : Color.FromRgb(0x28, 0x7D, 0xC8)),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = Math.Max(2, pct * 3)
                };
                var barGrid = new Grid();
                barGrid.Children.Add(barBg);
                barGrid.Children.Add(bar);
                barBg.SizeChanged += (_, e) =>
                {
                    bar.Width = Math.Max(2, e.NewSize.Width * (pct / 100.0));
                };

                stack.Children.Add(labelRow);
                stack.Children.Add(barGrid);
            }
            stack.Children.Add(new TextBlock
            {
                Text = closed ? $"Poll closed • {poll.TotalVoters ?? totalVotes} voters" : $"{poll.TotalVoters ?? totalVotes} voters",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 140)),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }

        private static Control SectionHeader(string text) => new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 185)),
            Margin = new Thickness(0, 8, 0, 2)
        };

        private static Button LinkButton(string label, string url)
        {
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = label,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 175, 230)),
                    TextDecorations = TextDecorations.Underline,
                    FontSize = 13
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(btn, url);
            btn.Click += (_, _) => OpenUrl(url);
            return btn;
        }

        private static Button ActionButton(string text, Color background) => new()
        {
            Content = text,
            Background = new SolidColorBrush(background),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 6),
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        private static Border BadgePill(string text, Color color) => new()
        {
            Background = new SolidColorBrush(Color.FromArgb(40, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(color),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(7, 1),
            Child = new TextBlock { Text = text, FontSize = 10, Foreground = new SolidColorBrush(color) }
        };

        private static (Color accent, string icon) SeverityVisual(NotificationSeverity sev) => sev switch
        {
            NotificationSeverity.Success => (Color.FromRgb(40, 200, 125), "✅"),
            NotificationSeverity.Warning => (Color.FromRgb(230, 175, 60), "⚠"),
            NotificationSeverity.Critical => (Color.FromRgb(230, 80, 80), "⛔"),
            NotificationSeverity.Announcement => (Color.FromRgb(170, 130, 230), "📢"),
            _ => (Color.FromRgb(100, 160, 230), "ℹ")
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

        private static string ExtractPreview(string source)
        {
            var t = HtmlToPlainText(source).Replace("\n", " ");
            return t.Length <= 140 ? t : t[..140] + "…";
        }

        private static string ExtractPreview(Notification n)
        {
            string text;
            if (!string.IsNullOrWhiteSpace(n.BodyHtml))
                text = HtmlToPlainText(n.BodyHtml);
            else if (!string.IsNullOrWhiteSpace(n.BodyMarkdown))
                text = MarkdownRenderer.ToPlainText(n.BodyMarkdown!);
            else
                text = string.Empty;

            text = text.Replace("\n", " ");
            return text.Length <= 140 ? text : text[..140] + "…";
        }

        private static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB" };
            double n = bytes;
            var i = 0;
            while (n >= 1024 && i < suf.Length - 1) { n /= 1024; i++; }
            return $"{n:0.#} {suf[i]}";
        }

        private static void OpenUrl(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return;
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationsPanel] OpenUrl failed: {ex.Message}");
            }
        }

        private async void RefreshButton_Click(object? sender, RoutedEventArgs e)
        {
            if (manager == null) return;
            await manager.RefreshNowAsync();
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
