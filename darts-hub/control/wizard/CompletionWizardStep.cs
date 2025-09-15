using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Threading.Tasks;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Final wizard step that completes the setup and provides summary
    /// </summary>
    public class CompletionWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;

        public string Title => "Setup Complete!";
        public string Description => "Your darts applications are now configured and ready to use";
        public string IconName => "darts";
        public bool CanSkip => false;

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 30,
                MaxWidth = 600,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Success message
            var successPanel = CreateSuccessSection();
            mainPanel.Children.Add(successPanel);

            // Configuration summary
            var summaryPanel = CreateSummarySection();
            mainPanel.Children.Add(summaryPanel);

            // Next steps
            var nextStepsPanel = CreateNextStepsSection();
            mainPanel.Children.Add(nextStepsPanel);

            // Tips section
            var tipsPanel = CreateTipsSection();
            mainPanel.Children.Add(tipsPanel);

            return mainPanel;
        }

        private Control CreateSuccessSection()
        {
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(51, 40, 167, 69)), // Semi-transparent green
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(30, 25)
            };

            var panel = new StackPanel
            {
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "🎉 Congratulations!",
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Your Darts-Hub setup is now complete and ready for action!",
                FontSize = 18,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 255, 220)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            section.Child = panel;
            return section;
        }

        private Control CreateSummarySection()
        {
            var section = new Expander
            {
                Header = "📋 Configuration Summary",
                IsExpanded = true,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 18
            };

            var panel = new StackPanel 
            { 
                Spacing = 15, 
                Margin = new Avalonia.Thickness(10) 
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"Profile: {profile?.Name ?? "Default"}",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237))
            });

            // Show configured apps
            var appsPanel = new StackPanel { Spacing = 8 };
            
            if (profile?.Apps != null)
            {
                foreach (var appState in profile.Apps.Values)
                {
                    var app = appState.App;
                    var statusColor = appState.TaggedForStart ? 
                        new SolidColorBrush(Color.FromRgb(144, 238, 144)) : // Light green for enabled
                        new SolidColorBrush(Color.FromRgb(169, 169, 169));   // Gray for disabled

                    var status = appState.TaggedForStart ? "✓ Enabled" : "○ Available";
                    
                    var appItem = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10
                    };

                    appItem.Children.Add(new TextBlock
                    {
                        Text = GetAppIcon(app.CustomName),
                        FontSize = 16,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    appItem.Children.Add(new TextBlock
                    {
                        Text = app.CustomName,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    appItem.Children.Add(new TextBlock
                    {
                        Text = status,
                        Foreground = statusColor,
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Avalonia.Thickness(10, 0, 0, 0)
                    });

                    appsPanel.Children.Add(appItem);
                }
            }

            panel.Children.Add(appsPanel);
            section.Content = panel;
            return section;
        }

        private Control CreateNextStepsSection()
        {
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(51, 2, 176, 250)), // Semi-transparent blue
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20)
            };

            var panel = new StackPanel { Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = "🚀 What's Next?",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var stepsList = new StackPanel { Spacing = 8 };

            stepsList.Children.Add(CreateNextStepItem("1.", "Click 'Finish' to complete the setup"));
            stepsList.Children.Add(CreateNextStepItem("2.", "Use the 'Start Profile' button to launch your dart applications"));
            stepsList.Children.Add(CreateNextStepItem("3.", "Fine-tune settings anytime in the application settings panel"));
            stepsList.Children.Add(CreateNextStepItem("4.", "Check the Console tab to monitor your applications"));

            panel.Children.Add(stepsList);
            section.Child = panel;
            return section;
        }

        private Control CreateTipsSection()
        {
            var panel = new StackPanel { Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = "💡 Pro Tips",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0))
            });

            var tipsList = new StackPanel { Spacing = 8 };

            tipsList.Children.Add(CreateTipItem("🎯", "Calibrate your camera position for best dart detection accuracy"));
            tipsList.Children.Add(CreateTipItem("💡", "Test your LED setup with the WLED web interface first"));
            tipsList.Children.Add(CreateTipItem("📱", "Ensure your Pixelit device is on the same network"));
            tipsList.Children.Add(CreateTipItem("🔧", "Visit application settings to customize advanced options"));
            tipsList.Children.Add(CreateTipItem("📋", "Use the Console view to troubleshoot any issues"));

            panel.Children.Add(tipsList);
            return panel;
        }

        private Control CreateNextStepItem(string number, string description)
        {
            var item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            item.Children.Add(new TextBlock
            {
                Text = number,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                VerticalAlignment = VerticalAlignment.Top,
                Width = 20
            });

            item.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top
            });

            return item;
        }

        private Control CreateTipItem(string icon, string tip)
        {
            var item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            item.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 20
            });

            item.Children.Add(new TextBlock
            {
                Text = tip,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Top
            });

            return item;
        }

        private string GetAppIcon(string appName)
        {
            return appName.ToLower() switch
            {
                var name when name.Contains("caller") => "🎯",
                var name when name.Contains("wled") => "💡",
                var name when name.Contains("pixelit") => "📱",
                var name when name.Contains("voice") => "🗣️",
                var name when name.Contains("gif") => "🎬",
                var name when name.Contains("extern") => "🔗",
                _ => "⚙️"
            };
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            // Completion step doesn't need validation
            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            // Final configuration is already applied by previous steps
            // Just ensure the wizard completed flag is set
            configurator.Settings.WizardCompleted = true;
            configurator.SaveSettings();
            
            await Task.CompletedTask;
        }

        public async Task OnStepShown()
        {
            await Task.CompletedTask;
        }

        public async Task OnStepHidden()
        {
            await Task.CompletedTask;
        }

        public async Task ResetStep()
        {
            await Task.CompletedTask;
        }
    }
}