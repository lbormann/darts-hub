using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.caller
{
    /// <summary>
    /// Essential Autodarts credentials and media path settings for Caller guided configuration
    /// </summary>
    public class CallerEssentialSettingsStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;

        public CallerEssentialSettingsStep(AppBase callerApp, WizardArgumentsConfig wizardConfig, 
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
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
                Text = "Essential Caller Settings",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            // darts-caller now uses the new Autodarts device-link auth flow,
            // so the email (U) and password (P) inputs are no longer needed
            // and have been removed from this step.
            var essentialArgs = new[] { "B", "M" }; // Board ID, Media Path

            foreach (var argName in essentialArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    Control control;
                    // Use enhanced control for media path
                    if (argName == "M")
                    {
                        control = CallerArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetArgumentDescription, callerApp);
                    }
                    else
                    {
                        control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetArgumentDescription);
                    }
                    
                    content.Children.Add(control);
                }
            }

            card.Child = content;
            return card;
        }

        private string GetArgumentDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions for essential Caller arguments
            return argument.Name.ToUpper() switch
            {
                "B" => "Your Autodarts board ID - this identifies your specific dartboard in the system",
                "M" => "Path to folder containing voice media files for announcements (required for voice calls)",
                _ => $"Caller voice announcement setting: {argument.NameHuman}"
            };
        }
    }
}