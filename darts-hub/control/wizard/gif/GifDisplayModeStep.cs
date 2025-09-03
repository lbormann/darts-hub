using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.gif
{
    /// <summary>
    /// Display mode configuration step for GIF guided configuration
    /// </summary>
    public class GifDisplayModeStep
    {
        private readonly AppBase gifApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onDisplayModeSelected;
        private readonly Action onDisplayModeSkipped;

        public bool ShowDisplayModeSettings { get; private set; }
        public string SelectedDisplayMode { get; private set; } = "windowed";

        public GifDisplayModeStep(AppBase gifApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onDisplayModeSelected, Action onDisplayModeSkipped)
        {
            this.gifApp = gifApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onDisplayModeSelected = onDisplayModeSelected;
            this.onDisplayModeSkipped = onDisplayModeSkipped;
        }

        public Border CreateDisplayModeQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 140, 0)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "DisplayModeCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🖥️ Display Mode Configuration",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "How would you like to display GIFs and media during games?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            // Display mode options
            var modePanel = new StackPanel { Spacing = 15 };

            // Windowed mode button
            var windowedButton = new Button
            {
                Content = "🖼️ Windowed Display",
                Padding = new Avalonia.Thickness(20, 15),
                Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 110, 150)),
                BorderThickness = new Avalonia.Thickness(2),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 200
            };

            // Web-based mode button
            var webButton = new Button
            {
                Content = "🌐 Web-Based Display",
                Padding = new Avalonia.Thickness(20, 15),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(2),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 200
            };

            // Skip button
            var skipButton = new Button
            {
                Content = "❌ Use Default Settings",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 98, 104)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };

            // Add descriptions for each mode
            var descriptionPanel = new StackPanel { Spacing = 10, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
            
            descriptionPanel.Children.Add(new TextBlock
            {
                Text = "🖼️ Windowed: Display GIFs in a desktop window on your computer",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            descriptionPanel.Children.Add(new TextBlock
            {
                Text = "🌐 Web-Based: Stream GIFs to web browser for remote viewing",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            windowedButton.Click += (s, e) =>
            {
                SelectedDisplayMode = "windowed";
                ShowDisplayModeSettings = true;
                ShowDisplayModeConfiguration(content, "windowed");
                onDisplayModeSelected?.Invoke();
            };

            webButton.Click += (s, e) =>
            {
                SelectedDisplayMode = "web";
                ShowDisplayModeSettings = true;
                ShowDisplayModeConfiguration(content, "web");
                onDisplayModeSelected?.Invoke();
            };

            skipButton.Click += (s, e) =>
            {
                ShowDisplayModeSettings = false;
                onDisplayModeSkipped?.Invoke();
            };

            modePanel.Children.Add(windowedButton);
            modePanel.Children.Add(webButton);
            content.Children.Add(modePanel);
            content.Children.Add(descriptionPanel);
            content.Children.Add(skipButton);

            // Display mode settings (initially hidden)
            var displayModePanel = new StackPanel { Spacing = 10, IsVisible = false };
            displayModePanel.Name = "DisplayModePanel";

            content.Children.Add(displayModePanel);
            card.Child = content;
            return card;
        }

        private void ShowDisplayModeConfiguration(StackPanel content, string mode)
        {
            var displayPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "DisplayModePanel");
            if (displayPanel != null)
            {
                displayPanel.Children.Clear();
                displayPanel.IsVisible = true;

                // Add separator
                displayPanel.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Avalonia.Thickness(20, 15)
                });

                // Add mode-specific configuration
                if (mode == "windowed")
                {
                    ShowWindowedModeSettings(displayPanel);
                }
                else if (mode == "web")
                {
                    ShowWebModeSettings(displayPanel);
                }
            }
        }

        private void ShowWindowedModeSettings(StackPanel displayPanel)
        {
            displayPanel.Children.Add(new TextBlock
            {
                Text = "🖼️ Windowed Display Settings",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 5)
            });

            displayPanel.Children.Add(new TextBlock
            {
                Text = "Configure windowed display options:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });

            // Set WEB to false for windowed mode
            var webArg = gifApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "WEB");
            if (webArg != null)
            {
                webArg.Value = "False";
                webArg.IsValueChanged = true;
            }

            // Add windowed-specific arguments if they exist
            var windowedArgs = new[] { "FULLSCREEN", "WINDOW_WIDTH", "WINDOW_HEIGHT", "ALWAYS_ON_TOP" };
            foreach (var argName in windowedArgs)
            {
                var argument = gifApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    var control = GifArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetDisplayModeDescription);
                    displayPanel.Children.Add(control);
                }
            }
        }

        private void ShowWebModeSettings(StackPanel displayPanel)
        {
            displayPanel.Children.Add(new TextBlock
            {
                Text = "🌐 Web-Based Display Settings",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 5)
            });

            displayPanel.Children.Add(new TextBlock
            {
                Text = "Configure web interface for remote viewing:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });

            // Set WEB to true for web mode
            var webArg = gifApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "WEB");
            if (webArg != null)
            {
                webArg.Value = "True";
                webArg.IsValueChanged = true;
            }

            // Show web port configuration if it wasn't already shown in essential settings
            var webPortArg = gifApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "WEBP");
            if (webPortArg != null && !argumentControls.ContainsKey("WEBP"))
            {
                var control = GifArgumentControlFactory.CreateSimpleArgumentControl(webPortArg, argumentControls, GetDisplayModeDescription);
                displayPanel.Children.Add(control);
            }

            // Add web access info
            displayPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 70, 130, 180)),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15),
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            var infoPanel = new StackPanel { Spacing = 8 };
            infoPanel.Children.Add(new TextBlock
            {
                Text = "ℹ️ Web Access Information",
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = "After starting the GIF display, access the web interface at:",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            infoPanel.Children.Add(new TextBlock
            {
                Text = "http://localhost:[PORT] (replace [PORT] with your configured port)",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 120)),
                Background = new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
                Padding = new Avalonia.Thickness(8, 4),
                Margin = new Avalonia.Thickness(0, 5, 0, 0)
            });

            ((Border)displayPanel.Children.Last()).Child = infoPanel;
        }

        private string GetDisplayModeDescription(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "FULLSCREEN" => "Display GIFs in fullscreen mode",
                "WINDOW_WIDTH" => "Width of the display window in pixels",
                "WINDOW_HEIGHT" => "Height of the display window in pixels",
                "ALWAYS_ON_TOP" => "Keep the display window always on top of other windows",
                "WEBP" => "Port number for web-based display interface",
                "WEB" => "Enable web-based display interface",
                _ => $"Display mode setting: {argument.NameHuman}"
            };
        }
    }
}