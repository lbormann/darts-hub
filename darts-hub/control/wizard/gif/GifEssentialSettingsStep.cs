using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.gif
{
    /// <summary>
    /// Essential GIF settings step for guided configuration
    /// </summary>
    public class GifEssentialSettingsStep
    {
        private readonly AppBase gifApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;

        public GifEssentialSettingsStep(AppBase gifApp, WizardArgumentsConfig wizardConfig, Dictionary<string, Control> argumentControls)
        {
            this.gifApp = gifApp;
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

            // Essential arguments - Caller connection, Media path, Web settings
            var essentialArgs = new[] { "CON", "MP", "WEB", "WEBP" }; // Connection, Media Path, Web enable, Web Port

            foreach (var argName in essentialArgs)
            {
                var argument = gifApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    // Use enhanced control for media path parameters
                    if (argName == "MP")
                    {
                        control = GifArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetArgumentDescription, gifApp);
                    }
                    else
                    {
                        control = GifArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetArgumentDescription);
                    }
                    
                    content.Children.Add(control);
                }
            }

            card.Child = content;
            return card;
        }

        private string GetArgumentDescription(Argument argument)
        {
            // Fallback descriptions for essential GIF arguments
            return argument.Name.ToLower() switch
            {
                "con" => "Connection URL to darts-caller service for receiving game event notifications",
                "mp" => "Path to folder containing GIF files, images, and videos to display during games and events",
                "web" => "Enable web-based display interface for remote viewing of GIFs and media content",
                "webp" => "Port number for the web-based display interface (default: 8090)",
                _ => $"GIF display configuration setting: {argument.NameHuman}"
            };
        }
    }
}