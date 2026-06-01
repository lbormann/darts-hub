using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace darts_hub.UI
{
    /// <summary>
    /// Minimal Markdown renderer for notification bodies.
    /// Supports: headings, paragraphs, blank lines, unordered &amp; ordered lists,
    /// fenced + indented code blocks, blockquotes, horizontal rules,
    /// inline bold (**text**), italic (*text* / _text_), inline code (`code`),
    /// and links ([label](url)). Unknown syntax is rendered as plain text.
    /// </summary>
    public static class MarkdownRenderer
    {
        private static readonly Regex InlineLinkRegex = new(@"\[(?<label>[^\]]+)\]\((?<url>[^)\s]+)\)", RegexOptions.Compiled);
        private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
        private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex ItalicAsterisk = new(@"(?<!\*)\*(?!\s)(.+?)(?<!\s)\*(?!\*)", RegexOptions.Compiled);
        private static readonly Regex ItalicUnderscore = new(@"(?<!_)_(?!\s)(.+?)(?<!\s)_(?!_)", RegexOptions.Compiled);

        private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(225, 225, 230));
        private static readonly IBrush MutedBrush = new SolidColorBrush(Color.FromRgb(170, 170, 175));
        private static readonly IBrush CodeBrush = new SolidColorBrush(Color.FromRgb(245, 200, 120));
        private static readonly IBrush LinkBrush = new SolidColorBrush(Color.FromRgb(140, 175, 230));
        private static readonly IBrush CodeBg = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
        private static readonly IBrush QuoteBar = new SolidColorBrush(Color.FromRgb(120, 130, 160));

        /// <summary>
        /// Builds a <see cref="StackPanel"/> containing block elements rendered from Markdown.
        /// </summary>
        public static StackPanel Render(string markdown, double baseFontSize = 13)
        {
            var panel = new StackPanel { Spacing = 6 };
            if (string.IsNullOrWhiteSpace(markdown)) return panel;

            var lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var paragraph = new List<string>();
            var i = 0;

            void FlushParagraph()
            {
                if (paragraph.Count == 0) return;
                var text = string.Join(" ", paragraph).Trim();
                paragraph.Clear();
                if (text.Length == 0) return;
                panel.Children.Add(BuildInlineBlock(text, baseFontSize));
            }

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmed = line.TrimEnd();

                // Blank line -> paragraph break
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    FlushParagraph();
                    i++;
                    continue;
                }

                // Fenced code block
                if (trimmed.StartsWith("```"))
                {
                    FlushParagraph();
                    var sb = new StringBuilder();
                    i++;
                    while (i < lines.Length && !lines[i].TrimEnd().StartsWith("```"))
                    {
                        sb.AppendLine(lines[i]);
                        i++;
                    }
                    if (i < lines.Length) i++; // skip closing fence
                    panel.Children.Add(BuildCodeBlock(sb.ToString().TrimEnd(), baseFontSize));
                    continue;
                }

                // Horizontal rule
                if (Regex.IsMatch(trimmed, @"^(\s*-\s*-\s*-+|\s*\*\s*\*\s*\*+|\s*_\s*_\s*_+)$"))
                {
                    FlushParagraph();
                    panel.Children.Add(new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.FromRgb(80, 80, 86)),
                        Margin = new Thickness(0, 6, 0, 6)
                    });
                    i++;
                    continue;
                }

                // Headings
                var headingMatch = Regex.Match(trimmed, @"^(#{1,6})\s+(.*)$");
                if (headingMatch.Success)
                {
                    FlushParagraph();
                    var level = headingMatch.Groups[1].Value.Length;
                    var text = headingMatch.Groups[2].Value;
                    panel.Children.Add(BuildHeading(text, level, baseFontSize));
                    i++;
                    continue;
                }

                // Blockquote
                if (trimmed.StartsWith(">"))
                {
                    FlushParagraph();
                    var quoteLines = new List<string>();
                    while (i < lines.Length && lines[i].TrimEnd().StartsWith(">"))
                    {
                        var content = Regex.Replace(lines[i].TrimEnd(), @"^>\s?", "");
                        quoteLines.Add(content);
                        i++;
                    }
                    panel.Children.Add(BuildBlockquote(string.Join(" ", quoteLines), baseFontSize));
                    continue;
                }

                // Unordered list
                if (Regex.IsMatch(trimmed, @"^[-*+]\s+"))
                {
                    FlushParagraph();
                    panel.Children.Add(BuildList(lines, ref i, ordered: false, baseFontSize));
                    continue;
                }

                // Ordered list
                if (Regex.IsMatch(trimmed, @"^\d+\.\s+"))
                {
                    FlushParagraph();
                    panel.Children.Add(BuildList(lines, ref i, ordered: true, baseFontSize));
                    continue;
                }

                // Otherwise: paragraph line
                paragraph.Add(trimmed);
                i++;
            }

            FlushParagraph();
            return panel;
        }

        private static Control BuildHeading(string text, int level, double baseFontSize)
        {
            double size = level switch
            {
                1 => baseFontSize + 8,
                2 => baseFontSize + 5,
                3 => baseFontSize + 3,
                _ => baseFontSize + 1
            };

            var tb = new SelectableTextBlock
            {
                FontSize = size,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, level <= 2 ? 8 : 4, 0, 2)
            };
            ApplyInlines(tb, text, size, isHeading: true);
            return tb;
        }

        private static Control BuildInlineBlock(string text, double baseFontSize)
        {
            var tb = new SelectableTextBlock
            {
                FontSize = baseFontSize,
                Foreground = TextBrush,
                TextWrapping = TextWrapping.Wrap
            };
            ApplyInlines(tb, text, baseFontSize);
            return tb;
        }

        private static Control BuildCodeBlock(string code, double baseFontSize)
        {
            return new Border
            {
                Background = CodeBg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8),
                Margin = new Thickness(0, 4, 0, 4),
                Child = new SelectableTextBlock
                {
                    Text = code,
                    FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
                    FontSize = baseFontSize - 1,
                    Foreground = CodeBrush,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private static Control BuildBlockquote(string text, double baseFontSize)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("4,*") };
            var bar = new Border
            {
                Background = QuoteBar,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 2, 8, 2)
            };
            Grid.SetColumn(bar, 0);
            grid.Children.Add(bar);

            var tb = new SelectableTextBlock
            {
                FontSize = baseFontSize,
                FontStyle = FontStyle.Italic,
                Foreground = MutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(8, 0, 0, 0)
            };
            ApplyInlines(tb, text, baseFontSize);
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);
            return grid;
        }

        private static Control BuildList(string[] lines, ref int i, bool ordered, double baseFontSize)
        {
            var stack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 2) };
            int index = 1;
            var pattern = ordered ? @"^\d+\.\s+(.*)$" : @"^[-*+]\s+(.*)$";
            var checkPattern = ordered ? @"^\d+\.\s+" : @"^[-*+]\s+";

            while (i < lines.Length && Regex.IsMatch(lines[i].TrimEnd(), checkPattern))
            {
                var match = Regex.Match(lines[i].TrimEnd(), pattern);
                var content = match.Success ? match.Groups[1].Value : lines[i];

                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("24,*") };
                var bullet = new SelectableTextBlock
                {
                    Text = ordered ? $"{index}." : "•",
                    Foreground = MutedBrush,
                    FontSize = baseFontSize,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                Grid.SetColumn(bullet, 0);
                row.Children.Add(bullet);

                var tb = new SelectableTextBlock
                {
                    FontSize = baseFontSize,
                    Foreground = TextBrush,
                    TextWrapping = TextWrapping.Wrap
                };
                ApplyInlines(tb, content, baseFontSize);
                Grid.SetColumn(tb, 1);
                row.Children.Add(tb);

                stack.Children.Add(row);
                index++;
                i++;
            }

            return stack;
        }

        /// <summary>
        /// Parses inline markdown (bold/italic/code/link) and appends Inline runs to the textblock.
        /// </summary>
        private static void ApplyInlines(SelectableTextBlock tb, string text, double baseFontSize, bool isHeading = false)
        {
            tb.Inlines = new InlineCollection();

            // We tokenize by scanning the string and looking for the next match of any inline rule.
            int cursor = 0;
            while (cursor < text.Length)
            {
                var nextMatch = FindNextInline(text, cursor, out var kind);
                if (nextMatch == null)
                {
                    AppendPlain(tb.Inlines, text.Substring(cursor), baseFontSize, isHeading);
                    break;
                }

                if (nextMatch.Index > cursor)
                    AppendPlain(tb.Inlines, text.Substring(cursor, nextMatch.Index - cursor), baseFontSize, isHeading);

                switch (kind)
                {
                    case InlineKind.Link:
                        AppendLink(tb.Inlines, nextMatch.Groups["label"].Value, nextMatch.Groups["url"].Value, baseFontSize);
                        break;
                    case InlineKind.Code:
                        AppendCode(tb.Inlines, nextMatch.Groups[1].Value, baseFontSize);
                        break;
                    case InlineKind.Bold:
                        AppendStyled(tb.Inlines, nextMatch.Groups[1].Value, FontWeight.Bold, FontStyle.Normal, baseFontSize, isHeading);
                        break;
                    case InlineKind.Italic:
                        AppendStyled(tb.Inlines, nextMatch.Groups[1].Value, isHeading ? FontWeight.Bold : FontWeight.Normal, FontStyle.Italic, baseFontSize, isHeading);
                        break;
                }
                cursor = nextMatch.Index + nextMatch.Length;
            }
        }

        private enum InlineKind { Link, Code, Bold, Italic }

        private static Match? FindNextInline(string text, int from, out InlineKind kind)
        {
            kind = InlineKind.Bold;
            Match? best = null;
            InlineKind bestKind = InlineKind.Bold;

            void Consider(Match m, InlineKind k)
            {
                if (!m.Success || m.Index < from) return;
                if (best == null || m.Index < best.Index)
                {
                    best = m;
                    bestKind = k;
                }
            }

            Consider(InlineLinkRegex.Match(text, from), InlineKind.Link);
            Consider(InlineCodeRegex.Match(text, from), InlineKind.Code);
            Consider(BoldRegex.Match(text, from), InlineKind.Bold);
            Consider(ItalicAsterisk.Match(text, from), InlineKind.Italic);
            Consider(ItalicUnderscore.Match(text, from), InlineKind.Italic);

            kind = bestKind;
            return best;
        }

        private static void AppendPlain(InlineCollection inlines, string text, double fontSize, bool isHeading)
        {
            if (string.IsNullOrEmpty(text)) return;
            inlines.Add(new Run
            {
                Text = text,
                FontSize = fontSize,
                Foreground = isHeading ? Brushes.White : TextBrush,
                FontWeight = isHeading ? FontWeight.Bold : FontWeight.Normal
            });
        }

        private static void AppendStyled(InlineCollection inlines, string text, FontWeight weight, FontStyle style, double fontSize, bool isHeading)
        {
            inlines.Add(new Run
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = weight,
                FontStyle = style,
                Foreground = isHeading ? Brushes.White : TextBrush
            });
        }

        private static void AppendCode(InlineCollection inlines, string text, double fontSize)
        {
            inlines.Add(new Run
            {
                Text = text,
                FontSize = fontSize - 1,
                FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace"),
                Foreground = CodeBrush,
                Background = CodeBg
            });
        }

        private static void AppendLink(InlineCollection inlines, string label, string url, double fontSize)
        {
            var btn = new Button
            {
                Content = new TextBlock
                {
                    Text = label,
                    Foreground = LinkBrush,
                    TextDecorations = TextDecorations.Underline,
                    FontSize = fontSize
                },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            ToolTip.SetTip(btn, url);
            btn.Click += (_, _) => OpenUrl(url);
            inlines.Add(new InlineUIContainer(btn));
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
                Debug.WriteLine($"[MarkdownRenderer] OpenUrl failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Quick Markdown ? plain text for previews/toasts.
        /// </summary>
        public static string ToPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return string.Empty;
            var s = Regex.Replace(markdown, @"```[\s\S]*?```", " ");
            s = InlineCodeRegex.Replace(s, "$1");
            s = InlineLinkRegex.Replace(s, "${label}");
            s = BoldRegex.Replace(s, "$1");
            s = ItalicAsterisk.Replace(s, "$1");
            s = ItalicUnderscore.Replace(s, "$1");
            s = Regex.Replace(s, @"^\s{0,3}#{1,6}\s+", "", RegexOptions.Multiline);
            s = Regex.Replace(s, @"^\s*[-*+]\s+", "• ", RegexOptions.Multiline);
            s = Regex.Replace(s, @"^\s*\d+\.\s+", "", RegexOptions.Multiline);
            s = Regex.Replace(s, @"^>\s?", "", RegexOptions.Multiline);
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s;
        }
    }
}
