using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace darts_hub.UI
{
    public partial class UpdateDialog : Window
    {
        private string? _version;
        private string? _changelogMarkdown;

        public UpdateDialog()
        {
            InitializeComponent();
            AttachHandlers();
        }

        public void SetData(string version, string changelogMarkdown)
        {
            _version = version;
            _changelogMarkdown = changelogMarkdown;

            if (this.FindControl<TextBlock>("TitleTextBlock") is { } titleBlock)
                titleBlock.Text = "Update available";

            if (this.FindControl<TextBlock>("VersionTextBlock") is { } versionBlock)
                versionBlock.Text = $"Version: {version}";

            RenderMarkdown();
        }

        private void RenderMarkdown()
        {
            var host = this.FindControl<ContentControl>("MarkdownHost");
            if (host == null)
                return;

            host.Content = BuildSimpleMarkdownView(_changelogMarkdown ?? string.Empty);
        }

        private Control BuildSimpleMarkdownView(string markdown)
        {
            var stack = new StackPanel { Spacing = 8, Margin = new Thickness(4) };
            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            var buffer = new List<string>();

            void FlushParagraph()
            {
                if (buffer.Count == 0) return;
                var text = string.Join(" ", buffer).Trim();
                if (text.Length == 0)
                {
                    buffer.Clear();
                    return;
                }

                stack.Children.Add(new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
                buffer.Clear();
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock { Text = "", Height = 4 });
                    continue;
                }

                if (line.StartsWith("### "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[4..].Trim(),
                        FontSize = 16,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("## "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[3..].Trim(),
                        FontSize = 18,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("# "))
                {
                    FlushParagraph();
                    stack.Children.Add(new TextBlock
                    {
                        Text = line[2..].Trim(),
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        TextWrapping = TextWrapping.Wrap
                    });
                    continue;
                }

                if (line.StartsWith("- ") || line.StartsWith("* "))
                {
                    FlushParagraph();
                    var bullet = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
                    bullet.Children.Add(new TextBlock
                    {
                        Text = "•",
                        FontSize = 14,
                        FontWeight = FontWeight.Bold
                    });
                    bullet.Children.Add(new TextBlock
                    {
                        Text = line[2..].Trim(),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14
                    });
                    stack.Children.Add(bullet);
                    continue;
                }

                buffer.Add(line.Trim());
            }

            FlushParagraph();

            if (stack.Children.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = markdown,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
            }

            return stack;
        }

        private void AttachHandlers()
        {
            if (this.FindControl<Button>("UpdateButton") is { } updateButton)
                updateButton.Click += (_, __) => Close(true);

            if (this.FindControl<Button>("SkipButton") is { } skipButton)
                skipButton.Click += (_, __) => Close(false);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
