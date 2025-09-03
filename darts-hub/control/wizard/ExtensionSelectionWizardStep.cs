using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Wizard step for selecting which extensions to configure
    /// </summary>
    public class ExtensionSelectionWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private Dictionary<string, CheckBox> extensionCheckBoxes;
        private Dictionary<string, AppBase> availableExtensions;

        public string Title => "Select Extensions to Configure";
        public string Description => "Choose which extensions you want to set up in this wizard";
        public string IconName => "darts";
        public bool CanSkip => false;

        // Property to store selected extensions for other wizard steps
        public HashSet<string> SelectedExtensions { get; private set; }

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
            this.extensionCheckBoxes = new Dictionary<string, CheckBox>();
            this.availableExtensions = new Dictionary<string, AppBase>();
            this.SelectedExtensions = new HashSet<string>();

            // Collect available extensions
            if (profile?.Apps != null)
            {
                foreach (var appState in profile.Apps.Values)
                {
                    var app = appState.App;
                    var appName = app.CustomName.ToLower();
                    
                    // Add configurable extensions (excluding caller which is mandatory)
                    if (app.IsConfigurable() && !appName.Contains("caller"))
                    {
                        availableExtensions[appName] = app;
                    }
                }
            }
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 25,
                MaxWidth = 700,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Header
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Caller Information (always configured)
            var callerSection = CreateCallerSection();
            mainPanel.Children.Add(callerSection);

            // Extensions Selection
            var extensionsSection = CreateExtensionsSection();
            mainPanel.Children.Add(extensionsSection);

            // Information
            var infoSection = CreateInfoSection();
            mainPanel.Children.Add(infoSection);

            return mainPanel;
        }

        private Control CreateHeader()
        {
            var panel = new StackPanel { Spacing = 10 };

            panel.Children.Add(new TextBlock
            {
                Text = "🔧 Extension Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Select which extensions you want to configure. You can always configure additional extensions later in the settings.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private Control CreateCallerSection()
        {
            var section = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(51, 40, 167, 69)), // Semi-transparent green
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20)
            };

            var panel = new StackPanel { Spacing = 10 };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = "🎯",
                FontSize = 20,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = "Darts-Caller (Required)",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerPanel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(8, 4),
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = "INCLUDED",
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                }
            });

            panel.Children.Add(headerPanel);

            panel.Children.Add(new TextBlock
            {
                Text = "The core dart recognition system will always be configured as it's essential for the darts experience.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap
            });

            section.Child = panel;
            return section;
        }

        private Control CreateExtensionsSection()
        {
            var section = new Expander
            {
                Header = "📦 Available Extensions",
                IsExpanded = true,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 16
            };

            var panel = new StackPanel { Spacing = 15, Margin = new Avalonia.Thickness(10) };

            if (availableExtensions.Any())
            {
                foreach (var extension in availableExtensions)
                {
                    var extensionCard = CreateExtensionCard(extension.Key, extension.Value);
                    panel.Children.Add(extensionCard);
                }
            }
            else
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "No additional extensions are available for configuration in this profile.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontStyle = FontStyle.Italic,
                    TextWrapping = TextWrapping.Wrap
                });
            }

            section.Content = panel;
            return section;
        }

        private Control CreateExtensionCard(string extensionKey, AppBase app)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(51, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(15)
            };

            var panel = new StackPanel { Spacing = 10 };

            // Header with checkbox
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var checkBox = new CheckBox
            {
                Content = GetExtensionDisplayName(extensionKey, app), // Add the extension name here
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                IsChecked = GetDefaultSelection(extensionKey)
            };

            checkBox.Checked += (s, e) => UpdateSelection();
            checkBox.Unchecked += (s, e) => UpdateSelection();

            extensionCheckBoxes[extensionKey] = checkBox;

            headerPanel.Children.Add(new TextBlock
            {
                Text = GetExtensionIcon(extensionKey),
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerPanel.Children.Add(checkBox);

            panel.Children.Add(headerPanel);

            // Description
            var description = GetExtensionDescription(extensionKey);
            panel.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap
            });

            // Configuration count
            var configCount = app.Configuration?.Arguments?.Count(a => !a.IsRuntimeArgument) ?? 0;
            if (configCount > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"📝 {configCount} configuration options available",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                    FontStyle = FontStyle.Italic
                });
            }

            card.Child = panel;
            return card;
        }

        private bool GetDefaultSelection(string extensionKey)
        {
            // Default selections based on common usage
            return extensionKey switch
            {
                var key when key.Contains("wled") => true,
                var key when key.Contains("pixelit") => true,
                _ => false
            };
        }

        private string GetExtensionIcon(string extensionKey)
        {
            return extensionKey switch
            {
                var key when key.Contains("wled") => "💡",
                var key when key.Contains("pixelit") => "📱",
                var key when key.Contains("voice") => "🗣️",
                var key when key.Contains("gif") => "🎬",
                var key when key.Contains("extern") => "🔗",
                _ => "⚙️"
            };
        }

        private string GetExtensionDescription(string extensionKey)
        {
            return extensionKey switch
            {
                var key when key.Contains("wled") => "Control LED strips with dynamic effects and dart game feedback. Perfect for ambient lighting and visual dart tracking.",
                var key when key.Contains("pixelit") => "Display scores, animations, and game information on a smart LED matrix display.",
                var key when key.Contains("voice") => "Add voice announcements and audio feedback to enhance your dart game experience.",
                var key when key.Contains("gif") => "Display animated GIFs and custom visuals during dart games for entertainment.",
                var key when key.Contains("extern") => "Integrate with external services and APIs for extended functionality.",
                _ => "Additional functionality and customization options for your dart setup."
            };
        }

        private string GetExtensionDisplayName(string extensionKey, AppBase app)
        {
            // First try to get a nice display name from the extension key
            var displayName = extensionKey switch
            {
                var key when key.Contains("wled") => "WLED LED Strip Control",
                var key when key.Contains("pixelit") => "Pixelit LED Matrix Display",
                var key when key.Contains("voice") => "Voice Announcements",
                var key when key.Contains("gif") => "GIF Display & Media",
                var key when key.Contains("extern") => "External Integrations",
                _ => null
            };

            // If no specific display name found, use the app's custom name or name
            if (displayName == null)
            {
                displayName = !string.IsNullOrEmpty(app.CustomName) ? app.CustomName : app.Name;
                
                // Clean up the name for better display
                displayName = displayName.Replace("darts-", "").Replace("_", " ");
                displayName = char.ToUpper(displayName[0]) + displayName.Substring(1);
            }

            return displayName;
        }

        private Control CreateInfoSection()
        {
            var panel = new StackPanel { Spacing = 10 };

            panel.Children.Add(new TextBlock
            {
                Text = "💡 Tip: You can configure extensions individually later",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 149, 237)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Don't worry if you're unsure about an extension - you can always add or modify configurations later through the main application settings.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                FontStyle = FontStyle.Italic,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private void UpdateSelection()
        {
            SelectedExtensions.Clear();
            foreach (var kvp in extensionCheckBoxes)
            {
                if (kvp.Value.IsChecked == true)
                {
                    SelectedExtensions.Add(kvp.Key);
                }
            }
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            UpdateSelection();
            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            // Store selections for use by subsequent wizard steps
            UpdateSelection();
            await Task.CompletedTask;
        }

        public async Task OnStepShown()
        {
            // Initialize selections
            UpdateSelection();
            await Task.CompletedTask;
        }

        public async Task OnStepHidden()
        {
            UpdateSelection();
            await Task.CompletedTask;
        }

        public async Task ResetStep()
        {
            // Reset to default selections
            foreach (var kvp in extensionCheckBoxes)
            {
                kvp.Value.IsChecked = GetDefaultSelection(kvp.Key);
            }
            UpdateSelection();
            await Task.CompletedTask;
        }
    }
}