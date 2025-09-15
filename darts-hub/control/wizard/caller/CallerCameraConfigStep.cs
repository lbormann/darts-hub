using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System;

namespace darts_hub.control.wizard.caller
{
    /// <summary>
    /// Voice and media configuration step for Caller guided configuration
    /// </summary>
    public class CallerCameraConfigStep
    {
        private readonly AppBase callerApp;
        private readonly WizardArgumentsConfig wizardConfig;
        private readonly Dictionary<string, Control> argumentControls;
        private readonly Dictionary<string, string> argumentDescriptions;
        private readonly Action onCameraConfigSelected;
        private readonly Action onCameraConfigSkipped;

        public bool ShowCameraConfiguration { get; private set; }

        public CallerCameraConfigStep(AppBase callerApp, WizardArgumentsConfig wizardConfig,
            Dictionary<string, Control> argumentControls, Dictionary<string, string> argumentDescriptions,
            Action onCameraConfigSelected, Action onCameraConfigSkipped)
        {
            this.callerApp = callerApp;
            this.wizardConfig = wizardConfig;
            this.argumentControls = argumentControls;
            this.argumentDescriptions = argumentDescriptions;
            this.onCameraConfigSelected = onCameraConfigSelected;
            this.onCameraConfigSkipped = onCameraConfigSkipped;
        }

        public Border CreateCameraConfigQuestionCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 70, 130, 180)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 8),
                Name = "CameraConfigCard"
            };

            var content = new StackPanel { Spacing = 15 };

            // Header
            content.Children.Add(new TextBlock
            {
                Text = "🎤 Caller Basics configuration",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "You should configure the main behaviour of the Caller. ",
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
                Content = "✅ Yes, lets go!",
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
                Content = "❌ Use default voice settings",
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
                ShowCameraConfiguration = true;
                ShowCameraConfigSettings(content);
                onCameraConfigSelected?.Invoke();
            };

            noButton.Click += (s, e) =>
            {
                ShowCameraConfiguration = false;
                onCameraConfigSkipped?.Invoke();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            content.Children.Add(buttonPanel);

            // Voice configuration settings (initially hidden)
            var cameraConfigPanel = new StackPanel { Spacing = 10, IsVisible = false };
            cameraConfigPanel.Name = "CameraConfigPanel";

            // Voice configuration arguments: Volume, Caller, Random settings
            var voiceConfigArgs = new[] { "CBA", "CCP", "E", "ETS", "CRL" }; 
            foreach (var argName in voiceConfigArgs)
            {
                var argument = callerApp.Configuration?.Arguments?.FirstOrDefault(a => 
                    a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                
                if (argument != null)
                {
                    // Use enhanced controls for media path parameters
                    Control control;
                    if (argName == "MS") // Shared media path
                    {
                        control = CallerArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, GetVoiceConfigDescription, callerApp);
                    }
                    else
                    {
                        control = CallerArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetVoiceConfigDescription);
                    }
                    cameraConfigPanel.Children.Add(control);
                }
            }

            content.Children.Add(cameraConfigPanel);
            card.Child = content;
            return card;
        }

        private void ShowCameraConfigSettings(StackPanel content)
        {
            var configPanel = content.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "CameraConfigPanel");
            if (configPanel != null)
            {
                configPanel.IsVisible = true;

                // Add separator before settings
                configPanel.Children.Insert(0, new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Avalonia.Thickness(20, 15)
                });

                // Add header
                configPanel.Children.Insert(1, new TextBlock
                {
                    Text = "🎤 Caller basic Settings",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10, 0, 5)
                });

                configPanel.Children.Insert(2, new TextBlock
                {
                    Text = "Here you can configure the basic settings for the caller. Should every throw be called, should the total score be called, bot scores, etc.\r\nRead the descriptions of the settings carefully to find the right settings.",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 10)
                });
            }
        }

        private string GetVoiceConfigDescription(Argument argument)
        {
            // First try to get description from parsed README
            if (argumentDescriptions.TryGetValue(argument.Name, out string parsedDescription) && !string.IsNullOrEmpty(parsedDescription))
            {
                return parsedDescription;
            }

            // Fallback descriptions
            return argument.Name.ToUpper() switch
            {
                _ => $"Voice caller configuration setting: {argument.NameHuman}"
            };
        }
    }
}