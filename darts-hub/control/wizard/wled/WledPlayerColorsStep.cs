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
    /// Player-specific colors step for WLED guided configuration
    /// </summary>
    public class WledPlayerColorsStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Action onPlayerColorsSelected;
        private readonly Action onPlayerColorsSkipped;
        private bool isProcessing = false; // ⭐ Flag to prevent multiple clicks

        // Track saved color effects
        private readonly Dictionary<string, string> savedPlayerColors = new Dictionary<string, string>();

        public bool ShowPlayerSpecificColors { get; private set; }

        public WledPlayerColorsStep(AppBase wledApp, WizardArgumentsConfig wizardConfig, 
            Dictionary<string, Control> argumentControls, Action onPlayerColorsSelected, Action onPlayerColorsSkipped)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.onPlayerColorsSelected = onPlayerColorsSelected;
            this.onPlayerColorsSkipped = onPlayerColorsSkipped;
        }

        public Border CreatePlayerColorsQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 2, 176, 250)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎨 Player-Specific Colors",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Would you like different colors for different players during idle time?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
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
                Content = "✅ Yes, customize player colors",
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
                Content = "❌ No, use default colors",
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
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowPlayerSpecificColors = true;
                    ShowPlayerColorSettings(content);
                    
                    // Disable both buttons after selection
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onPlayerColorsSelected?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            noButton.Click += (s, e) =>
            {
                // ⭐ Prevent multiple clicks
                if (isProcessing) return;
                isProcessing = true;
                
                try
                {
                    ShowPlayerSpecificColors = false;
                    
                    // Disable both buttons after selection
                    yesButton.IsEnabled = false;
                    noButton.IsEnabled = false;
                    
                    onPlayerColorsSkipped?.Invoke();
                }
                finally
                {
                    isProcessing = false;
                }
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Player color settings (initially hidden)
            var playerColorsPanel = new StackPanel { Spacing = 15, IsVisible = false };
            playerColorsPanel.Name = "PlayerColorsPanel";

            var playerColorArgs = new[] { "IDE2", "IDE3", "IDE4", "IDE5", "IDE6" };
            foreach (var argName in playerColorArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Create enhanced controls with "Use this" button
                    var control = CreatePlayerColorControlWithUseButton(argument, argName);
                    playerColorsPanel.Children.Add(control);
                }
            }

            content.Children.Add(playerColorsPanel);
            card.Child = content;
            return card;
        }

        private Control CreatePlayerColorControlWithUseButton(Argument argument, string argName)
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
                Text = GetPlayerDisplayName(argName),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(titleLabel);

            var descLabel = new TextBlock
            {
                Text = GetPlayerColorDescription(argument),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            };
            headerPanel.Children.Add(descLabel);

            content.Children.Add(headerPanel);

            // Color selection panel
            var selectionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            // Create color dropdown with auto-save callback to ensure the parameter is always updated
            var colorDropdown = WledSettings.CreateColorEffectsDropdown(argument, 
                () => { 
                    // Auto-save callback - this ensures the parameter is updated whenever selection changes
                    argument.IsValueChanged = true; 
                }, wledApp);
            selectionPanel.Children.Add(colorDropdown);

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
                Text = "💡 Select a color and click 'Use this' to save",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(10, 0, 0, 0)
            };

            // CRITICAL FIX: Check if there's already a preselected color and update status
            if (!string.IsNullOrEmpty(argument.Value))
            {
                statusText.Text = $"💡 Current: {GetColorDisplayName(argument.Value)} - Click 'Use this' to save";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 255));
            }

            useThisButton.Click += (s, e) =>
            {
                // Prevent multiple clicks
                if (useThisButton.Tag?.ToString() == "processing") return;
                useThisButton.Tag = "processing";
                useThisButton.IsEnabled = false;

                try
                {
                    var currentValue = argument.Value;
                    
                    // CRITICAL FIX: Also check if the argument has a valid value even if it appears empty
                    // This can happen when the dropdown preselects a color but doesn't trigger the save
                    if (string.IsNullOrEmpty(currentValue))
                    {
                        // Try to get the currently selected value from the color dropdown
                        // The color dropdown should have set the argument.Value during initialization
                        System.Diagnostics.Debug.WriteLine($"WARNING: Player color argument {argName} is empty, checking dropdown selection...");
                        
                        // Check if a color was preselected in the dropdown but not saved to the argument
                        // This should not happen with the fixed color dropdown, but let's be safe
                        statusText.Text = "⚠️ Please select a color first or check the dropdown selection";
                        statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                        return;
                    }
                    
                    // Save the current value
                    savedPlayerColors[argName] = currentValue;
                    
                    // Update status
                    statusText.Text = $"✅ Saved: {GetColorDisplayName(currentValue)}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    
                    // Update button
                    useThisButton.Content = "✅ Saved";
                    useThisButton.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                    
                    System.Diagnostics.Debug.WriteLine($"Saved player color for {argName}: {currentValue}");
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

        private string GetPlayerDisplayName(string argName)
        {
            return argName.ToLower() switch
            {
                "ide2" => "Player 2 Idle Color",
                "ide3" => "Player 3 Idle Color",
                "ide4" => "Player 4 Idle Color",
                "ide5" => "Player 5 Idle Color",
                "ide6" => "Player 6 Idle Color",
                _ => $"{argName} Color"
            };
        }

        private string GetColorDisplayName(string colorValue)
        {
            if (string.IsNullOrEmpty(colorValue)) return "None";
            
            // Remove "solid|" prefix if present
            if (colorValue.StartsWith("solid|", StringComparison.OrdinalIgnoreCase))
            {
                return colorValue.Substring(6);
            }
            
            return colorValue;
        }

        private void ShowPlayerColorSettings(StackPanel content)
        {
            var colorsPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "PlayerColorsPanel");
            if (colorsPanel != null && !colorsPanel.IsVisible) // ⭐ Only show if not already visible
            {
                colorsPanel.IsVisible = true;
            }
        }

        private string GetPlayerColorDescription(Argument argument)
        {
            return argument.Name.ToLower() switch
            {
                "ide2" => "Color effect for Player 2 during idle time - select from available WLED effects and colors",
                "ide3" => "Color effect for Player 3 during idle time - select from available WLED effects and colors",
                "ide4" => "Color effect for Player 4 during idle time - select from available WLED effects and colors",
                "ide5" => "Color effect for Player 5 during idle time - select from available WLED effects and colors",
                "ide6" => "Color effect for Player 6 during idle time - select from available WLED effects and colors",
                _ => $"Player color setting: {argument.NameHuman}"
            };
        }

        /// <summary>
        /// Applies the saved player colors to the WLED configuration
        /// Called at the end of the wizard to transfer only the explicitly saved effects
        /// </summary>
        public void ApplySavedPlayerColors()
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLYING SAVED PLAYER COLORS ===");
            System.Diagnostics.Debug.WriteLine($"Saved colors count: {savedPlayerColors.Count}");

            foreach (var savedColor in savedPlayerColors)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(savedColor.Key, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    argument.Value = savedColor.Value;
                    argument.IsValueChanged = true;
                    System.Diagnostics.Debug.WriteLine($"Applied saved color for {savedColor.Key}: {savedColor.Value}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"WARNING: Could not find argument for {savedColor.Key}");
                }
            }

            // Clear values for player colors that were not explicitly saved
            var playerColorArgs = new[] { "IDE2", "IDE3", "IDE4", "IDE5", "IDE6" };
            foreach (var argName in playerColorArgs)
            {
                if (!savedPlayerColors.ContainsKey(argName))
                {
                    var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                        a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null && !string.IsNullOrEmpty(argument.Value))
                    {
                        argument.Value = null; // Clear unsaved colors
                        argument.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"Cleared unsaved color for {argName}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"=== SAVED PLAYER COLORS APPLIED ===");
        }

        /// <summary>
        /// Gets the number of saved player colors for summary display
        /// </summary>
        public int GetSavedColorsCount()
        {
            return savedPlayerColors.Count;
        }

        /// <summary>
        /// Gets a summary of saved player colors for display
        /// </summary>
        public string GetSavedColorsSummary()
        {
            if (savedPlayerColors.Count == 0)
            {
                return "No player colors saved";
            }

            var summary = string.Join(", ", savedPlayerColors.Select(kv => 
                $"{GetPlayerDisplayName(kv.Key)}: {GetColorDisplayName(kv.Value)}"));
            
            return $"Saved {savedPlayerColors.Count} player colors: {summary}";
        }
    }
}