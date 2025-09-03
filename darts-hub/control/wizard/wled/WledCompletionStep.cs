using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Collections.Generic;
using System;

namespace darts_hub.control.wizard.wled
{
    /// <summary>
    /// Completion step for WLED guided configuration
    /// </summary>
    public class WledCompletionStep
    {
        private readonly bool showPlayerSpecificColors;
        private readonly bool showGameWinEffects;
        private readonly bool showScoreEffects;
        private readonly int selectedScoresCount;

        public WledCompletionStep(bool showPlayerSpecificColors, bool showGameWinEffects, 
            bool showScoreEffects, int selectedScoresCount)
        {
            this.showPlayerSpecificColors = showPlayerSpecificColors;
            this.showGameWinEffects = showGameWinEffects;
            this.showScoreEffects = showScoreEffects;
            this.selectedScoresCount = selectedScoresCount;
        }

        public Border CreateCompletionCard()
        {
            var completionCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 40, 167, 69)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 10 };

            content.Children.Add(new TextBlock
            {
                Text = "✅ WLED Configuration Complete!",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            var summary = new List<string>();
            if (showPlayerSpecificColors) summary.Add("• Player-specific colors enabled");
            if (showGameWinEffects) summary.Add("• Game/match win effects enabled");
            if (showScoreEffects) summary.Add($"• Score effects for {selectedScoresCount} scores");
            
            if (summary.Count > 0)
            {
                content.Children.Add(new TextBlock
                {
                    Text = string.Join("\n", summary),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 200)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                });
            }

            completionCard.Child = content;
            return completionCard;
        }
    }
}