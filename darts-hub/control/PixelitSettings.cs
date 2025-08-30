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

            // Input mode selector - only Manual and Future Mode for Pixelit
            var modeSelector = new ComboBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                PlaceholderText = "Select input mode..."
            };

            var manualItem = new ComboBoxItem { Content = "🖊️ Manual Input", Tag = "manual", Foreground = Brushes.White };
            var futureItem = new ComboBoxItem { Content = "🔮 Future Mode", Tag = "future", Foreground = Brushes.White };

            modeSelector.Items.Add(manualItem);
            modeSelector.Items.Add(futureItem);

            // Container for the input control
            var inputContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 5, 0, 0)
            };

            // Analyze current value to determine mode
            bool isManualMode = true;
            
            System.Diagnostics.Debug.WriteLine($"=== PIXELIT EFFECT PARAMETER PARSING START ===");
            System.Diagnostics.Debug.WriteLine($"Parameter Value: '{param.Value}'");
            System.Diagnostics.Debug.WriteLine($"Parameter Name: '{param.Name}'");
            System.Diagnostics.Debug.WriteLine($"Is Score Area Effect: {isScoreAreaEffect}");

            // Check if this looks like a future mode value (we can define this later)
            if (!string.IsNullOrEmpty(param.Value))
            {
                // For now, everything defaults to manual mode
                // Future: add logic to detect future mode values
                if (param.Value.StartsWith("future:", StringComparison.OrdinalIgnoreCase))
                {
                    modeSelector.SelectedItem = futureItem;
                    isManualMode = false;
                    System.Diagnostics.Debug.WriteLine($"MODE: FUTURE (detected future mode parameter)");
                }
                else
                {
                    modeSelector.SelectedItem = manualItem;
                    isManualMode = true;
                    System.Diagnostics.Debug.WriteLine($"MODE: MANUAL");
                }
            }
            else
            {
                modeSelector.SelectedItem = manualItem;
                isManualMode = true;
                System.Diagnostics.Debug.WriteLine($"MODE: MANUAL (empty value)");
            }

            // Handle mode changes
            modeSelector.SelectionChanged += (s, e) =>
            {
                if (modeSelector.SelectedItem is ComboBoxItem selectedItem)
                {
                    var mode = selectedItem.Tag?.ToString();
                    
                    Control newControl = mode switch
                    {
                        "manual" => CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect),
                        "future" => CreateFuturePixelitInput(param, saveCallback, isScoreAreaEffect),
                        _ => CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect)
                    };
                    
                    inputContainer.Child = newControl;
                }
            };

            // Initialize with correct control based on detected mode
            System.Diagnostics.Debug.WriteLine($"=== PIXELIT INITIALIZATION START ===");

            if (isManualMode)
            {
                System.Diagnostics.Debug.WriteLine($"INITIALIZING: Manual mode");
                var currentInputControl = CreateManualPixelitInput(param, saveCallback, isScoreAreaEffect);
                inputContainer.Child = currentInputControl;
                System.Diagnostics.Debug.WriteLine($"INITIALIZED: Manual text input created");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"INITIALIZING: Future mode");
                var futureInputControl = CreateFuturePixelitInput(param, saveCallback, isScoreAreaEffect);
                inputContainer.Child = futureInputControl;
                System.Diagnostics.Debug.WriteLine($"INITIALIZED: Future mode input created");
            }

            System.Diagnostics.Debug.WriteLine($"=== PIXELIT INITIALIZATION COMPLETE ===");

            mainPanel.Children.Add(modeSelector);
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

        /// <summary>
        /// Creates a future mode input for Pixelit effect parameters (placeholder for future development)
        /// </summary>
        private static Control CreateFuturePixelitInput(Argument param, Action? saveCallback = null, bool isScoreAreaEffect = false)
        {
            var panel = new StackPanel
            {
                Spacing = 10
            };

            // Info text about future mode
            var infoText = new TextBlock
            {
                Text = isScoreAreaEffect ? 
                    "🔮 Future Mode - Advanced Pixelit Score Area Controls" :
                    "🔮 Future Mode - Advanced Pixelit Controls",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 255)),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var descriptionText = new TextBlock
            {
                Text = isScoreAreaEffect ?
                    "This mode will provide advanced Pixelit score area controls in a future version.\n" +
                    "For now, you can enter score area effects manually with the 'future:' prefix." :
                    "This mode will provide advanced Pixelit-specific controls in a future version.\n" +
                    "For now, you can enter values manually with the 'future:' prefix.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Temporary input for future mode values
            var textBox = new TextBox
            {
                Text = param.Value ?? "future:",
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255)),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(8),
                CornerRadius = new CornerRadius(3),
                FontSize = 13,
                Watermark = isScoreAreaEffect ?
                    "future:score_area_effect_here" :
                    "future:your_pixelit_effect_here"
            };

            textBox.TextChanged += (s, e) =>
            {
                param.Value = textBox.Text;
                param.IsValueChanged = true;
                saveCallback?.Invoke();
            };

            // Placeholder for future controls
            var placeholderPanel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 100, 200, 255)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var placeholderText = new TextBlock
            {
                Text = isScoreAreaEffect ?
                    "🚧 Future Score Area Controls Coming Soon:\n" +
                    "• Score range selector (from-to)\n" +
                    "• Pixelit animation library\n" +
                    "• Animation speed controls\n" +
                    "• Color palette selection\n" +
                    "• Score-triggered effects builder\n" +
                    "• Real-time preview" :
                    "🚧 Future Controls Coming Soon:\n" +
                    "• Pixelit animation library\n" +
                    "• Animation speed controls\n" +
                    "• Color palette selection\n" +
                    "• Custom effect builder\n" +
                    "• Real-time preview",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 220, 255)),
                TextWrapping = TextWrapping.Wrap
            };

            placeholderPanel.Child = placeholderText;

            panel.Children.Add(infoText);
            panel.Children.Add(descriptionText);
            panel.Children.Add(textBox);
            panel.Children.Add(placeholderPanel);

            return panel;
        }
    }
}