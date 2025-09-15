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
    /// Fixed caller selection step for Caller guided configuration
    /// </summary>
    public class CallerFixedStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onFixedConfigSelected;
        private readonly Action onFixedConfigSkipped;

        public bool ShowFixedConfiguration { get; private set; } = false;

        public CallerFixedStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onFixedConfigSelected, Action onFixedConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onFixedConfigSelected = onFixedConfigSelected;
            this.onFixedConfigSkipped = onFixedConfigSkipped;
        }

        public Border CreateFixedConfigQuestionCard()
        {
            var card = new Border
            {
                Name = "FixedConfigCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 111, 66, 193)),
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
                Text = "🔊",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Fixed Caller voice",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to configure specific caller voice",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = " ",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 255)),
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
                Content = "✅ Set your favorite caller",
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
                Content = "❌ no need",
                Padding = new Avalonia.Thickness(15, 8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            configureButton.Click += (s, e) =>
            {
                ShowFixedConfiguration = true;
                ShowFixedConfigSettings(content);
                configureButton.IsVisible = false;
                skipButton.IsVisible = false;
                onFixedConfigSelected?.Invoke();
            };

            skipButton.Click += (s, e) =>
            {
                ShowFixedConfiguration = false;
                onFixedConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(configureButton);
            buttonPanel.Children.Add(skipButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        private void ShowFixedConfigSettings(StackPanel content)
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
                Text = "Fixed Caller voice",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Audio and volume-related arguments
            var audioArgs = new[] { "C"}; // Caller, Volume, Ambient, Ambient After Call, Background Audio Volume

            foreach (var argName in audioArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    if (argName == "C")
                    {
                        // For caller selection, use enhanced control
                        control = CallerArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetFixedConfigDescription, callerApp);
                    }
                    else
                    {
                        control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetFixedConfigDescription);
                    }
                    
                    content.Children.Add(control);
                }
            }

            // Add helpful tip
            content.Children.Add(new TextBlock
            {
                Text = "💡 Tip: Over time, you get used to a voice, which you can then set here.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 255)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });
        }

        private string GetFixedConfigDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions for audio and volume-related Caller arguments
            return argument.Name.ToUpper() switch
            {
                "C" => "Select a specific caller voice for announcements (leave empty to use default or random selection)",
                _ => $"Audio setting: {argument.NameHuman}"
            };
        }
    }
}