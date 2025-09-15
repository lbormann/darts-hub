using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using darts_hub.control.wizard.caller;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Enhanced caller setup wizard step with guided configuration
    /// </summary>
    public class CallerSetupWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private AppBase callerApp;
        private ReadmeParser readmeParser;
        private Dictionary<string, string> argumentDescriptions;
        private Dictionary<string, Control> argumentControls;
        private WizardArgumentsConfig wizardConfig;

        // Guided configuration steps
        private StackPanel guidedConfigPanel;
        private CallerEssentialSettingsStep essentialSettingsStep;
        private CallerCameraConfigStep cameraConfigStep;
        private CallerDownloadStep downloadStep;
        private CallerRandomStep randomStep;
        private CallerCheckoutStep checkoutStep;
        private CallerFixedStep fixedStep;

        public string Title => "Configure Darts Caller (Voice Announcements)";
        public string Description => "Set up voice announcements and scoring calls";
        public string IconName => "darts";
        public bool CanSkip => false; // Caller is mandatory

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
            this.readmeParser = new ReadmeParser();
            this.argumentControls = new Dictionary<string, Control>();
            this.argumentDescriptions = new Dictionary<string, string>();
            this.wizardConfig = WizardArgumentsConfig.Instance;
            
            // Find the Caller app
            callerApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains("caller"))?.App;

            // Initialize guided configuration steps
            InitializeGuidedSteps();
        }

        private void InitializeGuidedSteps()
        {
            // Steps werden später initialisiert, nachdem die Beschreibungen geladen wurden
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 20,
                MaxWidth = 750,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (callerApp == null)
            {
                return CreateNotAvailableMessage();
            }

            // Load argument descriptions FIRST
            await LoadArgumentDescriptions();

            // THEN initialize guided steps with loaded descriptions
            InitializeGuidedStepsWithDescriptions();

            // Header
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Create guided configuration immediately
            await CreateGuidedConfiguration();
            mainPanel.Children.Add(guidedConfigPanel);

            // Load existing argument values
            LoadExistingArgumentValues();

            return mainPanel;
        }

        private void InitializeGuidedStepsWithDescriptions()
        {
            if (callerApp == null) return;

            essentialSettingsStep = new CallerEssentialSettingsStep(callerApp, wizardConfig, argumentControls, argumentDescriptions);

            cameraConfigStep = new CallerCameraConfigStep(callerApp, wizardConfig, argumentControls, argumentDescriptions,
                onCameraConfigSelected: () => ShowNextStep("DownloadConfigCard"),
                onCameraConfigSkipped: () => ShowNextStep("DownloadConfigCard"));

            downloadStep = new CallerDownloadStep(callerApp, wizardConfig, argumentControls, argumentDescriptions,
                onDownloadConfigSelected: () => ShowNextStep("RandomConfigCard"),
                onDownloadConfigSkipped: () => ShowNextStep("RandomConfigCard"));

            randomStep = new CallerRandomStep(callerApp, wizardConfig, argumentControls, argumentDescriptions,
                onRandomConfigSelected: () => ShowNextStep("CheckoutConfigCard"),
                onRandomConfigSkipped: () => ShowNextStep("CheckoutConfigCard"));

            checkoutStep = new CallerCheckoutStep(callerApp, wizardConfig, argumentControls, argumentDescriptions,
                onCheckoutConfigSelected: () => ShowNextStep("FixedConfigCard"),
                onCheckoutConfigSkipped: () => ShowNextStep("FixedConfigCard"));

            fixedStep = new CallerFixedStep(callerApp, wizardConfig, argumentControls, argumentDescriptions,
                onFixedConfigSelected: CompleteGuidedSetup,
                onFixedConfigSkipped: CompleteGuidedSetup);
        }

        private async Task LoadArgumentDescriptions()
        {
            try
            {
                var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md";
                argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Caller argument descriptions: {ex.Message}");
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
                Text = "⚠️ Darts Caller Not Available",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "The darts caller (voice announcements) is not available in the current profile. This is required for score announcements and cannot be skipped.",
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
                Text = "🗣️ Darts Caller Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Configure voice announcements, score calls, and media settings for enhanced dart game experience.",
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

            // Step 1: Essential Settings (Autodarts credentials, Media path)
            var essentialCard = essentialSettingsStep.CreateEssentialSettingsCard();
            guidedConfigPanel.Children.Add(essentialCard);

            // Step 2: Voice & Media configuration question
            var cameraConfigCard = cameraConfigStep.CreateCameraConfigQuestionCard();
            guidedConfigPanel.Children.Add(cameraConfigCard);

            // Step 3: Download configuration question (initially hidden)
            var downloadConfigCard = downloadStep.CreateDownloadConfigQuestionCard();
            downloadConfigCard.IsVisible = false;
            guidedConfigPanel.Children.Add(downloadConfigCard);

            // Step 4: Random caller configuration question (initially hidden)
            var randomConfigCard = randomStep.CreateRandomConfigQuestionCard();
            randomConfigCard.IsVisible = false;
            guidedConfigPanel.Children.Add(randomConfigCard);

            // Step 5: Checkout configuration question (initially hidden)
            var checkoutConfigCard = checkoutStep.CreateCheckoutConfigQuestionCard();
            checkoutConfigCard.IsVisible = false;
            guidedConfigPanel.Children.Add(checkoutConfigCard);

            // Step 6: Fixed caller configuration question (initially hidden)
            var fixedConfigCard = fixedStep.CreateFixedConfigQuestionCard();
            fixedConfigCard.IsVisible = false;
            guidedConfigPanel.Children.Add(fixedConfigCard);

            // Add autostart section (Caller should always autostart)
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
            var completionStep = new CallerCompletionStep(
                cameraConfigStep.ShowCameraConfiguration,
                downloadStep.ShowDownloadConfiguration,
                randomStep.ShowRandomConfiguration,
                checkoutStep.ShowCheckoutConfiguration,
                fixedStep.ShowFixedConfiguration);

            var completionCard = completionStep.CreateCompletionCard();
            guidedConfigPanel.Children.Add(completionCard);
        }

        //private string GetArgumentDescription(Argument argument)
        //{
        //    // First try to get description from parsed README
        //    if (argumentDescriptions.TryGetValue(argument.Name, out string description) && !string.IsNullOrEmpty(description))
        //    {
        //        return description;
        //    }

        //    // Then try the argument's own description
        //    if (!string.IsNullOrEmpty(argument.Description))
        //    {
        //        return argument.Description;
        //    }

        //    // Fallback descriptions for Caller arguments (based on actual arguments from ProfileManager)
        //    return argument.Name.ToUpper() switch
        //    {
        //        "U" => "Your Autodarts email address for authentication",
        //        "P" => "Your Autodarts password for authentication", 
        //        "B" => "Your Autodarts board ID for connection",
        //        "M" => "Path to folder containing voice media files for announcements",
        //        "MS" => "Path to shared media folder for additional voice packs",
        //        "V" => "Volume level for voice announcements (0.0 to 1.0)",
        //        "C" => "Select specific caller/announcer voice",
        //        "R" => "Random caller selection mode",
        //        "RL" => "Language for random caller selection",
        //        "RG" => "Gender preference for random caller selection", 
        //        "CCP" => "Call out current player name",
        //        "CBA" => "Enable announcements for bot actions",
        //        "E" => "Frequency of dart throw announcements",
        //        "ETS" => "Include total score in dart announcements",
        //        "PCC" => "Call out possible checkout scores",
        //        "PCCYO" => "Only announce checkouts for yourself",
        //        "A" => "Ambient sound volume level",
        //        "AAC" => "Play ambient sounds after call announcements",
        //        "DL" => "Download limit for voice packs",
        //        "DLLA" => "Language preference for voice pack downloads",
        //        "DLN" => "Specific caller name for downloads",
        //        "ROVP" => "Remove old voice packs when downloading new ones",
        //        "BAV" => "Background audio volume level",
        //        "LPB" => "Enable local playback mode",
        //        "WEBDH" => "Disable HTTPS for web caller interface",
        //        "HP" => "Host port for caller web service",
        //        "DEB" => "Enable debug mode for troubleshooting",
        //        "CC" => "Enable certificate checking for HTTPS connections",
        //        "CRL" => "Enable real-life caller mode",
        //        _ => $"Caller voice announcement setting: {argument.NameHuman}"
        //    };
        //}

        private Control CreateAutostartSection()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 40, 167, 69)),
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
                Content = "Start darts caller automatically with profile (Recommended)",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = true // Caller should typically autostart
            };

            // Set current autostart status
            var appState = profile.Apps.Values.FirstOrDefault(a => a.App == callerApp);
            if (appState != null)
            {
                autostartCheckBox.IsChecked = appState.TaggedForStart;
                
                // Force autostart for caller since it's essential
                if (!appState.TaggedForStart)
                {
                    appState.TaggedForStart = true;
                    autostartCheckBox.IsChecked = true;
                }
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
                Text = "The darts caller should start automatically as it provides essential voice announcements and score calls.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        private void LoadExistingArgumentValues()
        {
            try
            {
                if (callerApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing Caller argument values:");
                
                foreach (var argument in callerApp.Configuration.Arguments)
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
                System.Diagnostics.Debug.WriteLine($"Error loading existing Caller argument values: {ex.Message}");
            }
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (callerApp == null)
            {
                return WizardValidationResult.Error("Darts caller is required and must be configured.");
            }

            // Basic validation - ensure required arguments are set
            var requiredArgs = new[] { "U", "P", "B", "M" }; // Email, Password, Board ID, Media Path
            var missingArgs = new List<string>();

            foreach (var argName in requiredArgs)
            {
                var arg = callerApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                if (arg != null && string.IsNullOrEmpty(arg.Value))
                {
                    missingArgs.Add(arg.NameHuman);
                }
            }

            if (missingArgs.Count > 0)
            {
                return WizardValidationResult.Error($"Please fill in the required fields: {string.Join(", ", missingArgs)}");
            }

            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            if (callerApp == null) return;

            try
            {
                if (callerApp.Configuration?.Arguments != null)
                {
                    foreach (var argument in callerApp.Configuration.Arguments)
                    {
                        if (!string.IsNullOrEmpty(argument.Value))
                        {
                            argument.IsValueChanged = true;
                        }
                    }
                }

                // Ensure caller is set to autostart since it's essential
                var appState = profile.Apps.Values.FirstOrDefault(a => a.App == callerApp);
                if (appState != null)
                {
                    appState.TaggedForStart = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply Caller configuration: {ex.Message}");
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
                var argument = callerApp?.Configuration?.Arguments?.FirstOrDefault(a => a.Name == kvp.Key);
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