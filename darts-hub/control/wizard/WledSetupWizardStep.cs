using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using darts_hub.control.wizard.wled;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Avalonia.Interactivity;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Specialized wizard step for WLED configuration with connection testing
    /// </summary>
    public class WledSetupWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private AppBase wledApp;
        private ReadmeParser readmeParser;
        private Dictionary<string, string> argumentDescriptions;
        private Dictionary<string, Control> argumentControls;
        private WizardArgumentsConfig wizardConfig;
        
        // Connection testing
        private TextBox wledIpTextBox;
        private Button testConnectionButton;
        private Button scanNetworkButton;
        private TextBlock connectionStatusText;
        private Border connectionTestPanel;
        private StackPanel configurationPanel;
        private ComboBox discoveredDevicesComboBox;
        private bool isConnected = false;
        private bool isScanning = false;
        private static readonly HttpClient httpClient = new HttpClient();
        private CancellationTokenSource scanCancellationTokenSource;

        // Guided configuration steps
        private StackPanel guidedConfigPanel;
        private WledEssentialSettingsStep essentialSettingsStep;
        private WledPlayerColorsStep playerColorsStep;
        private WledGameWinEffectsStep gameWinEffectsStep;
        private WledScoreEffectsStep scoreEffectsStep;
        private WledBoardStatusStep boardStatusStep;
        
        // Score areas configuration
        private int currentAreaCount = 2; // Start with area 1 and 2

        public string Title => "Configure WLED Integration";
        public string Description => "Set up LED strip control and visual effects";
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
            
            // Find the WLED app
            wledApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains("wled"))?.App;

            // Initialize guided configuration steps
            InitializeGuidedSteps();
        }

        private void InitializeGuidedSteps()
        {
            if (wledApp == null) return;

            essentialSettingsStep = new WledEssentialSettingsStep(wledApp, wizardConfig, argumentControls);

            playerColorsStep = new WledPlayerColorsStep(wledApp, wizardConfig, argumentControls,
                onPlayerColorsSelected: () => ShowNextStep("GameWinCard"),
                onPlayerColorsSkipped: () => ShowNextStep("GameWinCard"));

            gameWinEffectsStep = new WledGameWinEffectsStep(wledApp, wizardConfig, argumentControls,
                onGameWinEffectsSelected: () => ShowNextStep("BoardStatusCard"),
                onGameWinEffectsSkipped: () => ShowNextStep("BoardStatusCard"));

            boardStatusStep = new WledBoardStatusStep(wledApp, wizardConfig, argumentControls,
                onBoardStatusConfigSelected: () => ShowNextStep("ScoreEffectsCard"),
                onBoardStatusConfigSkipped: () => ShowNextStep("ScoreEffectsCard"));

            scoreEffectsStep = new WledScoreEffectsStep(wledApp, wizardConfig, argumentControls,
                onScoreEffectsSelected: () => ShowNextStep("ScoreSelectionCard"),
                onScoreEffectsSkipped: CompleteGuidedSetup,
                onScoreEffectsCompleted: (selectedScores) => ShowSelectedScoreArgumentsAndAreas(selectedScores));
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 20,
                MaxWidth = 750,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (wledApp == null)
            {
                return CreateNotAvailableMessage();
            }

            // Load argument descriptions
            await LoadArgumentDescriptions();

            // Header
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Connection Test Panel
            connectionTestPanel = CreateConnectionTestPanel();
            mainPanel.Children.Add(connectionTestPanel);

            // Configuration Panel (initially hidden)
            configurationPanel = new StackPanel { Spacing = 20, IsVisible = false };
            mainPanel.Children.Add(configurationPanel);

            // Load existing IP if available
            await LoadExistingConfiguration();

            return mainPanel;
        }

        private async Task LoadArgumentDescriptions()
        {
            try
            {
                var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md";
                argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load WLED argument descriptions: {ex.Message}");
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
                Text = "⚠️ WLED Integration Not Available",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "WLED integration is not available in the current profile. You can skip this step and add WLED support later if needed.",
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
                Text = "💡 WLED Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "First, we'll connect to your WLED device, then configure the settings.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private Border CreateConnectionTestPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(80, 2, 176, 250)),
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(25),
                BorderBrush = new SolidColorBrush(Color.FromRgb(2, 176, 250)),
                BorderThickness = new Avalonia.Thickness(1)
            };

            var content = new StackPanel { Spacing = 20 };

            // Title
            content.Children.Add(new TextBlock
            {
                Text = "🔗 Connect to WLED Device",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Instructions
            content.Children.Add(new TextBlock
            {
                Text = "Use network scan to automatically discover your WLED device or enter the IP address manually. The scan looks for devices with 'WLED' title.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            // Network Scan Section
            var scanSection = new StackPanel { Spacing = 15 };

            scanNetworkButton = new Button
            {
                Content = "🔍 Scan Network for WLED Devices",
                Padding = new Avalonia.Thickness(25, 12),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 15
            };

            scanNetworkButton.Click += ScanNetwork_Click;
            scanSection.Children.Add(scanNetworkButton);

            // Discovered devices dropdown
            discoveredDevicesComboBox = new ComboBox
            {
                Width = 350,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                IsVisible = false,
                PlaceholderText = "Select discovered WLED device...",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            discoveredDevicesComboBox.SelectionChanged += (s, e) =>
            {
                if (discoveredDevicesComboBox.SelectedItem is WledDevice device)
                {
                    wledIpTextBox.Text = device.IpAddress;
                    discoveredDevicesComboBox.IsVisible = false;
                }
            };

            scanSection.Children.Add(discoveredDevicesComboBox);
            content.Children.Add(scanSection);

            // Separator
            content.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Margin = new Avalonia.Thickness(20, 10)
            });

            content.Children.Add(new TextBlock
            {
                Text = "OR enter IP address manually:",
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // IP Input Section
            var inputSection = new StackPanel { Spacing = 15 };

            var inputContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            wledIpTextBox = new TextBox
            {
                Text = "192.168.1.20",
                Width = 200,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Avalonia.Thickness(12, 8),
                Watermark = "e.g., 192.168.1.20"
            };

            testConnectionButton = new Button
            {
                Content = "🔌 Test Connection",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(126, 31, 141)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            testConnectionButton.Click += TestConnection_Click;

            inputContainer.Children.Add(wledIpTextBox);
            inputContainer.Children.Add(testConnectionButton);
            inputSection.Children.Add(inputContainer);

            content.Children.Add(inputSection);

            // Status Section
            connectionStatusText = new TextBlock
            {
                Text = "Use network scan to auto-discover or enter IP address manually",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            content.Children.Add(connectionStatusText);

            panel.Child = content;
            return panel;
        }

        private async Task CreateConfigurationSections()
        {
            if (wledApp?.Configuration?.Arguments == null) 
            {
                System.Diagnostics.Debug.WriteLine("No WLED arguments found");
                return;
            }

            var extensionConfig = wizardConfig.GetExtensionConfig("darts-wled");
            if (extensionConfig?.Sections == null) 
            {
                System.Diagnostics.Debug.WriteLine("No WLED extension config found for 'darts-wled'");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Creating {extensionConfig.Sections.Count} WLED sections");

            // Create enhanced settings style sections
            foreach (var sectionKvp in extensionConfig.Sections.OrderBy(s => s.Value.Priority))
            {
                var sectionName = sectionKvp.Key;
                var sectionConfig = sectionKvp.Value;

                var sectionCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(80, 45, 45, 48)),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Padding = new Avalonia.Thickness(20),
                    Margin = new Avalonia.Thickness(0, 8)
                };

                var sectionContent = new StackPanel { Spacing = 12 };

                // Section Header with expand/collapse
                var headerPanel = CreateSectionHeader(sectionName, sectionConfig.Expanded);
                sectionContent.Children.Add(headerPanel);

                // Section Arguments (initially visible based on expanded state)
                var argumentsPanel = new StackPanel { Spacing = 12, IsVisible = sectionConfig.Expanded };

                System.Diagnostics.Debug.WriteLine($"  Section '{sectionName}' with {sectionConfig.Arguments.Count} arguments");

                foreach (var argumentName in sectionConfig.Arguments)
                {
                    var argument = wledApp.Configuration.Arguments.FirstOrDefault(a => 
                        a.Name.Equals(argumentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Creating control for WLED argument: {argument.Name} = '{argument.Value}'");
                        var argumentControl = await CreateEnhancedArgumentControl(argument);
                        if (argumentControl != null)
                        {
                            argumentsPanel.Children.Add(argumentControl);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    WLED argument '{argumentName}' not found in app configuration");
                    }
                }

                // Add toggle functionality
                if (headerPanel.Children[0] is Button toggleButton)
                {
                    toggleButton.Click += (s, e) =>
                    {
                        argumentsPanel.IsVisible = !argumentsPanel.IsVisible;
                        toggleButton.Content = argumentsPanel.IsVisible ? "▼" : "▶";
                    };
                }

                sectionContent.Children.Add(argumentsPanel);
                sectionCard.Child = sectionContent;
                configurationPanel.Children.Add(sectionCard);
                
                System.Diagnostics.Debug.WriteLine($"  Added section '{sectionName}' with {argumentsPanel.Children.Count} controls");
            }
            
            System.Diagnostics.Debug.WriteLine($"Total WLED configuration sections created: {configurationPanel.Children.Count}");
        }

        private StackPanel CreateSectionHeader(string sectionName, bool expanded)
        {
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            var toggleButton = new Button
            {
                Content = expanded ? "▼" : "▶",
                FontSize = 16,
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Avalonia.Thickness(5)
            };

            headerPanel.Children.Add(toggleButton);

            headerPanel.Children.Add(new TextBlock
            {
                Text = sectionName,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });

            return headerPanel;
        }

        private async Task<Control> CreateEnhancedArgumentControl(Argument argument)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 10 };

            // Label and Description
            var labelPanel = new StackPanel { Spacing = 5 };

            var titleLabel = new TextBlock
            {
                Text = argument.NameHuman + (argument.Required ? " *" : ""),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            labelPanel.Children.Add(titleLabel);

            // Description
            string description = GetArgumentDescription(argument);
            if (!string.IsNullOrEmpty(description))
            {
                var descLabel = new TextBlock
                {
                    Text = description,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    TextWrapping = TextWrapping.Wrap
                };
                labelPanel.Children.Add(descLabel);
            }

            content.Children.Add(labelPanel);

            // Input Control
            var inputControl = CreateInputControl(argument);
            if (inputControl != null)
            {
                var inputContainer = new Grid();
                inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn(inputControl, 0);
                inputContainer.Children.Add(inputControl);

                // Clear button
                var clearButton = CreateClearButton(argument, inputControl);
                Grid.SetColumn(clearButton, 1);
                inputContainer.Children.Add(clearButton);

                content.Children.Add(inputContainer);
                argumentControls[argument.Name] = inputControl;
            }

            container.Child = content;
            return container;
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

            // Fallback descriptions for WLED arguments
            return argument.Name.ToLower() switch
            {
                "weps" or "con" => "IP address and port of your WLED controller device",
                "bri" => "Global brightness level for LED effects (1-255)",
                "ide" => "Default effect shown when no game is active",
                "hfo" => "Score threshold for high finish effects",
                "hf" => "Special effects for high finishes and checkouts",
                "g" => "Effects played when a game is won",
                "m" => "Effects played when a match is won",
                "b" => "Effects played when a player goes bust",
                _ => $"WLED configuration setting: {argument.NameHuman}"
            };
        }

        private Control CreateInputControl(Argument argument)
        {
            string type = argument.GetTypeClear();

            // Set default values from config
            if (string.IsNullOrEmpty(argument.Value))
            {
                var defaultValue = wizardConfig.GetDefaultValue(argument.Name);
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    argument.Value = defaultValue;
                }
            }

            // Use enhanced controls for WLED effect parameters
            if (WledSettings.IsEffectParameter(argument))
            {
                return WledSettings.CreateAdvancedEffectParameterControl(argument, 
                    () => { argument.IsValueChanged = true; }, wledApp);
            }

            return type switch
            {
                Argument.TypeString or Argument.TypePassword => CreateTextBox(argument),
                Argument.TypeBool => CreateCheckBox(argument),
                Argument.TypeInt => CreateNumericUpDown(argument, false),
                Argument.TypeFloat => CreateNumericUpDown(argument, true),
                _ => CreateTextBox(argument)
            };
        }

        private Control CreateSimpleArgumentControl(Argument argument)
        {
            // Use the factory method instead of duplicating code
            return WledArgumentControlFactory.CreateSimpleArgumentControl(argument, argumentControls, GetArgumentDescription);
        }

        private Control CreateTextBox(Argument argument)
        {
            var textBox = new TextBox
            {
                Text = argument.Value ?? "",
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            return textBox;
        }

        private Control CreateCheckBox(Argument argument)
        {
            bool isChecked = false;
            if (!string.IsNullOrEmpty(argument.Value))
            {
                isChecked = argument.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                           argument.Value == "1";
            }

            var checkBox = new CheckBox
            {
                Content = "Enable this feature",
                IsChecked = isChecked,
                FontSize = 13,
                Foreground = Brushes.White
            };

            checkBox.Checked += (s, e) =>
            {
                argument.Value = "True";
                argument.IsValueChanged = true;
            };

            checkBox.Unchecked += (s, e) =>
            {
                argument.Value = "False";
                argument.IsValueChanged = true;
            };

            return checkBox;
        }

        private Control CreateNumericUpDown(Argument argument, bool isFloat)
        {
            var numericUpDown = new NumericUpDown
            {
                FontSize = 13,
                Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Padding = new Avalonia.Thickness(10, 8),
                Width = 150,
                HorizontalAlignment = HorizontalAlignment.Left,
                Increment = isFloat ? 0.1m : 1m,
                FormatString = isFloat ? "F1" : "F0"
            };

            // Set value and limits
            if (isFloat)
            {
                if (double.TryParse(argument.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                {
                    numericUpDown.Value = (decimal)doubleVal;
                }
            }
            else
            {
                if (int.TryParse(argument.Value, out var intVal))
                {
                    numericUpDown.Value = intVal;
                }
            }

            // Set appropriate limits
            switch (argument.Name.ToLower())
            {
                case "bri" or "brightness":
                    numericUpDown.Minimum = 1;
                    numericUpDown.Maximum = 255;
                    break;
                case "hp" or "port" or "webp":
                    numericUpDown.Minimum = 1024;
                    numericUpDown.Maximum = 65535;
                    break;
                case "hfo":
                    numericUpDown.Minimum = 2;
                    numericUpDown.Maximum = 170;
                    break;
                case "du":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 10;
                    break;
                default:
                    numericUpDown.Minimum = isFloat ? -9999.9m : -9999;
                    numericUpDown.Maximum = isFloat ? 9999.9m : 9999;
                    break;
            }

            numericUpDown.ValueChanged += (s, e) =>
            {
                argument.Value = numericUpDown.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
                argument.IsValueChanged = true;
            };

            return numericUpDown;
        }

        private Control CreateClearButton(Argument argument, Control inputControl)
        {
            var clearButton = new Button
            {
                Content = "🗑️",
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 10,
                Margin = new Avalonia.Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            clearButton.Click += (s, e) =>
            {
                ResetArgumentToDefault(argument, inputControl);
            };

            return clearButton;
        }

        private void ResetArgumentToDefault(Argument argument, Control inputControl)
        {
            var defaultValue = wizardConfig.GetDefaultValue(argument.Name);
            argument.Value = defaultValue;
            argument.IsValueChanged = true;

            switch (inputControl)
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
                Content = "Start WLED integration automatically with profile",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = false
            };

            // Set current autostart status
            var appState = profile.Apps.Values.FirstOrDefault(a => a.App == wledApp);
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
                Text = "When enabled, WLED integration will automatically start when you launch your dart profile.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 200)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (wledApp == null)
            {
                return WizardValidationResult.Success(); // Skip if not available
            }

            if (!isConnected)
            {
                return WizardValidationResult.Error("Please test the WLED connection before proceeding. A successful connection is required to configure WLED integration.");
            }

            return WizardValidationResult.Success();
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var ipAddress = wledIpTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(ipAddress))
            {
                UpdateConnectionStatus("❌ Please enter an IP address", new SolidColorBrush(Color.FromRgb(220, 53, 69)));
                return;
            }

            testConnectionButton.IsEnabled = false;
            testConnectionButton.Content = "🔄 Testing...";
            UpdateConnectionStatus("Testing connection...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

            try
            {
                // Test WLED connection
                var result = await TestWledConnection(ipAddress);
                if (result.Success)
                {
                    isConnected = true;
                    UpdateConnectionStatus("✅ Connected! Found " + result.LedCount + " LEDs", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    
                    // Update WLED endpoint in configuration
                    UpdateWledConfiguration(ipAddress);
                    
                    // Load existing argument values
                    LoadExistingArgumentValues();
                    
                    // Show configuration panel with guided setup
                    configurationPanel.IsVisible = true;
                    configurationPanel.Children.Clear();
                    await CreateGuidedConfiguration();
                    
                    System.Diagnostics.Debug.WriteLine($"[WLED] Guided configuration panel created");
                }
                else
                {
                    isConnected = false;
                    UpdateConnectionStatus("❌ Connection failed: " + result.ErrorMessage, new SolidColorBrush(Color.FromRgb(220, 53, 69)));
                    configurationPanel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                UpdateConnectionStatus("❌ Error: " + ex.Message, new SolidColorBrush(Color.FromRgb(220, 53, 69)));
                configurationPanel.IsVisible = false;
                System.Diagnostics.Debug.WriteLine($"[WLED] Connection error: {ex}");
            }

            testConnectionButton.IsEnabled = true;
            testConnectionButton.Content = "🔌 Test Connection";
        }

        private async void ScanNetwork_Click(object sender, RoutedEventArgs e)
        {
            if (isScanning)
            {
                scanCancellationTokenSource?.Cancel();
                return;
            }

            isScanning = true;
            scanNetworkButton.IsEnabled = false;
            testConnectionButton.IsEnabled = false;
            scanNetworkButton.Content = "⏹️ Cancel Scan";
            UpdateConnectionStatus("🔍 Scanning network for WLED devices...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

            try
            {
                scanCancellationTokenSource = new CancellationTokenSource();
                var discoveredDevices = await NetworkDeviceScanner.ScanForWledDevices(scanCancellationTokenSource.Token);

                if (discoveredDevices.Count > 0)
                {
                    UpdateConnectionStatus($"✅ Found {discoveredDevices.Count} WLED device(s)!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    
                    discoveredDevicesComboBox.IsVisible = true;
                    discoveredDevicesComboBox.ItemsSource = discoveredDevices;

                    if (discoveredDevices.Count == 1)
                    {
                        discoveredDevicesComboBox.SelectedItem = discoveredDevices[0];
                        wledIpTextBox.Text = discoveredDevices[0].IpAddress;
                        discoveredDevicesComboBox.IsVisible = false;
                        
                        var deviceInfo = discoveredDevices[0].LedCount > 0 
                            ? $"✅ Auto-selected: {discoveredDevices[0].Name} at {discoveredDevices[0].IpAddress} ({discoveredDevices[0].LedCount} LEDs)"
                            : $"✅ Auto-selected: {discoveredDevices[0].Name} at {discoveredDevices[0].IpAddress}";
                        
                        UpdateConnectionStatus(deviceInfo, new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    }
                }
                else
                {
                    UpdateConnectionStatus("⚠️ No WLED devices found in network", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                }
            }
            catch (OperationCanceledException)
            {
                UpdateConnectionStatus("🛑 Network scan cancelled", new SolidColorBrush(Color.FromRgb(128, 128, 128)));
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus($"❌ Scan error: {ex.Message}", new SolidColorBrush(Color.FromRgb(220, 53, 69)));
                System.Diagnostics.Debug.WriteLine($"[WLED] Network scan error: {ex}");
            }

            isScanning = false;
            scanNetworkButton.IsEnabled = true;
            testConnectionButton.IsEnabled = true;
            scanNetworkButton.Content = "🔍 Scan Network for WLED Devices";
        }

        private async Task CreateGuidedConfiguration()
        {
            guidedConfigPanel = new StackPanel { Spacing = 20 };

            // Step 1: Essential Settings
            var essentialCard = essentialSettingsStep.CreateEssentialSettingsCard();
            guidedConfigPanel.Children.Add(essentialCard);

            // Step 2: Player-specific colors question
            var playerColorsCard = playerColorsStep.CreatePlayerColorsQuestionCard();
            guidedConfigPanel.Children.Add(playerColorsCard);

            // Step 3: Game win effects question (initially hidden)
            var gameWinCard = gameWinEffectsStep.CreateGameWinEffectsQuestionCard();
            gameWinCard.IsVisible = false;
            guidedConfigPanel.Children.Add(gameWinCard);

            // Step 4: Board status effects question (initially hidden)
            var boardStatusCard = boardStatusStep.CreateBoardStatusQuestionCard();
            boardStatusCard.IsVisible = false;
            guidedConfigPanel.Children.Add(boardStatusCard);

            // Step 5: Score effects question (initially hidden)
            var scoreEffectsCard = scoreEffectsStep.CreateScoreEffectsQuestionCard();
            scoreEffectsCard.IsVisible = false;
            guidedConfigPanel.Children.Add(scoreEffectsCard);

            // Step 6: Score selection (initially hidden)
            var scoreSelectionCard = scoreEffectsStep.CreateScoreSelectionCard();
            scoreSelectionCard.IsVisible = false;
            guidedConfigPanel.Children.Add(scoreSelectionCard);

            // Add autostart section
            var autostartCard = CreateAutostartSection();
            guidedConfigPanel.Children.Add(autostartCard);

            // Add to main configuration panel
            configurationPanel.Children.Add(guidedConfigPanel);
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
            var completionStep = new WledCompletionStep(
                playerColorsStep.ShowPlayerSpecificColors,
                gameWinEffectsStep.ShowGameWinEffects,
                boardStatusStep.ShowBoardStatusConfiguration,
                scoreEffectsStep.ShowScoreEffects,
                scoreEffectsStep.SelectedScores.Count);

            var completionCard = completionStep.CreateCompletionCard();
            guidedConfigPanel.Children.Add(completionCard);
        }

        private void UpdateWledConfiguration(string ipAddress)
        {
            // Update WLED endpoint in configuration (WEPS argument)
            var wledEndpointsArg = wledApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "WEPS");
            if (wledEndpointsArg != null)
            {
                // Store IP address WITHOUT http:// prefix for the argument
                var cleanIpAddress = ipAddress;
                if (cleanIpAddress.StartsWith("http://"))
                {
                    cleanIpAddress = cleanIpAddress.Substring(7);
                }
                else if (cleanIpAddress.StartsWith("https://"))
                {
                    cleanIpAddress = cleanIpAddress.Substring(8);
                }
                
                // Remove trailing slash if present
                if (cleanIpAddress.EndsWith("/"))
                {
                    cleanIpAddress = cleanIpAddress.Substring(0, cleanIpAddress.Length - 1);
                }
                
                wledEndpointsArg.Value = cleanIpAddress;
                wledEndpointsArg.IsValueChanged = true;
                System.Diagnostics.Debug.WriteLine($"[WLED] Updated WEPS argument: {cleanIpAddress} (without http://)");
            }

            // Update LED brightness to a reasonable default if not set
            var briArg = wledApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "BRI");
            if (briArg != null && string.IsNullOrEmpty(briArg.Value))
            {
                briArg.Value = "128";
                briArg.IsValueChanged = true;
                System.Diagnostics.Debug.WriteLine($"[WLED] Set BRI argument to default: 128");
            }
        }

        // Helper methods moved from original implementation
        private void LoadExistingArgumentValues()
        {
            try
            {
                if (wledApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing WLED argument values:");
                
                foreach (var argument in wledApp.Configuration.Arguments)
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
                System.Diagnostics.Debug.WriteLine($"Error loading existing WLED argument values: {ex.Message}");
            }
        }

        private async Task LoadExistingConfiguration()
        {
            if (wledApp?.Configuration?.Arguments != null)
            {
                var wledIpArg = wledApp.Configuration.Arguments.FirstOrDefault(a => a.Name == "WEPS");
                if (wledIpArg != null && !string.IsNullOrEmpty(wledIpArg.Value))
                {
                    // Argument contains IP without http://, so use it directly
                    var cleanIpAddress = wledIpArg.Value;
                    
                    // Remove http:// if somehow present in stored value
                    if (cleanIpAddress.StartsWith("http://"))
                    {
                        cleanIpAddress = cleanIpAddress.Substring(7);
                    }
                    else if (cleanIpAddress.StartsWith("https://"))
                    {
                        cleanIpAddress = cleanIpAddress.Substring(8);
                    }
                    
                    // Remove trailing slash if present
                    if (cleanIpAddress.EndsWith("/"))
                    {
                        cleanIpAddress = cleanIpAddress.Substring(0, cleanIpAddress.Length - 1);
                    }
                    
                    wledIpTextBox.Text = cleanIpAddress;
                }
            }
        }

        private void UpdateConnectionStatus(string message, SolidColorBrush color)
        {
            connectionStatusText.Text = message;
            connectionStatusText.Foreground = color;
        }

        private async Task<WledConnectionResult> TestWledConnection(string ipAddress)
        {
            try
            {
                var url = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                var infoUrl = $"{url}/json/info";
                using var response = await httpClient.GetAsync(infoUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var info = JsonConvert.DeserializeObject<WledInfo>(jsonContent);
                    
                    return new WledConnectionResult
                    {
                        Success = true,
                        LedCount = info?.leds?.count ?? 0,
                        Version = info?.ver ?? "Unknown",
                        Name = info?.name ?? "WLED Device"
                    };
                }
                else
                {
                    return new WledConnectionResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new WledConnectionResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new WledConnectionResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task ApplyConfiguration()
        {
            if (wledApp == null) return;

            try
            {
                // Configuration changes are already applied through the input controls
                if (wledApp.Configuration?.Arguments != null)
                {
                    foreach (var argument in wledApp.Configuration.Arguments)
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
                throw new Exception($"Failed to apply WLED configuration: {ex.Message}");
            }
        }

        public async Task OnStepShown()
        {
            await Task.CompletedTask;
        }

        public async Task OnStepHidden()
        {
            if (isScanning)
            {
                scanCancellationTokenSource?.Cancel();
            }
            await Task.CompletedTask;
        }

        public async Task ResetStep()
        {
            if (isScanning)
            {
                scanCancellationTokenSource?.Cancel();
            }
            
            isConnected = false;
            isScanning = false;
            configurationPanel.IsVisible = false;
            wledIpTextBox.Text = "192.168.1.20";
            
            discoveredDevicesComboBox.IsVisible = false;
            discoveredDevicesComboBox.ItemsSource = null;
            
            scanNetworkButton.Content = "🔍 Scan Network for WLED Devices";
            scanNetworkButton.IsEnabled = true;
            testConnectionButton.IsEnabled = true;
            
            UpdateConnectionStatus("Use network scan to auto-discover or enter IP address manually", 
                new SolidColorBrush(Color.FromRgb(180, 180, 180)));
        }

        private void ApplySelectedScores()
        {
            if (scoreEffectsStep.SelectedScores.Count == 0) return;

            // Find score-related arguments and set them based on selection
            var scoreArgs = wledApp.Configuration?.Arguments?
                .Where(a => a.Name.StartsWith("S") && int.TryParse(a.Name.Substring(1), out _))
                .ToList();

            if (scoreArgs != null)
            {
                foreach (var arg in scoreArgs)
                {
                    var scoreNumber = int.Parse(arg.Name.Substring(1));
                    if (scoreEffectsStep.SelectedScores.Contains(scoreNumber))
                    {
                        // Set a default effect for selected scores
                        if (string.IsNullOrEmpty(arg.Value))
                        {
                            arg.Value = "solid,#00FF00,1000"; // Green solid for 1 second as example
                            arg.IsValueChanged = true;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[WLED] Applied effects for {scoreEffectsStep.SelectedScores.Count} selected scores");
        }

        private void ShowSelectedScoreArgumentsAndAreas(HashSet<int> selectedScores)
        {
            // Show the selected score arguments directly in the existing score selection card
            // and automatically include score areas question - everything in one flow
            var scoreSelectionCard = guidedConfigPanel.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Name == "ScoreSelectionCard");

            if (scoreSelectionCard?.Child is StackPanel scoreSelectionContent)
            {
                // Add separator to the existing score selection card
                scoreSelectionContent.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Avalonia.Thickness(20, 20)
                });

                // Add header for score configuration
                scoreSelectionContent.Children.Add(new TextBlock
                {
                    Text = "🎯 Configure Selected Score Effects",
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 10, 0, 0)
                });

                scoreSelectionContent.Children.Add(new TextBlock
                {
                    Text = $"Configure effects for your {selectedScores.Count} selected scores using the enhanced WLED effect controls:",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 0, 10)
                });

                // Show arguments for selected scores using enhanced controls
                var scoreArgsPanel = new StackPanel { Spacing = 10 };

                foreach (var score in selectedScores.OrderBy(s => s))
                {
                    var argName = $"S{score}";
                    var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                        a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null)
                    {
                        // Use enhanced control for score effects - these are effect parameters
                        var control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, 
                            arg => $"Effect for score {score} - select from available WLED effects, colors, and durations", wledApp);
                        scoreArgsPanel.Children.Add(control);
                        System.Diagnostics.Debug.WriteLine($"[WLED] Added enhanced control for score {score} argument: {argName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED] Score argument not found: {argName}");
                    }
                }

                scoreSelectionContent.Children.Add(scoreArgsPanel);

                // Add Score Areas question directly here (part of same score-based effects container)
                ShowScoreAreasQuestionInline(scoreSelectionContent);
            }

            System.Diagnostics.Debug.WriteLine($"[WLED] Extended score selection card with {selectedScores.Count} score argument controls and inline score areas question");
        }

        private void ShowSelectedScoreArguments(HashSet<int> selectedScores)
        {
            // Redirect to the combined method
            ShowSelectedScoreArgumentsAndAreas(selectedScores);
        }

        private void ShowSelectedScoreArguments()
        {
            // Legacy method that uses scoreEffectsStep.SelectedScores
            ShowSelectedScoreArgumentsAndAreas(scoreEffectsStep.SelectedScores);
        }

        private void ShowScoreAreasQuestionInline(StackPanel parentContent)
        {
            // Add another separator for score areas section
            parentContent.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Margin = new Avalonia.Thickness(20, 20)
            });

            // Score Areas Question Header
            parentContent.Children.Add(new TextBlock
            {
                Text = "🎯 Score Area Effects",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            parentContent.Children.Add(new TextBlock
            {
                Text = "Would you like different effects for specific areas/regions on your LED strip?",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            // Yes/No buttons for Score Areas
            var areasButtonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0),
                Name = "AreasButtonPanel"
            };

            var yesAreasButton = new Button
            {
                Content = "🎯 Yes, configure score areas",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var noAreasButton = new Button
            {
                Content = "❌ No area effects needed",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(90, 98, 104)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            yesAreasButton.Click += (s, e) =>
            {
                ShowScoreAreasConfigurationInline(parentContent, areasButtonPanel);
            };

            noAreasButton.Click += (s, e) =>
            {
                CompleteGuidedSetup();
            };

            areasButtonPanel.Children.Add(yesAreasButton);
            areasButtonPanel.Children.Add(noAreasButton);
            parentContent.Children.Add(areasButtonPanel);
        }

        private void ShowScoreAreasConfigurationInline(StackPanel parentContent, StackPanel buttonsToHide)
        {
            // Hide the Yes/No buttons
            buttonsToHide.IsVisible = false;

            // Add separator for configuration section
            parentContent.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Margin = new Avalonia.Thickness(20, 15)
            });

            // Configuration header
            parentContent.Children.Add(new TextBlock
            {
                Text = "🎯 Configure Score Areas",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            });

            parentContent.Children.Add(new TextBlock
            {
                Text = "Configure effects for different areas/segments of your LED strip (A1-A12) with range selection:",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 10)
            });

            // Areas panel - will be populated dynamically
            var areasPanel = new StackPanel { Spacing = 15 };
            areasPanel.Name = "AreasPanel";

            // Show initial areas (A1 and A2) with enhanced score area controls
            for (int i = 1; i <= currentAreaCount; i++)
            {
                AddScoreAreaControlWithRangeSelection(areasPanel, i);
            }

            parentContent.Children.Add(areasPanel);

            // Add Area button
            var addAreaButton = new Button
            {
                Content = "➕ Add Another Score Area",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 110, 150)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 10, 0, 0)
            };

            addAreaButton.Click += (s, e) =>
            {
                currentAreaCount++;
                AddScoreAreaControlWithRangeSelection(areasPanel, currentAreaCount);
            };

            parentContent.Children.Add(addAreaButton);

            // Complete button
            var completeButton = new Button
            {
                Content = "✅ Complete WLED Setup",
                Padding = new Avalonia.Thickness(20, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(34, 142, 58)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 15, 0, 0)
            };

            completeButton.Click += (s, e) =>
            {
                CompleteGuidedSetup();
            };

            parentContent.Children.Add(completeButton);
        }

        private void AddScoreAreaControlWithRangeSelection(StackPanel parentPanel, int areaNumber)
        {
            var argName = $"A{areaNumber}";
            var argument = wledApp.Configuration?.Arguments?.FirstOrDefault(a => 
                a.Name.Equals(argName, StringComparison.OrdinalIgnoreCase));
            
            if (argument != null)
            {
                // Check if this is a score area effect parameter using the helper
                if (WledScoreAreaHelper.IsScoreAreaEffectParameter(argument))
                {
                    // Use the enhanced score area control with range selection
                    var control = CreateScoreAreaContainer(argument, areaNumber);
                    parentPanel.Children.Add(control);
                    System.Diagnostics.Debug.WriteLine($"[WLED] Added enhanced score area control for area {areaNumber} argument: {argName}");
                }
                else
                {
                    // Fallback to regular enhanced control
                    var control = WledArgumentControlFactory.CreateEnhancedArgumentControl(argument, argumentControls, 
                        arg => $"Effects for score area {areaNumber} (A{areaNumber}) - configure LED strip region effects with WLED controls", wledApp);
                    parentPanel.Children.Add(control);
                    System.Diagnostics.Debug.WriteLine($"[WLED] Added fallback enhanced control for area {areaNumber} argument: {argName}");
                }
            }
            else
            {
                // If argument doesn't exist, create a placeholder
                var infoPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(60, 70, 70, 70)),
                    CornerRadius = new Avalonia.CornerRadius(6),
                    Padding = new Avalonia.Thickness(15),
                    Margin = new Avalonia.Thickness(0, 8)
                };

                var infoContent = new StackPanel { Spacing = 10 };

                var titleLabel = new TextBlock
                {
                    Text = $"Score Area {areaNumber} Effects (A{areaNumber})",
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                };
                infoContent.Children.Add(titleLabel);

                var descLabel = new TextBlock
                {
                    Text = $"Configuration for {argName} (argument not found in current WLED configuration)",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                    TextWrapping = TextWrapping.Wrap
                };
                infoContent.Children.Add(descLabel);

                var textBox = new TextBox
                {
                    Text = "",
                    FontSize = 13,
                    Background = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    BorderThickness = new Avalonia.Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(4),
                    Padding = new Avalonia.Thickness(10, 8),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Watermark = "Enter effect configuration (e.g., 1-50 solid,#FF0000,2000)..."
                };

                infoContent.Children.Add(textBox);
                infoPanel.Child = infoContent;
                parentPanel.Children.Add(infoPanel);
                
                System.Diagnostics.Debug.WriteLine($"[WLED] Added placeholder for area {areaNumber} argument: {argName} (not found)");
            }
        }

        private Control CreateScoreAreaContainer(Argument argument, int areaNumber)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(60, 70, 70, 70)),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(15),
                Margin = new Avalonia.Thickness(0, 8)
            };

            var content = new StackPanel { Spacing = 10 };

            // Label and Description
            var labelPanel = new StackPanel { Spacing = 5 };

            var titleLabel = new TextBlock
            {
                Text = $"Score Area {areaNumber} Effects (A{areaNumber})" + (argument.Required ? " *" : ""),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            };
            labelPanel.Children.Add(titleLabel);

            var descLabel = new TextBlock
            {
                Text = $"Configure LED effects for score area {areaNumber} with range selection and effect parameters",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                TextWrapping = TextWrapping.Wrap
            };
            labelPanel.Children.Add(descLabel);

            content.Children.Add(labelPanel);

            // Use the WledScoreAreaHelper to create the advanced control with range dropdowns
            var scoreAreaControl = WledScoreAreaHelper.CreateScoreAreaEffectParameterControl(
                argument, 
                () => { argument.IsValueChanged = true; }, 
                wledApp
            );

            content.Children.Add(scoreAreaControl);
            
            // Store control reference
            argumentControls[argument.Name] = scoreAreaControl;

            container.Child = content;
            return container;
        }
    }

    // Helper classes for WLED connection
    public class WledConnectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public int LedCount { get; set; }
        public string Version { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class WledInfo
    {
        public string ver { get; set; } = "";
        public string name { get; set; } = "";
        public WledLeds leds { get; set; } = new();
    }

    public class WledLeds
    {
        public int count { get; set; }
    }
}