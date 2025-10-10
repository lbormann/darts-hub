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
        private bool isProcessing = false; // Flag to prevent multiple clicks

        // Track saved effects
        private readonly Dictionary<string, string> savedGameWinEffects = new Dictionary<string, string>();

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
                Content = "✅ Yes, show win effects",
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
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowGameWinEffects = true;
                    ShowGameWinSettings(content);
                    
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onGameWinEffectsSelected?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            noButton.Click += (s, e) =>
            {
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowGameWinEffects = false;
                    
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onGameWinEffectsSkipped?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
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
                    // Create enhanced controls with "Use this" button
                    var control = CreateGameWinEffectControlWithUseButton(argument, argName);
                    gameWinPanel.Children.Add(control);
                }
            }

            content.Children.Add(gameWinPanel);
            card.Child = content;
            return card;
        }

        private Control CreateGameWinEffectControlWithUseButton(Argument argument, string argName)
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
                Text = GetGameWinDisplayName(argName),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(titleLabel);

            var descLabel = new TextBlock
            {
                Text = GetGameWinDescription(argument),
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
                    savedGameWinEffects[argName] = currentValue;
                    
                    // Update status
                    statusText.Text = $"✅ Saved: {GetEffectDisplayName(currentValue)}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    
                    // Update button
                    useThisButton.Content = "✅ Saved";
                    useThisButton.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                    
                    System.Diagnostics.Debug.WriteLine($"Saved game win effect for {argName}: {currentValue}");
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

        private string GetGameWinDisplayName(string argName)
        {
            return argName.ToLower() switch
            {
                "g" => "Game Won Effect",
                "m" => "Match Won Effect",
                "gs" => "Game Start Effect",
                "ms" => "Match Start Effect",
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

        private void ShowGameWinSettings(StackPanel content)
        {
            var winPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "GameWinPanel");
            if (winPanel != null && !winPanel.IsVisible)
            {
                winPanel.IsVisible = true;
            }
        }

        private string GetGameWinDescription(Argument argument)
        {
            return argument.Name.ToLower() switch
            {
                "g" => "Effects played when a game is won - select from available WLED effects, colors, and presets with test buttons",
                "m" => "Effects played when a match is won - select from available WLED effects, colors, and presets with test buttons",
                "gs" => "Effects played when a game starts - select from available WLED effects, colors, and presets with test buttons",
                "ms" => "Effects played when a match starts - select from available WLED effects, colors, and presets with test buttons",
                _ => $"Game/match effect: {argument.NameHuman} - configure with enhanced effect controls including test buttons"
            };
        }

        /// <summary>
        /// Applies the saved game win effects to the WLED configuration
        /// </summary>
        public void ApplySavedGameWinEffects()
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLYING SAVED GAME WIN EFFECTS ===");
            System.Diagnostics.Debug.WriteLine($"Saved effects count: {savedGameWinEffects.Count}");

            foreach (var savedEffect in savedGameWinEffects)
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

            System.Diagnostics.Debug.WriteLine($"=== SAVED GAME WIN EFFECTS APPLIED ===");
        }

        /// <summary>
        /// Gets the number of saved effects for summary display
        /// </summary>
        public int GetSavedEffectsCount()
        {
            return savedGameWinEffects.Count;
        }
    }
}