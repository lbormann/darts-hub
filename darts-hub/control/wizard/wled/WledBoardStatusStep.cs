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
    /// Board status configuration step for WLED guided configuration
    /// </summary>
    public class WledBoardStatusStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onBoardStatusConfigSelected;
        private readonly Action onBoardStatusConfigSkipped;
        private bool isProcessing = false; // Flag to prevent multiple clicks

        // Track saved effects
        private readonly Dictionary<string, string> savedBoardStatusEffects = new Dictionary<string, string>();

        public bool ShowBoardStatusConfiguration { get; private set; } = false;

        public WledBoardStatusStep(AppBase wledApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls,
            Action onBoardStatusConfigSelected, Action onBoardStatusConfigSkipped)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onBoardStatusConfigSelected = onBoardStatusConfigSelected;
            this.onBoardStatusConfigSkipped = onBoardStatusConfigSkipped;
        }

        public Border CreateBoardStatusQuestionCard()
        {
            var card = new Border
            {
                Name = "BoardStatusCard",
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 10)
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            header.Children.Add(new TextBlock
            {
                Text = "🎯",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Board Status Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Question
            content.Children.Add(new TextBlock
            {
                Text = "Do you want to configure visual effects for board status and game states?",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });

            content.Children.Add(new TextBlock
            {
                Text = "Configure effects for board connection status, game start/end, waiting for next player, and other board-related events.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 255)),
                TextWrapping = TextWrapping.Wrap
            });

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };

            var configureButton = new Button
            {
                Content = "✅ Configure Board Status",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var skipButton = new Button
            {
                Content = "❌ No Board Effects",
                Padding = new Avalonia.Thickness(15, 8),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125))
            };

            configureButton.Click += (s, e) =>
            {
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowBoardStatusConfiguration = true;
                    ShowBoardStatusConfigSettings(content);
                    
                    configureButton.IsVisible = false;
                    skipButton.IsVisible = false;
                    
                    onBoardStatusConfigSelected?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            skipButton.Click += (s, e) =>
            {
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowBoardStatusConfiguration = false;
                    onBoardStatusConfigSkipped?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            buttonPanel.Children.Add(configureButton);
            buttonPanel.Children.Add(skipButton);
            content.Children.Add(buttonPanel);

            card.Child = content;
            return card;
        }

        private void ShowBoardStatusConfigSettings(StackPanel content)
        {
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Opacity = 0.3,
                Margin = new Avalonia.Thickness(0, 15)
            };
            content.Children.Add(separator);

            var settingsLabel = new TextBlock
            {
                Text = "Board Status Configuration",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            };
            content.Children.Add(settingsLabel);

            // Board status-related arguments
            var boardStatusArgs = new[] { "CE","BSE", "TOE" }; // Board Connected, Board Disconnected, Game Start, Game End, Next Player Turn, Waiting for Player

            foreach (var argName in boardStatusArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Create enhanced controls with "Use this" button
                    var control = CreateBoardStatusEffectControlWithUseButton(argument, argName);
                    content.Children.Add(control);
                }
            }

            // Add helpful tip
            content.Children.Add(new TextBlock
            {
                Text = "💡 Tip: Board status effects help you stay informed about game state through visual feedback on your LED strip!",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 255)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });
        }

        private Control CreateBoardStatusEffectControlWithUseButton(Argument argument, string argName)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 10 };

            // Header
            var headerPanel = new StackPanel { Spacing = 5 };

            var titleLabel = new TextBlock
            {
                Text = GetBoardStatusDisplayName(argName),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(titleLabel);

            var descLabel = new TextBlock
            {
                Text = GetBoardStatusDescription(argument),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            };
            headerPanel.Children.Add(descLabel);

            content.Children.Add(headerPanel);

            // Effect selection panel
            var selectionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            // Create enhanced effect control with auto-save callback
            var effectControl = WledSettings.CreateAdvancedEffectParameterControl(argument, 
                () => { 
                    argument.IsValueChanged = true; 
                }, wledApp);
            selectionPanel.Children.Add(effectControl);

            // "Use this" button
            var useThisButton = new Button
            {
                Content = "✅ Use this",
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(3),
                Padding = new Avalonia.Thickness(15, 8),
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Top
            };

            // Status indicator
            var statusText = new TextBlock
            {
                Text = "💡 Configure effect and click 'Use this' to save",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(10, 0, 0, 0)
            };

            // Check if there's already a configured effect and update status
            if (!string.IsNullOrEmpty(argument.Value))
            {
                statusText.Text = $"💡 Current: {GetEffectDisplayName(argument.Value)} - Click 'Use this' to save";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 255));
            }

            useThisButton.Click += (s, e) =>
            {
                if (useThisButton.Tag?.ToString() == "processing") return;
                useThisButton.Tag = "processing";
                useThisButton.IsEnabled = false;

                try
                {
                    var currentValue = argument.Value;
                    
                    if (string.IsNullOrEmpty(currentValue))
                    {
                        statusText.Text = "⚠️ Please configure an effect first";
                        statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                        return;
                    }
                    
                    // Save the current value
                    savedBoardStatusEffects[argName] = currentValue;
                    
                    // Update status
                    statusText.Text = $"✅ Saved: {GetEffectDisplayName(currentValue)}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    
                    // Update button
                    useThisButton.Content = "✅ Saved";
                    useThisButton.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                    
                    System.Diagnostics.Debug.WriteLine($"Saved board status effect for {argName}: {currentValue}");
                }
                finally
                {
                    useThisButton.Tag = null;
                    useThisButton.IsEnabled = true;
                }
            };

            selectionPanel.Children.Add(useThisButton);
            selectionPanel.Children.Add(statusText);

            content.Children.Add(selectionPanel);
            container.Child = content;

            return container;
        }

        private string GetBoardStatusDisplayName(string argName)
        {
            return argName.ToUpper() switch
            {
                "CE" => "Calibration Effect",
                "BSE" => "Board Stop Effect",
                "TOE" => "Takeout Effect",
                _ => $"{argName} Effect"
            };
        }

        private string GetEffectDisplayName(string effectValue)
        {
            if (string.IsNullOrEmpty(effectValue)) return "None";
            
            // Extract the first part before the pipe for display
            var parts = effectValue.Split('|');
            return parts.Length > 0 ? parts[0] : effectValue;
        }

        private string GetBoardStatusDescription(Argument argument)
        {
            // Descriptions for board status-related WLED arguments
            return argument.Name.ToUpper() switch
            {

                "CE" => "Effect for calibration status - select from available WLED effects, colors, and presets with test buttons",
                "BSE" => "Effect for Board Stop status - select from available WLED effects, colors, and presets with test buttons",
                "TOE" => "Effect for Takeout status - select from available WLED effects, colors, and presets with test buttons",
                _ => $"Board status effect: {argument.NameHuman} - configure with enhanced effect controls including test buttons"
            };
        }

        /// <summary>
        /// Applies the saved board status effects to the WLED configuration
        /// </summary>
        public void ApplySavedBoardStatusEffects()
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLYING SAVED BOARD STATUS EFFECTS ===");
            System.Diagnostics.Debug.WriteLine($"Saved effects count: {savedBoardStatusEffects.Count}");

            foreach (var savedEffect in savedBoardStatusEffects)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(savedEffect.Key, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    argument.Value = savedEffect.Value;
                    argument.IsValueChanged = true;
                    System.Diagnostics.Debug.WriteLine($"Applied saved effect for {savedEffect.Key}: {savedEffect.Value}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"=== SAVED BOARD STATUS EFFECTS APPLIED ===");
        }

        /// <summary>
        /// Gets the number of saved effects for summary display
        /// </summary>
        public int GetSavedEffectsCount()
        {
            return savedBoardStatusEffects.Count;
        }
    }
}