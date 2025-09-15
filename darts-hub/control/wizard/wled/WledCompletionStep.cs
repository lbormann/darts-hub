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
        private readonly bool hasPlayerColors;
        private readonly bool hasGameWinEffects;
        private readonly bool hasBoardStatusEffects;
        private readonly bool hasScoreEffects;
        private readonly int configuredScoresCount;

        public WledCompletionStep(bool hasPlayerColors, bool hasGameWinEffects, 
            bool hasBoardStatusEffects, bool hasScoreEffects, int configuredScoresCount)
        {
            this.hasPlayerColors = hasPlayerColors;
            this.hasGameWinEffects = hasGameWinEffects;
            this.hasBoardStatusEffects = hasBoardStatusEffects;
            this.hasScoreEffects = hasScoreEffects;
            this.configuredScoresCount = configuredScoresCount;
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
            if (hasPlayerColors) summary.Add("• Player-specific colors enabled");
            if (hasGameWinEffects) summary.Add("• Game/match win effects enabled");
            if (hasBoardStatusEffects) summary.Add("• Board status effects enabled");
            if (hasScoreEffects) summary.Add($"• Score effects for {configuredScoresCount} scores");
            
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

            var leftFeatures = new StackPanel { Spacing = 4 };
            leftFeatures.Children.Add(CreateFeatureInfo("🎨 Customizable player colors", hasPlayerColors));
            leftFeatures.Children.Add(CreateFeatureInfo("🏆 Game win celebrations", hasGameWinEffects));
            leftFeatures.Children.Add(CreateFeatureInfo("🎯 Board status indicators", hasBoardStatusEffects));
            leftFeatures.Children.Add(CreateFeatureInfo("📊 Individual score effects", hasScoreEffects));
            
            var rightFeatures = new StackPanel { Spacing = 4 };
            rightFeatures.Children.Add(CreateFeatureInfo("💡 Global brightness control", true));
            rightFeatures.Children.Add(CreateFeatureInfo("⚡ Real-time dart detection", true));
            rightFeatures.Children.Add(CreateFeatureInfo("📍 Score area segments", true));
            rightFeatures.Children.Add(CreateFeatureInfo("🌟 Multiple effect styles", true));

            var featuresPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            featuresPanel.Children.Add(leftFeatures);
            featuresPanel.Children.Add(rightFeatures);

            content.Children.Add(featuresPanel);

            completionCard.Child = content;
            return completionCard;
        }

        private StackPanel CreateFeatureInfo(string text, bool enabled)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            panel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = enabled ? Brushes.White : Brushes.Gray
            });
            return panel;
        }
    }
}