using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System;

namespace darts_hub.control.wizard.pixelit
{
    /// <summary>
    /// Completion step for Pixelit guided configuration
    /// </summary>
    public class PixelitCompletionStep
    {
        private readonly bool hasGameAnimations;
        private readonly bool hasPlayerAnimations;

        public PixelitCompletionStep(bool hasGameAnimations, bool hasPlayerAnimations)
        {
            this.hasGameAnimations = hasGameAnimations;
            this.hasPlayerAnimations = hasPlayerAnimations;
        }

        public Border CreateCompletionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 40, 167, 69)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "PixelitCompletionCard"
            };

            var content = new StackPanel { Spacing = 20 };

            // Success header
            content.Children.Add(new TextBlock
            {
                Text = "🎉 Pixelit Configuration Complete!",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Summary of configured features
            var summaryPanel = new StackPanel { Spacing = 10 };

            summaryPanel.Children.Add(new TextBlock
            {
                Text = "✅ Your Pixelit display is now configured with:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            // Essential settings
            summaryPanel.Children.Add(CreateFeatureItem("📱 Device connection and basic settings configured"));
            summaryPanel.Children.Add(CreateFeatureItem("📁 Templates directory path set"));
            summaryPanel.Children.Add(CreateFeatureItem("💡 Display brightness configured"));
            summaryPanel.Children.Add(CreateFeatureItem("⏸️ Default idle animation specified"));

            // Optional features
            if (hasGameAnimations)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎮 Custom game and match animations enabled"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎮 Default game animations (can be customized later)", isOptional: true));
            }

            if (hasPlayerAnimations)
            {
                summaryPanel.Children.Add(CreateFeatureItem("👥 Custom player interaction animations enabled"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("👥 Default player animations (can be customized later)", isOptional: true));
            }

            content.Children.Add(summaryPanel);

            // Next steps info
            var nextStepsPanel = new StackPanel { Spacing = 10 };

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "📋 Next Steps:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "1. Make sure your Pixelit device is connected and accessible",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "2. Verify your templates directory contains the animation files you specified",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "3. Test the connection and animations before starting games",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "4. You can always modify these settings later in the configuration panel",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            content.Children.Add(nextStepsPanel);

            card.Child = content;
            return card;
        }

        private Control CreateFeatureItem(string text, bool isOptional = false)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Avalonia.Thickness(10, 0, 0, 0)
            };

            var bullet = new TextBlock
            {
                Text = "•",
                FontSize = 14,
                Foreground = isOptional ? 
                    new SolidColorBrush(Color.FromRgb(180, 180, 180)) : 
                    new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                VerticalAlignment = VerticalAlignment.Center
            };

            var itemText = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = isOptional ? 
                    new SolidColorBrush(Color.FromRgb(180, 180, 180)) : 
                    new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(bullet);
            panel.Children.Add(itemText);

            return panel;
        }
    }
}