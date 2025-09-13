using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System;

namespace darts_hub.control.wizard.caller
{
    /// <summary>
    /// Completion step for Caller guided configuration
    /// </summary>
    public class CallerCompletionStep
    {
        private readonly bool hasVoiceConfig;
        private readonly bool hasDownloadConfig;
        private readonly bool hasRandomConfig;
        private readonly bool hasCheckoutConfig;
        private readonly bool hasFixedConfig;

        public CallerCompletionStep(bool hasVoiceConfig, bool hasDownloadConfig = false, bool hasRandomConfig = false, 
            bool hasCheckoutConfig = false, bool hasFixedConfig = false)
        {
            this.hasVoiceConfig = hasVoiceConfig;
            this.hasDownloadConfig = hasDownloadConfig;
            this.hasRandomConfig = hasRandomConfig;
            this.hasCheckoutConfig = hasCheckoutConfig;
            this.hasFixedConfig = hasFixedConfig;
        }

        public Border CreateCompletionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 40, 167, 69)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "CallerCompletionCard"
            };

            var content = new StackPanel { Spacing = 20 };

            // Success header
            content.Children.Add(new TextBlock
            {
                Text = "🎉 Darts Caller Configuration Complete!",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Summary of configured features
            var summaryPanel = new StackPanel { Spacing = 10 };

            summaryPanel.Children.Add(new TextBlock
            {
                Text = "✅ Your darts caller is now configured with:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            // Essential settings
            summaryPanel.Children.Add(CreateFeatureItem("🔐 Autodarts credentials configured for authentication"));
            summaryPanel.Children.Add(CreateFeatureItem("🎭 Board ID specified for game connection"));
            summaryPanel.Children.Add(CreateFeatureItem("📁 Media folder path configured for voice files"));
            
            // Voice configuration
            if (hasVoiceConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎤 Voice caller preferences and volume settings optimized"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🗣️ Default voice settings (can be customized later)", isOptional: true));
            }

            // Download configuration
            if (hasDownloadConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("📥 Voice pack download settings configured for automatic updates"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("📦 Manual voice pack management (downloads can be configured later)", isOptional: true));
            }

            // Random configuration
            if (hasRandomConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎲 Random caller selection enabled for variety"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🗣️ Default caller mode (can be customized later)", isOptional: true));
            }

            // Audio configuration  
            if (hasFixedConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🔊 Audio and volume settings customized for optimal experience"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎵 Default audio settings (can be fine-tuned later)", isOptional: true));
            }

            // Checkout configuration
            if (hasCheckoutConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎯 Checkout announcements and finish calls configured"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("📢 Basic calls only (advanced announcements can be added later)", isOptional: true));
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
                Text = "1. Ensure your media folder contains voice announcement files",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "2. Test your Autodarts connection by starting a practice game",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "3. Adjust volume levels to suit your room acoustics",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            if (hasDownloadConfig)
            {
                nextStepsPanel.Children.Add(new TextBlock
                {
                    Text = "4. Monitor automatic voice pack downloads in the configured download folder",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                });
            }
            else
            {
                nextStepsPanel.Children.Add(new TextBlock
                {
                    Text = "4. Download additional voice packs manually if desired for variety",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                });
            }

            content.Children.Add(nextStepsPanel);

            // Feature info
            var featuresPanel = new StackPanel { Spacing = 8 };

            featuresPanel.Children.Add(new TextBlock
            {
                Text = "🎯 Caller Features:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            var featuresGrid = new Grid();
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition());
            featuresGrid.ColumnDefinitions.Add(new ColumnDefinition());
            
            var leftFeatures = new StackPanel { Spacing = 4 };
            leftFeatures.Children.Add(CreateFeatureInfo("🗣️ Multi-language voice support"));
            leftFeatures.Children.Add(CreateFeatureInfo("🎲 Random caller selection"));
            leftFeatures.Children.Add(CreateFeatureInfo("📢 Checkout announcements"));
            leftFeatures.Children.Add(CreateFeatureInfo("📥 Automatic voice pack downloads"));
            
            var rightFeatures = new StackPanel { Spacing = 4 };
            rightFeatures.Children.Add(CreateFeatureInfo("🎵 Ambient sound support"));
            rightFeatures.Children.Add(CreateFeatureInfo("⏪ Score call frequency control"));
            rightFeatures.Children.Add(CreateFeatureInfo("🌐 Web interface integration"));
            rightFeatures.Children.Add(CreateFeatureInfo("🎤 Fixed caller voice selection"));

            Grid.SetColumn(leftFeatures, 0);
            Grid.SetColumn(rightFeatures, 1);
            featuresGrid.Children.Add(leftFeatures);
            featuresGrid.Children.Add(rightFeatures);
            
            featuresPanel.Children.Add(featuresGrid);
            content.Children.Add(featuresPanel);

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

        private Control CreateFeatureInfo(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Avalonia.Thickness(10, 0, 0, 0)
            };
        }
    }
}