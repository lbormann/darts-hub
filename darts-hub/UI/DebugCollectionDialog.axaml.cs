using Avalonia;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using darts_hub.control;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    public partial class DebugCollectionDialog : Window
    {
        public sealed class ExtensionEntry : INotifyPropertyChanged
        {
            private bool _isSelected;

            public AppBase App { get; }
            public string DisplayName { get; }
            public bool IsCaller { get; }

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            public ExtensionEntry(AppBase app)
            {
                App = app ?? throw new ArgumentNullException(nameof(app));
                IsCaller = string.Equals(app.Name, "darts-caller", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(app.CustomName, "darts-caller", StringComparison.OrdinalIgnoreCase);
                DisplayName = IsCaller
                    ? $"{app.CustomName} (always included)"
                    : app.CustomName;
                _isSelected = IsCaller;
            }
        }

        public ObservableCollection<ExtensionEntry> Extensions { get; } = new();

        private readonly Profile? _profile;
        private readonly control.LicenseManager? _licenseManager;

        public DebugCollectionDialog(Profile? profile, control.LicenseManager? licenseManager = null)
        {
            _profile = profile;
            _licenseManager = licenseManager;
            InitializeComponent();
            PopulateExtensions();
            InitializeDate();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            var list = this.FindControl<ItemsControl>("ExtensionsItemsControl");
            if (list != null)
            {
                list.ItemsSource = Extensions;
            }
        }

        private void PopulateExtensions()
        {
            if (_profile?.Apps == null) return;

            var apps = _profile.Apps.Values
                .Where(s => s.App != null)
                .Select(s => s.App)
                .OrderByDescending(a => string.Equals(a.Name, "darts-caller", StringComparison.OrdinalIgnoreCase) ||
                                        string.Equals(a.CustomName, "darts-caller", StringComparison.OrdinalIgnoreCase))
                .ThenBy(a => a.CustomName, StringComparer.OrdinalIgnoreCase);

            foreach (var app in apps)
            {
                Extensions.Add(new ExtensionEntry(app));
            }
        }

        private void InitializeDate()
        {
            var picker = this.FindControl<DatePicker>("IncidentDatePicker");
            if (picker == null) return;

            picker.SelectedDate = DateTime.Today;
        }

        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            var statusPanel = this.FindControl<StackPanel>("StatusPanel");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var createButton = this.FindControl<Button>("CreateButton");
            var cancelButton = this.FindControl<Button>("CancelButton");
            var datePicker = this.FindControl<DatePicker>("IncidentDatePicker");
            var descriptionBox = this.FindControl<TextBox>("ProblemDescriptionTextBox");

            if (statusPanel == null || statusText == null || createButton == null || cancelButton == null) return;

            var description = descriptionBox?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                statusPanel.IsVisible = true;
                SetStatus(statusText, "Please describe the problem before creating the debug collection.", isError: true);
                return;
            }

            var incidentDate = datePicker?.SelectedDate?.LocalDateTime ?? DateTime.Today;
            if (incidentDate.Year != DateTime.Now.Year || incidentDate.Month != DateTime.Now.Month)
            {
                statusPanel.IsVisible = true;
                SetStatus(statusText,
                    "Daily log files only exist for the current month. Please pick a date in the current month or contact support directly for older incidents.",
                    isError: true);
                return;
            }

            var selectedApps = Extensions
                .Where(x => x.IsSelected)
                .Select(x => x.App)
                .ToList();

            createButton.IsEnabled = false;
            cancelButton.IsEnabled = false;
            statusPanel.IsVisible = true;
            SetStatus(statusText, "Collecting log files and configuration. This usually only takes a moment...", isError: false);

            try
            {
                var boardId = TryGetCallerBoardId();
                var licenseSnapshot = BuildLicenseSnapshot();
                var result = await DebugCollectionService.CreateAsync(selectedApps, description, incidentDate, boardId, licenseSnapshot);

                ShowSuccess(result);
            }
            catch (Exception ex)
            {
                SetStatus(statusText, $"Failed to create debug collection: {ex.Message}", isError: true);
                createButton.IsEnabled = true;
                cancelButton.IsEnabled = true;
            }
        }

        private string? TryGetCallerBoardId()
        {
            try
            {
                var caller = Extensions.FirstOrDefault(x => x.IsCaller)?.App
                             ?? _profile?.Apps?.Values
                                 .Select(s => s.App)
                                 .FirstOrDefault(a => string.Equals(a?.Name, "darts-caller", StringComparison.OrdinalIgnoreCase));

                var arg = caller?.Configuration?.Arguments?
                    .FirstOrDefault(a => string.Equals(a.Name, "B", StringComparison.OrdinalIgnoreCase));

                return arg?.Value;
            }
            catch
            {
                return null;
            }
        }

        private DebugCollectionService.LicenseSnapshot? BuildLicenseSnapshot()
        {
            try
            {
                if (_licenseManager == null) return null;

                var snapshot = new DebugCollectionService.LicenseSnapshot
                {
                    HasStoredKey = _licenseManager.HasStoredLicenseKey,
                    Status = _licenseManager.CurrentStatus.ToString(),
                    Message = _licenseManager.CurrentMessage ?? string.Empty,
                    ExpiresAt = _licenseManager.LastResult?.ExpiresAt,
                    FeatureCount = _licenseManager.LastResult?.Features?.Count ?? 0
                };

                try
                {
                    var hwid = control.LicenseManager.GetHardwareId();
                    if (!string.IsNullOrWhiteSpace(hwid) && hwid.Length >= 8)
                    {
                        // Only include the first 8 chars so it can correlate logs without
                        // exposing the full machine fingerprint.
                        snapshot.HardwareIdHashShort = hwid.Substring(0, 8);
                    }
                }
                catch
                {
                    // Hardware ID is optional, ignore failures.
                }

                return snapshot;
            }
            catch
            {
                return null;
            }
        }

        private void SetStatus(TextBlock statusText, string message, bool isError)
        {
            statusText.Text = message;
            statusText.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(240, 95, 95))
                : new SolidColorBrush(Color.FromRgb(191, 191, 191));
        }

        private void ShowSuccess(DebugCollectionService.CollectionResult result)
        {
            // Modern success view: gradient background, animated success ring,
            // staggered fade/slide-in for the cards, and modern button styling.
            var root = new Border
            {
                Padding = new Thickness(28, 26, 28, 22),
                CornerRadius = new CornerRadius(12),
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromRgb(0x1A, 0x1F, 0x2C), 0.0),
                        new GradientStop(Color.FromRgb(0x10, 0x18, 0x24), 0.55),
                        new GradientStop(Color.FromRgb(0x0C, 0x1F, 0x1F), 1.0)
                    }
                }
            };

            var stack = new StackPanel { Spacing = 16 };

            // ---- Hero ring with checkmark ----
            var ringHost = new Grid
            {
                Width = 130,
                Height = 130,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            // Outer pulsing glow
            var glowRing = new Border
            {
                Width = 130,
                Height = 130,
                CornerRadius = new CornerRadius(65),
                Background = new RadialGradientBrush
                {
                    Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                    Radius = 0.5,
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0x80, 0x3B, 0xE0, 0xA0), 0.0),
                        new GradientStop(Color.FromArgb(0x10, 0x3B, 0xE0, 0xA0), 0.7),
                        new GradientStop(Color.FromArgb(0x00, 0x3B, 0xE0, 0xA0), 1.0)
                    }
                },
                Opacity = 0.0,
                RenderTransform = new ScaleTransform(0.6, 0.6),
                RenderTransformOrigin = RelativePoint.Center
            };
            ringHost.Children.Add(glowRing);

            // Rotating dashed ring (subtle, slow)
            var spinRing = new Ellipse
            {
                Width = 110,
                Height = 110,
                Stroke = new SolidColorBrush(Color.FromArgb(0x55, 0x3B, 0xE0, 0xA0)),
                StrokeThickness = 2,
                StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { 4, 6 },
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                RenderTransform = new RotateTransform(0),
                RenderTransformOrigin = RelativePoint.Center,
                Opacity = 0.0
            };
            ringHost.Children.Add(spinRing);

            // Solid green disc with check
            var discTransform = new ScaleTransform(0.4, 0.4);
            var disc = new Border
            {
                Width = 88,
                Height = 88,
                CornerRadius = new CornerRadius(44),
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromRgb(0x36, 0xC7, 0x86), 0.0),
                        new GradientStop(Color.FromRgb(0x1F, 0x9C, 0x66), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                RenderTransform = discTransform,
                RenderTransformOrigin = RelativePoint.Center,
                Opacity = 0.0,
                Child = new TextBlock
                {
                    Text = "\u2713",
                    FontSize = 50,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            ringHost.Children.Add(disc);
            stack.Children.Add(ringHost);

            // ---- Title ----
            var title = new TextBlock
            {
                Text = "Debug collection ready",
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            stack.Children.Add(title);

            var subtitle = new TextBlock
            {
                Text = "Your support package has been created successfully.",
                Foreground = new SolidColorBrush(Color.FromRgb(0xB6, 0xD7, 0xC8)),
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            stack.Children.Add(subtitle);

            // ---- File path card ----
            var fileCard = BuildFilePathCard(result);
            stack.Children.Add(fileCard);

            // ---- Instructions card ----
            var instructions = BuildInstructionsCard();
            stack.Children.Add(instructions);

            // ---- Warnings (optional) ----
            Border? warnCard = null;
            if (result.Warnings.Count > 0)
            {
                warnCard = BuildWarningsCard(result.Warnings);
                stack.Children.Add(warnCard);
            }

            // ---- Buttons ----
            var buttons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 8, 0, 0),
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };

            var openFolderButton = BuildSecondaryButton("\uD83D\uDCC1  Open Folder",
                "Reveal the ZIP file in the file manager");
            openFolderButton.Click += (_, _) => OpenInFileManager(result);
            buttons.Children.Add(openFolderButton);

            var copyPathButton = BuildSecondaryButton("\uD83D\uDCCB  Copy Path",
                "Copy the file path to the clipboard");
            copyPathButton.Click += async (_, _) =>
            {
                try
                {
                    var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                    if (clipboard != null)
                    {
                        await clipboard.SetTextAsync(result.ZipFilePath);
                        FlashButton(copyPathButton, "Copied!");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DebugCollectionDialog] Clipboard copy failed: {ex.Message}");
                }
            };
            buttons.Children.Add(copyPathButton);

            var closeButton = BuildPrimaryButton("Close");
            closeButton.Click += (_, _) => Close();
            buttons.Children.Add(closeButton);

            stack.Children.Add(buttons);

            var scroll = new ScrollViewer
            {
                Content = stack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            root.Child = scroll;
            Content = root;

            // ---- Animations (fully software-driven, cross-platform safe) ----
            AnimateRing(glowRing, spinRing, disc, discTransform);
            FadeSlideIn(title, delayMs: 350, durationMs: 320);
            FadeSlideIn(subtitle, delayMs: 470, durationMs: 320);
            FadeSlideIn(fileCard, delayMs: 580, durationMs: 360);
            FadeSlideIn(instructions, delayMs: 720, durationMs: 360);
            if (warnCard != null) FadeSlideIn(warnCard, delayMs: 860, durationMs: 360);
            FadeSlideIn(buttons, delayMs: 980, durationMs: 320);
        }

        private static Border BuildFilePathCard(DebugCollectionService.CollectionResult result)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12),
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(new TextBlock
            {
                Text = "Generated file",
                Foreground = new SolidColorBrush(Color.FromRgb(0x9F, 0xC2, 0xB1)),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold
            });
            stack.Children.Add(new TextBlock
            {
                Text = System.IO.Path.GetFileName(result.ZipFilePath),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(new TextBlock
            {
                Text = result.ZipFilePath,
                Foreground = new SolidColorBrush(Color.FromRgb(0xB8, 0xC8, 0xC2)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace")
            });
            card.Child = stack;
            return card;
        }

        private Border BuildInstructionsCard()
        {
            var card = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(0x44, 0x58, 0x65, 0xF2), 0.0),
                        new GradientStop(Color.FromArgb(0x22, 0x58, 0x65, 0xF2), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x77, 0x58, 0x65, 0xF2)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12),
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            var stack = new StackPanel { Spacing = 8 };
            stack.Children.Add(new TextBlock
            {
                Text = "Share with the maintainers",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 14
            });
            stack.Children.Add(new TextBlock
            {
                Text = "Send the ZIP directly to I3uLL3t on Discord (DM) or post it in the official Darts-Hub Discord under the #bug-report channel together with a short description.",
                Foreground = new SolidColorBrush(Color.FromRgb(0xDD, 0xE2, 0xFF)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });

            var discordButtons = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 4, 0, 0)
            };
            discordButtons.Children.Add(BuildDiscordButton(
                "Darts-Hub Discord",
                "Open the official Darts-Hub Discord (#bug-report channel)",
                "https://discord.gg/aRhqH5WauV"));
            discordButtons.Children.Add(BuildDiscordButton(
                "DM I3uLL3t",
                "Open I3uLL3t's Discord profile to send a direct message",
                "https://discordapp.com/users/366537096414101504"));
            stack.Children.Add(discordButtons);

            stack.Children.Add(new TextBlock
            {
                Text = "Privacy: your Autodarts e-mail and password were stripped from the included config. Your license key is never part of the package.",
                Foreground = new SolidColorBrush(Color.FromRgb(0xBC, 0xC4, 0xEA)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            card.Child = stack;
            return card;
        }

        private static Border BuildWarningsCard(List<string> warnings)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xA5, 0x00)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x77, 0xFF, 0xA5, 0x00)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12),
                Opacity = 0.0,
                RenderTransform = new TranslateTransform(0, 12)
            };
            var stack = new StackPanel { Spacing = 4 };
            stack.Children.Add(new TextBlock
            {
                Text = $"Notes during collection ({warnings.Count})",
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xC8, 0x80)),
                FontWeight = FontWeight.Bold,
                FontSize = 13
            });
            foreach (var w in warnings)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"\u2022 {w}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0xB2)),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            card.Child = stack;
            return card;
        }

        private static Button BuildSecondaryButton(string text, string tooltip)
        {
            var button = new Button
            {
                Content = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xE6, 0xE6)),
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold
                },
                Padding = new Thickness(14, 8),
                Height = 36,
                Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6)
            };
            ToolTip.SetTip(button, tooltip);
            return button;
        }

        private static Button BuildPrimaryButton(string text)
        {
            return new Button
            {
                Content = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13
                },
                Padding = new Thickness(20, 8),
                Height = 36,
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.FromRgb(0x36, 0xC7, 0x86), 0.0),
                        new GradientStop(Color.FromRgb(0x1F, 0x9C, 0x66), 1.0)
                    }
                },
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0xBF, 0x72)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6)
            };
        }

        private static void FlashButton(Button button, string flashText)
        {
            // Briefly switch the inner text to provide quick visual feedback.
            if (button.Content is not TextBlock tb) return;
            var originalText = tb.Text;
            var originalForeground = tb.Foreground;
            tb.Text = flashText;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(0x36, 0xC7, 0x86));

            DispatcherTimer? revert = null;
            revert = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
            revert.Tick += (_, _) =>
            {
                revert!.Stop();
                tb.Text = originalText;
                tb.Foreground = originalForeground;
            };
            revert.Start();
        }

        private static void AnimateRing(Border glowRing, Ellipse spinRing, Border disc, ScaleTransform discTransform)
        {
            // Phase 1 (0-260ms):  glow + ring fade in (slight scale up)
            // Phase 2 (180-520ms): disc pop with overshoot
            // Phase 3 (continuous): spin ring rotates slowly, glow gently pulses

            var glowScale = (ScaleTransform)glowRing.RenderTransform!;
            var startTime = DateTime.UtcNow;
            var spinTransform = (RotateTransform)spinRing.RenderTransform!;

            DispatcherTimer? timer = null;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

                // Glow + spinRing fade-in / scale in (0..260ms)
                var introT = Math.Clamp(elapsed / 260.0, 0, 1);
                var introEased = 1 - Math.Pow(1 - introT, 3);
                glowRing.Opacity = 0.85 * introEased;
                spinRing.Opacity = 0.9 * introEased;
                var glowS = 0.6 + 0.4 * introEased;
                glowScale.ScaleX = glowS;
                glowScale.ScaleY = glowS;

                // Disc pop with overshoot (180..520ms)
                var discT = Math.Clamp((elapsed - 180) / 340.0, 0, 1);
                if (discT > 0)
                {
                    // Easing with overshoot (back-out).
                    var s = 1.70158;
                    var t1 = discT - 1;
                    var eased = t1 * t1 * ((s + 1) * t1 + s) + 1;
                    var scale = 0.4 + (1.0 - 0.4) * eased;
                    discTransform.ScaleX = scale;
                    discTransform.ScaleY = scale;
                    disc.Opacity = Math.Clamp(discT * 1.6, 0, 1);
                }

                // Continuous rotation + glow pulse (after intro)
                if (elapsed > 260)
                {
                    var spinElapsed = elapsed - 260;
                    spinTransform.Angle = (spinElapsed / 30.0) % 360.0; // ~12s per rev

                    var pulse = 0.85 + 0.12 * Math.Sin(spinElapsed / 380.0);
                    glowRing.Opacity = pulse;
                }

                // Stop when window closes - safety guard.
                if (elapsed > 60_000)
                {
                    timer!.Stop();
                }
            };
            timer.Start();

            // Make sure the timer is stopped when the window is closed to avoid
            // leaking a timer that keeps a closed window alive.
            EventHandler? closedHandler = null;
            closedHandler = (_, _) =>
            {
                timer.Stop();
                if (disc.GetVisualRoot() is Window w) w.Closed -= closedHandler;
            };
            if (disc.GetVisualRoot() is Window win)
            {
                win.Closed += closedHandler;
            }
            else
            {
                disc.AttachedToVisualTree += (_, _) =>
                {
                    if (disc.GetVisualRoot() is Window w2)
                    {
                        w2.Closed += closedHandler;
                    }
                };
            }
        }

        private static void FadeSlideIn(Control target, int delayMs, int durationMs)
        {
            var translate = target.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 12);
            target.RenderTransform = translate;
            target.Opacity = 0.0;

            var startTime = DateTime.UtcNow.AddMilliseconds(delayMs);

            DispatcherTimer? timer = null;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (_, _) =>
            {
                var now = DateTime.UtcNow;
                if (now < startTime) return;

                var t = (now - startTime).TotalMilliseconds / durationMs;
                if (t >= 1.0)
                {
                    target.Opacity = 1.0;
                    translate.Y = 0;
                    timer!.Stop();
                    return;
                }

                var eased = 1 - Math.Pow(1 - t, 3);
                target.Opacity = eased;
                translate.Y = 12 * (1 - eased);
            };
            timer.Start();
        }

        private static void OpenInFileManager(DebugCollectionService.CollectionResult result)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var fileToSelect = File.Exists(result.ZipFilePath) ? result.ZipFilePath : result.ZipFolderPath;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{fileToSelect}\"",
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = $"\"{result.ZipFolderPath}\"",
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{result.ZipFolderPath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DebugCollectionDialog] Failed to open file manager: {ex.Message}");
            }
        }

        private static Button BuildDiscordButton(string text, string tooltip, string url)
        {
            var content = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            try
            {
                content.Children.Add(new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(
                        Avalonia.Platform.AssetLoader.Open(new Uri("avares://darts-hub/Assets/discord.png"))),
                    Width = 22,
                    Height = 22,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });
            }
            catch
            {
                // If the asset cannot be loaded fall back to a unicode bullet so the button still works.
                content.Children.Add(new TextBlock { Text = "\u25CF", Foreground = Brushes.White, FontSize = 16 });
            }
            content.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontWeight = FontWeight.SemiBold,
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });

            var button = new Button
            {
                Content = content,
                Padding = new Thickness(12, 6),
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(0x58, 0x65, 0xF2)), // Discord brand color
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x4E, 0xCC)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6)
            };
            ToolTip.SetTip(button, tooltip);
            button.Click += (_, _) => OpenUrl(url);
            return button;
        }

        private static void OpenUrl(string url)
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
                Debug.WriteLine($"[DebugCollectionDialog] Failed to open url '{url}': {ex.Message}");
            }
        }
    }
}
