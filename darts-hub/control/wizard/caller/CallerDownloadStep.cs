using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.caller
{
    /// <summary>
    /// Download configuration step for Caller guided configuration
    /// </summary>
    public class CallerDownloadStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onDownloadConfigSelected;
        private readonly Action onDownloadConfigSkipped;
        private bool isProcessing = false; // ⭐ Flag to prevent multiple clicks

        public bool ShowDownloadConfiguration { get; private set; } = false;

        public CallerDownloadStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onDownloadConfigSelected, Action onDownloadConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onDownloadConfigSelected = onDownloadConfigSelected;
            this.onDownloadConfigSkipped = onDownloadConfigSkipped;
        }

        public Border CreateDownloadConfigQuestionCard()
        {
            var card = new Border
            {
                Name = "DownloadConfigCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 23, 162, 184)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 10)
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            header.Children.Add(new TextBlock
            {
                Text = "📥",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Voice Pack Downloads",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to configure automatic downloading of voice packs and caller announcements?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "This allows automatic downloading of new voice packs, caller announcements, and language packs for enhanced game experience.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 255)),
                TextWrapping = TextWrapping.Wrap
            });

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };

            var configureButton = new Button
            {
                Content = "✅ Configure Downloads",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var skipButton = new Button
            {
                Content = "❌ Skip for Now",
                Padding = new Avalonia.Thickness(15, 8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            configureButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowDownloadConfiguration = true;
                    ShowDownloadConfigSettings(content);
                    
                    // Disable both buttons after selection
                    configureButton.IsEnabled = false;
                    skipButton.IsEnabled = false;
                    
                    onDownloadConfigSelected?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            skipButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowDownloadConfiguration = false;
                    
                    // Disable both buttons after selection
                    configureButton.IsEnabled = false;
                    skipButton.IsEnabled = false;
                    
                    onDownloadConfigSkipped?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            buttonPanel.Children.Add(configureButton);
            buttonPanel.Children.Add(skipButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        private void ShowDownloadConfigSettings(StackPanel content)
        {
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Opacity = 0.3,
                Margin = new Avalonia.Thickness(0, 15)
            };
            content.Children.Add(separator);

            var settingsLabel = new TextBlock
            {
                Text = "Download Configuration",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Download-related arguments
            var downloadArgs = new[] { "DL", "DLLA", "DLN", "ROVP" }; // Download Limit, Language, Name, Remove Old Voice Packs

            foreach (var argName in downloadArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    var control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetDownloadConfigDescription);
                    content.Children.Add(control);
                }
            }
        }

        private string GetDownloadConfigDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions for download-related Caller arguments
            return argument.Name.ToUpper() switch
            {
                "DL" => "Download limit for voice packs and caller announcements (in MB)",
                "DLLA" => "Preferred language for downloading voice packs (e.g., 'en', 'de', 'fr')",
                "DLN" => "Specific caller name to download (leave empty for random selection)",
                "ROVP" => "Remove old voice packs when downloading new ones to save disk space",
                _ => $"Download setting for caller: {argument.NameHuman}"
            };
        }
    }
}