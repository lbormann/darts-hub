using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.pixelit
{
    /// <summary>
    /// Player interaction animations step for Pixelit guided configuration
    /// </summary>
    public class PixelitPlayerAnimationsStep
    {
        private readonly AppBase pixelitApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onPlayerAnimationsSelected;
        private readonly Action onPlayerAnimationsSkipped;

        public bool ShowPlayerAnimations { get; private set; }

        public PixelitPlayerAnimationsStep(AppBase pixelitApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onPlayerAnimationsSelected, Action onPlayerAnimationsSkipped)
        {
            this.pixelitApp = pixelitApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onPlayerAnimationsSelected = onPlayerAnimationsSelected;
            this.onPlayerAnimationsSkipped = onPlayerAnimationsSkipped;
        }

        public Border CreatePlayerAnimationsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "PlayerAnimationsCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "👥 Player Interaction Animations",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like custom animations when players join or leave the game?",
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
                Content = "👋 Yes, customize player animations",
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
                Content = "❌ No player animations needed",
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
                ShowPlayerAnimations = true;
                ShowPlayerAnimationSettings(content);
                onPlayerAnimationsSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowPlayerAnimations = false;
                onPlayerAnimationsSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Player animation settings (initially hidden)
            var playerAnimationsPanel = new StackPanel { Spacing = 10, IsVisible = false };
            playerAnimationsPanel.Name = "PlayerAnimationsPanel";

            // Player interaction animations: Application start, Player join, Player leave
            var playerAnimationArgs = new[] { "AS", "PJ", "PL" };
            foreach (var argName in playerAnimationArgs)
            {
                var argument = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for player animations
                    var control = PixelitArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetPlayerAnimationDescription, pixelitApp);
                    playerAnimationsPanel.Children.Add(control);
                }
            }

            content.Children.Add(playerAnimationsPanel);
            card.Child = content;
            return card;
        }

        private void ShowPlayerAnimationSettings(StackPanel content)
        {
            var animationsPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "PlayerAnimationsPanel");
            if (animationsPanel != null)
            {
                animationsPanel.IsVisible = true;
            }
        }

        private string GetPlayerAnimationDescription(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "AS" => "Startup animation displayed when the application launches - specify template file name",
                "PJ" => "Welcome animation when a player joins the lobby or game - specify template file name",
                "PL" => "Farewell animation when a player leaves the lobby or game - specify template file name",
                _ => $"Player animation setting: {argument.NameHuman}"
            };
        }
    }
}