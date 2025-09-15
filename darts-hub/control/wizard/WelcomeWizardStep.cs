using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Threading.Tasks;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Welcome step that introduces the user to the setup wizard
    /// </summary>
    public class WelcomeWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;

        public string Title => "Welcome to Darts-Hub Setup";
        public string Description => "Let's configure your Autodarts extensions to get you started quickly";
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
                Spacing = 25,
                MaxWidth = 600,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Welcome message
            var welcomePanel = new StackPanel { Spacing = 15 };
            
            welcomePanel.Children.Add(new TextBlock
            {
                Text = "🎯 Welcome to Darts-Hub!",
                FontSize = 28,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            welcomePanel.Children.Add(new TextBlock
            {
                Text = "This setup wizard will help you configure your darts applications for the best experience. We'll guide you through the essential settings to get you up and running quickly.",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            mainPanel.Children.Add(welcomePanel);

            // What we'll configure section
            var configSection = CreateConfigurationSection();
            mainPanel.Children.Add(configSection);

            // Profile information
            var profilePanel = CreateProfileSection();
            mainPanel.Children.Add(profilePanel);

            // Getting started info
            var infoPanel = CreateInfoSection();
            mainPanel.Children.Add(infoPanel);

            return mainPanel;
        }

        private Control CreateConfigurationSection()
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
                Text = "What we'll configure:",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var configItems = new StackPanel { Spacing = 8 };
            
            // Check which apps are available in the profile
            var hasWled = HasAppInProfile("wled");
            var hasPixelit = HasAppInProfile("pixelit");
            var hasVoice = HasAppInProfile("voice");
            var hasExtern = HasAppInProfile("extern");

            configItems.Children.Add(CreateConfigItem("🎯", "Darts-Caller", "Customizeable Caller with almost no limits."));
            
            if (hasWled)
                configItems.Children.Add(CreateConfigItem("💡", "Darts-WLED Integration", "LED strip control and effects"));
            
            if (hasPixelit)
                configItems.Children.Add(CreateConfigItem("📱", "Darts-Pixelit Display", "Smart Pixeldisplay for scores and animations"));
            
            if (hasVoice)
                configItems.Children.Add(CreateConfigItem("🗣️", "Darts-Voice", "Controle your Autodarts with Voice commands"));
                
            if (hasExtern)
                configItems.Children.Add(CreateConfigItem("🔗", "External Integration", "Connect with external services"));

            configItems.Children.Add(CreateConfigItem("⚙️", "Application Startup", "Configure which apps start automatically"));

            panel.Children.Add(configItems);
            section.Child = panel;

            return section;
        }

        private Control CreateProfileSection()
        {
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(51, 40, 167, 69)), // Semi-transparent green
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20)
            };

            var panel = new StackPanel { Spacing = 10 };

            panel.Children.Add(new TextBlock
            {
                Text = $"Selected Profile: {profile?.Name ?? "Default"}",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            var appCount = profile?.Apps?.Count ?? 0;
            panel.Children.Add(new TextBlock
            {
                Text = $"This profile contains {appCount} applications that can be configured.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220))
            });

            section.Child = panel;
            return section;
        }

        private Control CreateInfoSection()
        {
            var panel = new StackPanel { Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = "ℹ️ Don't worry - you can always change these settings later in the application settings.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Click 'Next' to begin the configuration process.",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return panel;
        }

        private Control CreateConfigItem(string icon, string title, string description)
        {
            var item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12
            };

            item.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Avalonia.Thickness(0, 2, 0, 0)
            });

            var textPanel = new StackPanel { Spacing = 2 };
            textPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                FontSize = 14
            });
            textPanel.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });

            item.Children.Add(textPanel);
            return item;
        }

        private bool HasAppInProfile(string appNamePart)
        {
            if (profile?.Apps == null) return false;
            
            foreach (var app in profile.Apps.Values)
            {
                if (app.App.CustomName.ToLower().Contains(appNamePart.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            // Welcome step doesn't need validation
            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            // Nothing to apply for welcome step
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