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
    /// Random caller configuration step for Caller guided configuration
    /// </summary>
    public class CallerRandomStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onRandomConfigSelected;
        private readonly Action onRandomConfigSkipped;

        public bool ShowRandomConfiguration { get; private set; } = false;

        public CallerRandomStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onRandomConfigSelected, Action onRandomConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onRandomConfigSelected = onRandomConfigSelected;
            this.onRandomConfigSkipped = onRandomConfigSkipped;
        }

        public Border CreateRandomConfigQuestionCard()
        {
            var card = new Border
            {
                Name = "RandomConfigCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 193, 7)),
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
                Text = "🎲",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Random Caller Selection",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to enable random caller selection for variety in voice announcements?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "Random selection will automatically choose different callers and voices to keep your dart games fresh and entertaining.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 240, 200)),
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
                Content = "✅ Configure Random Selection",
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
                Content = "❌ Use Fixed Caller",
                Padding = new Avalonia.Thickness(15, 8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            configureButton.Click += (s, e) =>
            {
                ShowRandomConfiguration = true;
                ShowRandomConfigSettings(content);
                configureButton.IsVisible = false;
                skipButton.IsVisible = false;
                onRandomConfigSelected?.Invoke();
            };

            skipButton.Click += (s, e) =>
            {
                ShowRandomConfiguration = false;
                onRandomConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(configureButton);
            buttonPanel.Children.Add(skipButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        private void ShowRandomConfigSettings(StackPanel content)
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
                Text = "Random Caller Configuration",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Random caller-related arguments
            var randomArgs = new[] { "R", "RL", "RG" }; // Random, Random Language, Random Gender

            foreach (var argName in randomArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    if (argName == "R")
                    {
                        // Enable random selection by default when this step is shown (1 = random selection enabled)
                        argument.Value = "1";
                        argument.IsValueChanged = true;
                    }
                    
                    control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetRandomConfigDescription);
                    content.Children.Add(control);
                }
            }

            // Add explanation
            content.Children.Add(new TextBlock
            {
                Text = "💡 Tip: Random selection works best with multiple voice packs installed. Configure download settings to get more variety!",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 240, 150)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });
        }

        private string GetRandomConfigDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions for random caller-related arguments
            return argument.Name.ToUpper() switch
            {
                "R" => "Enable random caller selection for variety in voice announcements",
                "RL" => "Preferred language for random caller selection (e.g., 'en', 'de', 'fr')",
                "RG" => "Gender preference for random caller selection ('male', 'female', or 'any')",
                _ => $"Random caller setting: {argument.NameHuman}"
            };
        }
    }
}