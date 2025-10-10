using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;

namespace darts_hub.control.wizard.wled
{
    /// <summary>
    /// Essential WLED settings step for guided configuration
    /// </summary>
    public class WledEssentialSettingsStep
    {
        private readonly AppBase wledApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;

        // Track saved effects
        private readonly Dictionary<string, string> savedEssentialEffects = new Dictionary<string, string>();

        public WledEssentialSettingsStep(AppBase wledApp, WizardArgumentsConfig wizardConfig, Dictionary<string, Control> argumentControls)
        {
            this.wledApp = wledApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
        }

        public Border CreateEssentialSettingsCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 45, 45, 48)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8)
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
                Text = "⚙️",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });

            header.Children.Add(new TextBlock
            {
                Text = "Essential Settings",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // Essential arguments only - use enhanced controls for effect parameters
            var essentialArgs = new[] { "WEPS", "IDE" }; // Endpoint, Brightness, and Idle Effect

            foreach (var argName in essentialArgs)
            {
                var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    // Use enhanced control with "Use this" button for IDE (effect parameter), simple for others
                    if (argName == "IDE")
                    {
                        control = CreateEssentialEffectControlWithUseButton(argument, argName);
                    }
                    else
                    {
                        control = WledArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetArgumentDescription);
                    }
                    
                    content.Children.Add(control);
                }
            }

            card.Child = content;
            return card;
        }

        private Control CreateEssentialEffectControlWithUseButton(Argument argument, string argName)
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
                Text = "Default Idle Effect" + (argument.Required ? " *" : ""),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            headerPanel.Children.Add(titleLabel);

            var descLabel = new TextBlock
            {
                Text = GetArgumentDescription(argument),
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
                    savedEssentialEffects[argName] = currentValue;
                    
                    // Update status
                    statusText.Text = $"✅ Saved: {GetEffectDisplayName(currentValue)}";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    
                    // Update button
                    useThisButton.Content = "✅ Saved";
                    useThisButton.Background = new SolidColorBrush(Color.FromRgb(108, 117, 125));
                    
                    System.Diagnostics.Debug.WriteLine($"Saved essential effect for {argName}: {currentValue}");
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

        private string GetEffectDisplayName(string effectValue)
        {
            if (string.IsNullOrEmpty(effectValue)) return "None";
            
            // Extract the first part before the pipe for display
            var parts = effectValue.Split('|');
            return parts.Length > 0 ? parts[0] : effectValue;
        }

        private string GetArgumentDescription(Argument argument)
        {
            // Fallback descriptions for essential WLED arguments
            return argument.Name.ToLower() switch
            {
                "weps" => "IP address and port of your WLED controller device",
                "bri" => "Global brightness level for LED effects (1-255)",
                "ide" => "Default effect shown when no game is active - select from available WLED effects, colors, and presets with test buttons",
                _ => $"WLED configuration setting: {argument.NameHuman}"
            };
        }

        /// <summary>
        /// Applies the saved essential effects to the WLED configuration
        /// </summary>
        public void ApplySavedEssentialEffects()
        {
            System.Diagnostics.Debug.WriteLine($"=== APPLYING SAVED ESSENTIAL EFFECTS ===");
            System.Diagnostics.Debug.WriteLine($"Saved effects count: {savedEssentialEffects.Count}");

            foreach (var savedEffect in savedEssentialEffects)
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

            System.Diagnostics.Debug.WriteLine($"=== SAVED ESSENTIAL EFFECTS APPLIED ===");
        }

        /// <summary>
        /// Gets the number of saved effects for summary display
        /// </summary>
        public int GetSavedEffectsCount()
        {
            return savedEssentialEffects.Count;
        }
    }
}