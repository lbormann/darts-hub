using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using darts_hub.control.wizard.gif;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Specialized wizard step for GIF display configuration with guided setup
    /// </summary>
    public class GifSetupWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private AppBase gifApp;
        private ReadmeParser readmeParser;
        private Dictionary<string, string> argumentDescriptions;
        private Dictionary<string, Control> argumentControls;
        private WizardArgumentsConfig wizardConfig;

        // Guided configuration steps
        private StackPanel guidedConfigPanel;
        private GifEssentialSettingsStep essentialSettingsStep;
        private GifDisplayModeStep displayModeStep;
        private GifMediaConfigStep mediaConfigStep;

        public string Title => "Configure GIF Display Integration";
        public string Description => "Set up animated GIF and media display for games";
        public string IconName => "darts";
        public bool CanSkip => true;

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
            this.readmeParser = new ReadmeParser();
            this.argumentControls = new Dictionary<string, Control>();
            this.argumentDescriptions = new Dictionary<string, string>();
            this.wizardConfig = WizardArgumentsConfig.Instance;
            
            // Find the GIF app
            gifApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains("gif"))?.App;

            // Initialize guided configuration steps
            InitializeGuidedSteps();
        }

        private void InitializeGuidedSteps()
        {
            if (gifApp == null) return;

            essentialSettingsStep = new GifEssentialSettingsStep(gifApp, wizardConfig, argumentControls);

            displayModeStep = new GifDisplayModeStep(gifApp, wizardConfig, argumentControls,
                onDisplayModeSelected: () => ShowNextStep("MediaConfigCard"),
                onDisplayModeSkipped: () => ShowNextStep("MediaConfigCard"));

            mediaConfigStep = new GifMediaConfigStep(gifApp, wizardConfig, argumentControls,
                onMediaConfigSelected: CompleteGuidedSetup,
                onMediaConfigSkipped: CompleteGuidedSetup);
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 20,
                MaxWidth = 750,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (gifApp == null)
            {
                return CreateNotAvailableMessage();
            }

            // Load argument descriptions
            await LoadArgumentDescriptions();

            // Header
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Create guided configuration immediately (no connection test needed for GIF)
            await CreateGuidedConfiguration();
            mainPanel.Children.Add(guidedConfigPanel);

            // Load existing argument values
            LoadExistingArgumentValues();

            return mainPanel;
        }

        private async Task LoadArgumentDescriptions()
        {
            try
            {
                var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md";
                argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load GIF argument descriptions: {ex.Message}");
                argumentDescriptions = new Dictionary<string, string>();
            }
        }

        private Control CreateNotAvailableMessage()
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "⚠️ GIF Display Integration Not Available",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "GIF display integration is not available in the current profile. You can skip this step and add GIF support later if needed.",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private Control CreateHeader()
        {
            var panel = new StackPanel { Spacing = 10 };

            panel.Children.Add(new TextBlock
            {
                Text = "🎬 GIF Display Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Configure your GIF and media display settings for an enhanced dart game experience with animated content.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private async Task CreateGuidedConfiguration()
        {
            guidedConfigPanel = new StackPanel { Spacing = 20 };

            // Step 1: Essential Settings
            var essentialCard = essentialSettingsStep.CreateEssentialSettingsCard();
            guidedConfigPanel.Children.Add(essentialCard);

            // Step 2: Display mode question
            var displayModeCard = displayModeStep.CreateDisplayModeQuestionCard();
            guidedConfigPanel.Children.Add(displayModeCard);

            // Step 3: Media configuration question (initially hidden)
            var mediaConfigCard = mediaConfigStep.CreateMediaConfigQuestionCard();
            mediaConfigCard.IsVisible = false;
            guidedConfigPanel.Children.Add(mediaConfigCard);

            // Add autostart section
            var autostartCard = CreateAutostartSection();
            guidedConfigPanel.Children.Add(autostartCard);
        }

        private void ShowNextStep(string stepName)
        {
            var stepCard = guidedConfigPanel.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Name == stepName);
            
            if (stepCard != null)
            {
                stepCard.IsVisible = true;
            }
        }

        private void CompleteGuidedSetup()
        {
            var completionStep = new GifCompletionStep(
                displayModeStep.ShowDisplayModeSettings,
                mediaConfigStep.ShowMediaConfiguration,
                displayModeStep.SelectedDisplayMode);

            var completionCard = completionStep.CreateCompletionCard();
            guidedConfigPanel.Children.Add(completionCard);
        }

        private string GetArgumentDescription(Argument argument)
        {
            if (argumentDescriptions.TryGetValue(argument.Name, out string description) && !string.IsNullOrEmpty(description))
            {
                return description;
            }

            if (!string.IsNullOrEmpty(argument.Description))
            {
                return argument.Description;
            }

            // Fallback descriptions for GIF arguments
            return argument.Name.ToLower() switch
            {
                "con" => "Connection URL to darts-caller service for receiving game events",
                "mp" or "mpath" => "Path to folder containing GIF files, images, and videos",
                "web" => "Enable web-based display interface for remote viewing",
                "webp" => "Port number for web-based display interface",
                "du" => "Duration to display each media file (in seconds, 0 = natural duration)",
                "autoplay" => "Automatically start playing media when displayed",
                "loop" => "Loop GIFs and videos continuously during playback",
                "volume" => "Audio volume level for videos with sound (0.0 to 1.0)",
                "fullscreen" => "Display media in fullscreen mode",
                "scaling" => "How to scale media to fit the display window",
                "filter" => "File type filter for media files (e.g., *.gif;*.jpg;*.png;*.mp4)",
                _ => $"GIF display configuration setting: {argument.NameHuman}"
            };
        }

        private Control CreateAutostartSection()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(20),
                Margin = new Avalonia.Thickness(0, 10)
            };

            var content = new StackPanel { Spacing = 15 };

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
                Text = "Startup Configuration",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            content.Children.Add(header);

            var autostartCheckBox = new CheckBox
            {
                Content = "Start GIF display integration automatically with profile",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = false
            };

            // Set current autostart status
            var appState = profile.Apps.Values.FirstOrDefault(a => a.App == gifApp);
            if (appState != null)
            {
                autostartCheckBox.IsChecked = appState.TaggedForStart;
            }

            autostartCheckBox.Checked += (s, e) =>
            {
                if (appState != null) appState.TaggedForStart = true;
            };

            autostartCheckBox.Unchecked += (s, e) =>
            {
                if (appState != null) appState.TaggedForStart = false;
            };

            content.Children.Add(autostartCheckBox);

            content.Children.Add(new TextBlock
            {
                Text = "When enabled, GIF display integration will automatically start when you launch your dart profile.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 240)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        private void LoadExistingArgumentValues()
        {
            try
            {
                if (gifApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing GIF argument values:");
                
                foreach (var argument in gifApp.Configuration.Arguments)
                {
                    if (!string.IsNullOrEmpty(argument.Value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  {argument.Name}: {argument.Value} (existing)");
                        continue;
                    }
                    
                    var defaultValue = wizardConfig.GetDefaultValue(argument.Name);
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        argument.Value = defaultValue;
                        System.Diagnostics.Debug.WriteLine($"  {argument.Name}: {defaultValue} (default)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  {argument.Name}: (empty)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading existing GIF argument values: {ex.Message}");
            }
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (gifApp == null)
            {
                return WizardValidationResult.Success();
            }

            // GIF display doesn't require connection testing, so we can always proceed
            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            if (gifApp == null) return;

            try
            {
                if (gifApp.Configuration?.Arguments != null)
                {
                    foreach (var argument in gifApp.Configuration.Arguments)
                    {
                        if (!string.IsNullOrEmpty(argument.Value))
                        {
                            argument.IsValueChanged = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply GIF configuration: {ex.Message}");
            }
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
            // Reset all controls to default values
            foreach (var kvp in argumentControls)
            {
                var argument = gifApp?.Configuration?.Arguments?.FirstOrDefault(a => a.Name == kvp.Key);
                if (argument != null)
                {
                    var defaultValue = wizardConfig.GetDefaultValue(argument.Name) ?? "";
                    argument.Value = defaultValue;
                    argument.IsValueChanged = true;

                    // Update the control
                    switch (kvp.Value)
                    {
                        case TextBox textBox:
                            textBox.Text = defaultValue;
                            break;
                        case CheckBox checkBox:
                            checkBox.IsChecked = defaultValue.Equals("True", StringComparison.OrdinalIgnoreCase);
                            break;
                        case NumericUpDown numericUpDown:
                            if (decimal.TryParse(defaultValue, out var decimalVal))
                                numericUpDown.Value = decimalVal;
                            else
                                numericUpDown.Value = 0;
                            break;
                    }
                }
            }
        }
    }
}