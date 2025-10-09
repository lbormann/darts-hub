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
    /// Game and match animations step for Pixelit guided configuration
    /// </summary>
    public class PixelitGameAnimationsStep
    {
        private readonly AppBase pixelitApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onGameAnimationsSelected;
        private readonly Action onGameAnimationsSkipped;
        private bool isProcessing = false; // ⭐ Flag to prevent multiple clicks

        public bool ShowGameAnimations { get; private set; }

        public PixelitGameAnimationsStep(AppBase pixelitApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onGameAnimationsSelected, Action onGameAnimationsSkipped)
        {
            this.pixelitApp = pixelitApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onGameAnimationsSelected = onGameAnimationsSelected;
            this.onGameAnimationsSkipped = onGameAnimationsSkipped;
        }

        public Border CreateGameAnimationsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 193, 7)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "GameAnimationsCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎮 Game & Match Animations",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like custom animations for game events like wins, starts, and player interactions?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
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
                Content = "🎬 Yes, customize animations",
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
                Content = "❌ No custom animations needed",
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
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowGameAnimations = true;
                    ShowGameAnimationSettings(content);
                    
                    // Disable both buttons after selection
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onGameAnimationsSelected?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            noButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowGameAnimations = false;
                    
                    // Disable both buttons after selection
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onGameAnimationsSkipped?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Game animation settings (initially hidden)
            var gameAnimationsPanel = new StackPanel { Spacing = 10, IsVisible = false };
            gameAnimationsPanel.Name = "GameAnimationsPanel";

            // Game event animations: Game start, Match start, Game win, Match win, Bust, High finish
            var gameAnimationArgs = new[] { "GS", "MS", "G", "M", "B", "HF" };
            foreach (var argName in gameAnimationArgs)
            {
                var argument = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for game animations (these are effect parameters)
                    var control = PixelitArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetGameAnimationDescription, pixelitApp);
                    gameAnimationsPanel.Children.Add(control);
                }
            }

            content.Children.Add(gameAnimationsPanel);
            card.Child = content;
            return card;
        }

        private void ShowGameAnimationSettings(StackPanel content)
        {
            var animationsPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "GameAnimationsPanel");
            if (animationsPanel != null && !animationsPanel.IsVisible) // ⭐ Only show if not already visible
            {
                animationsPanel.IsVisible = true;
            }
        }

        private string GetGameAnimationDescription(Argument argument)
        {
            return argument.Name.ToUpper() switch
            {
                "GS" => "Animation displayed when a new game starts - specify template file name",
                "MS" => "Animation displayed when a new match begins - specify template file name",
                "G" => "Celebration animation when a game is won - specify template file name",
                "M" => "Special animation when a match is won - specify template file name", 
                "B" => "Animation shown when a player goes bust - specify template file name",
                "HF" => "High finish celebration animation for impressive scores - specify template file name",
                _ => $"Game animation setting: {argument.NameHuman}"
            };
        }
    }
}