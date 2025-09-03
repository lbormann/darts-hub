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
    /// Game and match win effects step for WLED guided configuration
    /// </summary>
    public class WledGameWinEffectsStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onGameWinEffectsSelected;
        private readonly Action onGameWinEffectsSkipped;

        public bool ShowGameWinEffects { get; private set; }

        public WledGameWinEffectsStep(AppBase wledApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Action onGameWinEffectsSelected, Action onGameWinEffectsSkipped)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onGameWinEffectsSelected = onGameWinEffectsSelected;
            this.onGameWinEffectsSkipped = onGameWinEffectsSkipped;
        }

        public Border CreateGameWinEffectsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 193, 7)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "GameWinCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🏆 Game & Match Win Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like special effects when games or matches are won?",
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
                Content = "🎉 Yes, show win effects",
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
                Content = "❌ No win effects needed",
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
                ShowGameWinEffects = true;
                ShowGameWinSettings(content);
                onGameWinEffectsSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowGameWinEffects = false;
                onGameWinEffectsSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Game win settings (initially hidden)
            var gameWinPanel = new StackPanel { Spacing = 10, IsVisible = false };
            gameWinPanel.Name = "GameWinPanel";

            var gameWinArgs = new[] { "G", "M", "GS", "MS" }; // Game win, Match win, Game start, Match start
            foreach (var argName in gameWinArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for game win effects (these are effect parameters)
                    var control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetGameWinDescription, wledApp);
                    gameWinPanel.Children.Add(control);
                }
            }

            content.Children.Add(gameWinPanel);
            card.Child = content;
            return card;
        }

        private void ShowGameWinSettings(StackPanel content)
        {
            var winPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "GameWinPanel");
            if (winPanel != null)
            {
                winPanel.IsVisible = true;
            }
        }

        private string GetGameWinDescription(Argument argument)
        {
            return argument.Name.ToLower() switch
            {
                "g" => "Effects played when a game is won - select from available WLED effects and colors",
                "m" => "Effects played when a match is won - select from available WLED effects and colors",
                "gs" => "Effects played when a game starts - select from available WLED effects and colors",
                "ms" => "Effects played when a match starts - select from available WLED effects and colors",
                _ => $"Game/match effect: {argument.NameHuman}"
            };
        }
    }
}