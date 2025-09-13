using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.wled
{
    /// <summary>
    /// Board status configuration step for WLED guided configuration
    /// </summary>
    public class WledBoardStatusStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onBoardStatusConfigSelected;
        private readonly Action onBoardStatusConfigSkipped;

        public bool ShowBoardStatusConfiguration { get; private set; } = false;

        public WledBoardStatusStep(AppBase wledApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls,
            Action onBoardStatusConfigSelected, Action onBoardStatusConfigSkipped)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onBoardStatusConfigSelected = onBoardStatusConfigSelected;
            this.onBoardStatusConfigSkipped = onBoardStatusConfigSkipped;
        }

        public Border CreateBoardStatusQuestionCard()
        {
            var card = new Border
            {
                Name = "BoardStatusCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
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
                Text = "Board Status Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to configure visual effects for board status and game states?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "Configure effects for board connection status, game start/end, waiting for next player, and other board-related events.",
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
                Content = "🎛️ Configure Board Status",
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
                Content = "⏭️ No Board Effects",
                Padding = new Avalonia.Thickness(15, 8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            configureButton.Click += (s, e) =>
            {
                ShowBoardStatusConfiguration = true;
                ShowBoardStatusConfigSettings(content);
                configureButton.IsVisible = false;
                skipButton.IsVisible = false;
                onBoardStatusConfigSelected?.Invoke();
            };

            skipButton.Click += (s, e) =>
            {
                ShowBoardStatusConfiguration = false;
                onBoardStatusConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(configureButton);
            buttonPanel.Children.Add(skipButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        private void ShowBoardStatusConfigSettings(StackPanel content)
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
                Text = "Board Status Configuration",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Board status-related arguments
            var boardStatusArgs = new[] { "CE","BSE", "TOE" }; // Board Connected, Board Disconnected, Game Start, Game End, Next Player Turn, Waiting for Player

            foreach (var argName in boardStatusArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for effect parameters
                    var control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetBoardStatusDescription, wledApp);
                    content.Children.Add(control);
                }
            }

            // Add helpful tip
            content.Children.Add(new TextBlock
            {
                Text = "💡 Tip: Board status effects help you stay informed about game state through visual feedback on your LED strip!",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 255)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });
        }

        private string GetBoardStatusDescription(Argument argument)
        {
            // Descriptions for board status-related WLED arguments
            return argument.Name.ToUpper() switch
            {

                "CE" => "Effect for calibration status.",
                "BSE" => "Effect for Board Stop status.",
                "TOE" => "Effect for Takeout status.",
                _ => $"Board status effect: {argument.NameHuman}"
            };
        }
    }
}