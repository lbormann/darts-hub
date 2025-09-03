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
    /// Media configuration step for GIF guided configuration
    /// </summary>
    public class GifMediaConfigStep
    {
        private readonly AppBase gifApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onMediaConfigSelected;
        private readonly Action onMediaConfigSkipped;

        public bool ShowMediaConfiguration { get; private set; }

        public GifMediaConfigStep(AppBase gifApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onMediaConfigSelected, Action onMediaConfigSkipped)
        {
            this.gifApp = gifApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onMediaConfigSelected = onMediaConfigSelected;
            this.onMediaConfigSkipped = onMediaConfigSkipped;
        }

        public Border CreateMediaConfigQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 220, 20, 60)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "MediaConfigCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎬 Media & Animation Settings",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like to configure advanced media playback and animation settings?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            // Yes/No buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var yesButton = new Button
            {
                Content = "🎨 Yes, configure media settings",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var noButton = new Button
            {
                Content = "❌ Use default media settings",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 98, 104)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            yesButton.Click += (s, e) =>
            {
                ShowMediaConfiguration = true;
                ShowMediaConfigSettings(content);
                onMediaConfigSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowMediaConfiguration = false;
                onMediaConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Media configuration settings (initially hidden)
            var mediaConfigPanel = new StackPanel { Spacing = 10, IsVisible = false };
            mediaConfigPanel.Name = "MediaConfigPanel";

            // Media configuration arguments: Duration, Auto-play, Loop, etc.
            var mediaConfigArgs = new[] { "DU", "AUTOPLAY", "LOOP", "VOLUME", "SCALING", "FILTER" };
            foreach (var argName in mediaConfigArgs)
            {
                var argument = gifApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use simple controls for media configuration
                    var control = GifArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetMediaConfigDescription);
                    mediaConfigPanel.Children.Add(control);
                }
            }

            content.Children.Add(mediaConfigPanel);
            card.Child = content;
            return card;
        }

        private void ShowMediaConfigSettings(StackPanel content)
        {
            var configPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "MediaConfigPanel");
            if (configPanel != null)
            {
                configPanel.IsVisible = true;

                // Add separator before settings
                configPanel.Children.Insert(0, new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Avalonia.Thickness(20, 15)
                });

                // Add header
                configPanel.Children.Insert(1, new TextBlock
                {
                    Text = "🎬 Media Playback Configuration",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                });

                configPanel.Children.Insert(2, new TextBlock
                {
                    Text = "Customize how GIFs, images, and videos are displayed:",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 10)
                });
            }
        }

        private string GetMediaConfigDescription(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "DU" => "Duration (in seconds) to display each GIF or image - 0 means use file's natural duration",
                "AUTOPLAY" => "Automatically start playing media when displayed",
                "LOOP" => "Loop GIFs and videos continuously during playback",
                "VOLUME" => "Audio volume level for videos with sound (0.0 to 1.0)",
                "SCALING" => "How to scale media to fit the display window (fit, fill, stretch, none)",
                "FILTER" => "File type filter for media files (e.g., *.gif;*.jpg;*.png;*.mp4)",
                "QUALITY" => "Display quality setting for media rendering",
                "CACHE" => "Enable media file caching for faster loading",
                _ => $"Media configuration setting: {argument.NameHuman}"
            };
        }
    }
}