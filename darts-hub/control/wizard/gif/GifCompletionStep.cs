using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System;

namespace darts_hub.control.wizard.gif
{
    /// <summary>
    /// Completion step for GIF guided configuration
    /// </summary>
    public class GifCompletionStep
    {
        private readonly bool hasDisplayMode;
        private readonly bool hasMediaConfig;
        private readonly string displayMode;

        public GifCompletionStep(bool hasDisplayMode, bool hasMediaConfig, string displayMode = "windowed")
        {
            this.hasDisplayMode = hasDisplayMode;
            this.hasMediaConfig = hasMediaConfig;
            this.displayMode = displayMode;
        }

        public Border CreateCompletionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 40, 167, 69)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "GifCompletionCard"
            };

            var content = new StackPanel { Spacing = 20 };

            // Success header
            content.Children.Add(new TextBlock
            {
                Text = "🎉 GIF Display Configuration Complete!",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Summary of configured features
            var summaryPanel = new StackPanel { Spacing = 10 };

            summaryPanel.Children.Add(new TextBlock
            {
                Text = "✅ Your GIF display is now configured with:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            // Essential settings
            summaryPanel.Children.Add(CreateFeatureItem("🔗 Connection to darts-caller service configured"));
            summaryPanel.Children.Add(CreateFeatureItem("📁 Media folder path specified"));
            
            // Display mode
            if (hasDisplayMode)
            {
                var displayModeText = displayMode == "web" ? 
                    "🌐 Web-based display mode enabled for remote viewing" :
                    "🖼️ Windowed display mode configured";
                summaryPanel.Children.Add(CreateFeatureItem(displayModeText));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🖥️ Default display mode (can be customized later)", isOptional: true));
            }

            // Media configuration
            if (hasMediaConfig)
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎬 Advanced media playback settings configured"));
            }
            else
            {
                summaryPanel.Children.Add(CreateFeatureItem("🎨 Default media settings (can be customized later)", isOptional: true));
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
                Text = "1. Make sure your media folder contains GIFs, images, and videos",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "2. Verify darts-caller service is running and accessible",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "3. Test the display by starting a dart game",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            if (displayMode == "web")
            {
                nextStepsPanel.Children.Add(new TextBlock
                {
                    Text = "4. Access web interface at http://localhost:[PORT] for remote viewing",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
                });
            }

            nextStepsPanel.Children.Add(new TextBlock
            {
                Text = "5. You can always modify these settings later in the configuration panel",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            content.Children.Add(nextStepsPanel);

            // Supported formats info
            var formatsPanel = new StackPanel { Spacing = 8 };

            formatsPanel.Children.Add(new TextBlock
            {
                Text = "📎 Supported Media Formats:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            var formatsGrid = new Grid();
            formatsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            formatsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            
            var leftFormats = new StackPanel { Spacing = 4 };
            leftFormats.Children.Add(CreateFormatItem("🖼️ Images: JPG, PNG, BMP, TIFF"));
            leftFormats.Children.Add(CreateFormatItem("🎬 Videos: MP4, AVI, MOV, WMV"));
            
            var rightFormats = new StackPanel { Spacing = 4 };
            rightFormats.Children.Add(CreateFormatItem("🎭 Animations: GIF, WEBP"));
            rightFormats.Children.Add(CreateFormatItem("🔊 Audio: Support in video files"));

            Grid.SetColumn(leftFormats, 0);
            Grid.SetColumn(rightFormats, 1);
            formatsGrid.Children.Add(leftFormats);
            formatsGrid.Children.Add(rightFormats);
            
            formatsPanel.Children.Add(formatsGrid);
            content.Children.Add(formatsPanel);

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

        private Control CreateFormatItem(string text)
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