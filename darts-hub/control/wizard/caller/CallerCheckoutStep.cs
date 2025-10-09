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
    /// Checkout announcements configuration step for Caller guided configuration
    /// </summary>
    public class CallerCheckoutStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onCheckoutConfigSelected;
        private readonly Action onCheckoutConfigSkipped;
        private bool isProcessing = false; // ⭐ Flag to prevent multiple clicks

        public bool ShowCheckoutConfiguration { get; private set; } = false;

        public CallerCheckoutStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onCheckoutConfigSelected, Action onCheckoutConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onCheckoutConfigSelected = onCheckoutConfigSelected;
            this.onCheckoutConfigSkipped = onCheckoutConfigSkipped;
        }

        public Border CreateCheckoutConfigQuestionCard()
        {
            var card = new Border
            {
                Name = "CheckoutConfigCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 220, 53, 69)),
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
                Text = "🎯",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Checkout Announcements",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to configure checkout announcements and finish call settings?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "Enable announcements for possible checkouts.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 200)),
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
                Content = "✅ Configure Checkouts",
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
                Content = "❌ Basic Calls Only",
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
                    ShowCheckoutConfiguration = true;
                    ShowCheckoutConfigSettings(content);
                    
                    // Disable both buttons after selection
                    configureButton.IsEnabled = false;
                    skipButton.IsEnabled = false;
                    
                    onCheckoutConfigSelected?.Invoke();
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
                    ShowCheckoutConfiguration = false;
                    
                    // Disable both buttons after selection
                    configureButton.IsEnabled = false;
                    skipButton.IsEnabled = false;
                    
                    onCheckoutConfigSkipped?.Invoke();
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

        private void ShowCheckoutConfigSettings(StackPanel content)
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
                Text = "Checkout Configuration",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Checkout-related arguments
            var checkoutArgs = new[] { "PCC", "PCCYO"}; // Possible Checkout Call, Your Only, Call Current Player, Call Bot Actions, Events, Event Total Score

            foreach (var argName in checkoutArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    var control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetCheckoutConfigDescription);
                    content.Children.Add(control);
                }
            }

            // Add helpful tip
            content.Children.Add(new TextBlock
            {
                Text = "💡 Tip: Enable 'Possible Checkout Call' to get strategic announcements when you have finishing opportunities!",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 150)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });
        }

        private string GetCheckoutConfigDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions for checkout-related Caller arguments
            return argument.Name.ToUpper() switch
            {
                "PCC" => "Announce possible checkout combinations when you have a finishing opportunity",
                "PCCYO" => "Only announce checkouts for yourself, not for other players",
                _ => $"Checkout announcement setting: {argument.NameHuman}"
            };
        }
    }
}