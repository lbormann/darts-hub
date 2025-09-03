using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.pixelit
{
    /// <summary>
    /// Essential Pixelit settings step for guided configuration
    /// </summary>
    public class PixelitEssentialSettingsStep
    {
        private readonly AppBase pixelitApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;

        public PixelitEssentialSettingsStep(AppBase pixelitApp, WizardArgumentsConfig wizardConfig, Dictionary<string, Control> argumentControls)
        {
            this.pixelitApp = pixelitApp;
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

            // Essential arguments - Device endpoint, Templates path, Brightness, and default animation
            var essentialArgs = new[] { "PEPS", "TP", "BRI", "IDE" }; // Endpoint, Templates Path, Brightness, and Idle animation

            foreach (var argName in essentialArgs)
            {
                var argument = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    // Use enhanced control for animation parameters, simple for others
                    if (argName == "IDE")
                    {
                        control = PixelitArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetArgumentDescription, pixelitApp);
                    }
                    else
                    {
                        control = PixelitArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetArgumentDescription);
                    }
                    
                    content.Children.Add(control);
                }
            }

            card.Child = content;
            return card;
        }

        private string GetArgumentDescription(Argument argument)
        {
            // Fallback descriptions for essential Pixelit arguments
            return argument.Name.ToLower() switch
            {
                "peps" => "IP address and port of your Pixelit display controller device",
                "tp" => "Path to the templates directory containing display templates and animations",
                "bri" => "Global brightness level for display effects (1-255)",
                "ide" => "Default animation or template shown when no game is active - specify template file name",
                _ => $"Pixelit configuration setting: {argument.NameHuman}"
            };
        }
    }
}