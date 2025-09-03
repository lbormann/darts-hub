using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.wled
{
    /// <summary>
    /// Player-specific colors step for WLED guided configuration
    /// </summary>
    public class WledPlayerColorsStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onPlayerColorsSelected;
        private readonly Action onPlayerColorsSkipped;

        public bool ShowPlayerSpecificColors { get; private set; }

        public WledPlayerColorsStep(AppBase wledApp, WizardArgumentsConfig wizardConfig, 
            Dictionary<string, Control> argumentControls, Action onPlayerColorsSelected, Action onPlayerColorsSkipped)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onPlayerColorsSelected = onPlayerColorsSelected;
            this.onPlayerColorsSkipped = onPlayerColorsSkipped;
        }

        public Border CreatePlayerColorsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 2, 176, 250)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎨 Player-Specific Colors",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like different colors for different players during idle time?",
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
                Content = "✅ Yes, customize player colors",
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
                Content = "❌ No, use default colors",
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
                ShowPlayerSpecificColors = true;
                ShowPlayerColorSettings(content);
                onPlayerColorsSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowPlayerSpecificColors = false;
                onPlayerColorsSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Player color settings (initially hidden)
            var playerColorsPanel = new StackPanel { Spacing = 10, IsVisible = false };
            playerColorsPanel.Name = "PlayerColorsPanel";

            var playerColorArgs = new[] { "IDE2", "IDE3", "IDE4", "IDE5", "IDE6" };
            foreach (var argName in playerColorArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for player color effects (these are effect parameters)
                    var control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetPlayerColorDescription, wledApp);
                    playerColorsPanel.Children.Add(control);
                }
            }

            content.Children.Add(playerColorsPanel);
            card.Child = content;
            return card;
        }

        private void ShowPlayerColorSettings(StackPanel content)
        {
            var colorsPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "PlayerColorsPanel");
            if (colorsPanel != null)
            {
                colorsPanel.IsVisible = true;
            }
        }

        private string GetPlayerColorDescription(Argument argument)
        {
            return argument.Name.ToLower() switch
            {
                "ide2" => "Color effect for Player 2 during idle time - select from available WLED effects and colors",
                "ide3" => "Color effect for Player 3 during idle time - select from available WLED effects and colors",
                "ide4" => "Color effect for Player 4 during idle time - select from available WLED effects and colors",
                "ide5" => "Color effect for Player 5 during idle time - select from available WLED effects and colors",
                "ide6" => "Color effect for Player 6 during idle time - select from available WLED effects and colors",
                _ => $"Player color setting: {argument.NameHuman}"
            };
        }
    }
}