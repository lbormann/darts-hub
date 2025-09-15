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
                    // Use enhanced control for IDE (effect parameter), simple for others
                    if (argName == "IDE")
                    {
                        control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetArgumentDescription, wledApp);
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

        private string GetArgumentDescription(Argument argument)
        {
            // Fallback descriptions for essential WLED arguments
            return argument.Name.ToLower() switch
            {
                "weps" => "IP address and port of your WLED controller device",
                "bri" => "Global brightness level for LED effects (1-255)",
                "ide" => "Default effect shown when no game is active - use the dropdown to select from available WLED effects",
                _ => $"WLED configuration setting: {argument.NameHuman}"
            };
        }
    }
}