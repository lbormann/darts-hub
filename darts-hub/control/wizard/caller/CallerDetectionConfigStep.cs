using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.caller
{
    /// <summary>
    /// Advanced call settings step for Caller guided configuration
    /// </summary>
    public class CallerDetectionConfigStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onDetectionConfigSelected;
        private readonly Action onDetectionConfigSkipped;

        public bool ShowDetectionConfiguration { get; private set; }

        public CallerDetectionConfigStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onDetectionConfigSelected, Action onDetectionConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onDetectionConfigSelected = onDetectionConfigSelected;
            this.onDetectionConfigSkipped = onDetectionConfigSkipped;
        }

        public Border CreateDetectionConfigQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 220, 20, 60)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "DetectionConfigCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "📢 Advanced Call Settings",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like to configure advanced calling behavior and announcement preferences?",
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
                Content = "✅ Yes, configure advanced settings",
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
                Content = "❌ Use default call settings",
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
                ShowDetectionConfiguration = true;
                ShowDetectionConfigSettings(content);
                onDetectionConfigSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowDetectionConfiguration = false;
                onDetectionConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Advanced call configuration settings (initially hidden)
            var detectionConfigPanel = new StackPanel { Spacing = 10, IsVisible = false };
            detectionConfigPanel.Name = "DetectionConfigPanel";

            // Advanced call configuration arguments: Call frequency, checkout calls, ambient sounds, service settings
            var callConfigArgs = new[] { "CCP", "E", "ETS", "PCC", "PCCYO", "A", "AAC", "BAV", "LPB", "CRL" };
            foreach (var argName in callConfigArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use simple controls for advanced settings
                    var control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetAdvancedCallDescription);
                    detectionConfigPanel.Children.Add(control);
                }
            }

            content.Children.Add(detectionConfigPanel);
            card.Child = content;
            return card;
        }

        private void ShowDetectionConfigSettings(StackPanel content)
        {
            var configPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "DetectionConfigPanel");
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
                    Text = "📢 Advanced Call Configuration",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                });

                configPanel.Children.Insert(2, new TextBlock
                {
                    Text = "Fine-tune announcement behavior, checkout calls, and ambient audio for the perfect game experience:",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 10)
                });
            }
        }

        private string GetAdvancedCallDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions
            return argument.Name.ToUpper() switch
            {
                "CCP" => "Call out current player's name before each turn - helps track whose turn it is",
                "E" => "Frequency of dart throw announcements - how often scores are called out during play",
                "ETS" => "Include total score in dart throw announcements for better score tracking",
                "PCC" => "Announce possible checkout scores when players are close to finishing",
                "PCCYO" => "Only announce checkout opportunities for yourself (not for opponents)",
                "A" => "Ambient sound volume level for background atmosphere during games",
                "AAC" => "Play ambient sounds after voice announcements to fill quiet moments",
                "BAV" => "Background audio volume level for music or ambient sounds during play",
                "LPB" => "Enable local playback mode for offline voice announcements",
                "HP" => "Host port for the caller web service - used by other extensions to connect",
                "CRL" => "Enable caller real-life mode for enhanced realism in announcements",
                "CBA" => "Enable announcements for automated bot actions and system events",
                "DEB" => "Enable debug mode for troubleshooting caller issues",
                "CC" => "Enable certificate checking for secure HTTPS connections",
                "WEBDH" => "Disable HTTPS for web caller interface (use HTTP instead)",
                _ => $"Advanced caller setting: {argument.NameHuman}"
            };
        }
    }
}