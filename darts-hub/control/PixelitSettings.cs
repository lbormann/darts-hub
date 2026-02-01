using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System;
using System.Threading.Tasks;

namespace darts_hub.control
{
    /// <summary>
    /// Pixelit-specific settings and UI components for the new settings content provider
    /// </summary>
    public static class PixelitSettings
    {
        /// <summary>
        /// Checks if a parameter is a Pixelit effect parameter (includes both regular effects and score area effects)
        /// </summary>
        public static bool IsPixelitEffectParameter(Argument param, AppBase? app = null)
        {
            // Only apply to darts-pixelit apps
            if (app?.Name != "darts-pixelit") return false;
            
            // Check for regular effect parameters
            bool isRegularEffect = param.Name.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                                  param.Name.Contains("effects", StringComparison.OrdinalIgnoreCase) ||
                                  (param.NameHuman != null && 
                                   (param.NameHuman.Contains("effect", StringComparison.OrdinalIgnoreCase) || 
                                    param.NameHuman.Contains("effects", StringComparison.OrdinalIgnoreCase)));
            
            // Check for score area effect parameters (for darts-pixelit, these should also use Pixelit controls)
            bool isScoreAreaEffect = WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
            
            return isRegularEffect || isScoreAreaEffect;
        }

        /// <summary>
        /// Checks if a parameter is specifically a score area effect parameter for Pixelit
        /// </summary>
        public static bool IsPixelitScoreAreaEffectParameter(Argument param, AppBase? app = null)
        {
            // Only apply to darts-pixelit apps
            if (app?.Name != "darts-pixelit") return false;
            
            return WledScoreAreaHelper.IsScoreAreaEffectParameter(param);
        }

        /// <summary>
        /// Creates an advanced effect parameter control with mode selection for Pixelit effects
        /// </summary>
        public static Control CreateAdvancedPixelitParameterControl(Argument param, Action? saveCallback = null, AppBase? app = null)
        {
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Check if this is a score area effect parameter for special handling
            bool isScoreAreaEffect = IsPixelitScoreAreaEffectParameter(param, app);

            // Header for score area effects
            if (isScoreAreaEffect)
            {
                var scoreAreaHeader = new TextBlock
                {
                    Text = "🎯 Pixelit Score Area Effect",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 165, 0)), // Orange color
                    Margin = new Thickness(0, 0, 0, 5)
                };

                var scoreAreaInfo = new TextBlock
                {
                    Text = "Configure effects that trigger based on dart scores in specific areas.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                mainPanel.Children.Add(scoreAreaHeader);
                mainPanel.Children.Add(scoreAreaInfo);
            }

            // Container for the input control
            var inputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0)
            };

            System.Diagnostics.Debug.WriteLine($"=== PIXELIT EFFECT PARAMETER PARSING START ===");
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: '{param.Name}'");
            System.Diagnostics.Debug.WriteLine($"Is Score Area Effect: {isScoreAreaEffect}");
            System.Diagnostics.Debug.WriteLine($"MODE: MANUAL");

            var currentInputControl = CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect);
            inputContainer.Child = currentInputControl;

            System.Diagnostics.Debug.WriteLine($"=== PIXELIT INITIALIZATION COMPLETE ===");

            mainPanel.Children.Add(inputContainer);

            return mainPanel;
        }

        /// <summary>
        /// Creates a manual text input for Pixelit effect parameters
        /// </summary>
        private static Control CreateManualPixelitInput(Argument param, Action? saveCallback = null, bool isScoreAreaEffect = false)
        {
            var panel = new StackPanel
            {
                Spacing = 5
            };

            // Add info for score area effects
            if (isScoreAreaEffect)
            {
                var infoText = new TextBlock
                {
                    Text = "💡 For score area effects, use format: \"from-to effect\" (e.g., \"1-20 red\", \"100-180 rainbow\")",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)), // Gold color
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                panel.Children.Add(infoText);
            }

            var textBox = new TextBox
            {
                Text = param.Value ?? "",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = isScoreAreaEffect ? 
                    "Enter score area effect: \"from-to effect\"" : 
                    "Enter Pixelit effect manually..."
            };
            
            textBox.TextChanged += (s, e) =>
            {
                param.Value = textBox.Text;
                param.IsValueChanged = true;
                saveCallback?.Invoke();
            };
            
            panel.Children.Add(textBox);
            return panel;
        }
    }
}