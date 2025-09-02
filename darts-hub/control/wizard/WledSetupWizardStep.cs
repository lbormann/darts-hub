using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
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
            await CreateConfigurationSections();
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

            // Network Scan Section (moved to top)
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

            // IP Input Section (moved below scan section)
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
                    
                    // Update WLED endpoint in configuration (WEPS argument)
                    var wledEndpointsArg = wledApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "WEPS");
                    if (wledEndpointsArg != null)
                    {
                        var url = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                        wledEndpointsArg.Value = url;
                        wledEndpointsArg.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"[WLED] Updated WEPS argument: {url}");
                    }

                    // Update LED brightness to a reasonable default if not set
                    var briArg = wledApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "BRI");
                    if (briArg != null && string.IsNullOrEmpty(briArg.Value))
                    {
                        briArg.Value = "128";
                        briArg.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"[WLED] Set BRI argument to default: 128");
                    }

                    // Load existing argument values from the app configuration
                    LoadExistingArgumentValues();
                    
                    // Try to start WLED extension for proper wled_data.json creation
                    await TryStartWledExtension(ipAddress);
                    
                    // Show configuration panel
                    configurationPanel.IsVisible = true;
                    
                    // Create/update sections after successful connection
                    configurationPanel.Children.Clear();
                    await CreateConfigurationSections();
                    
                    // Add autostart section
                    var autostartCard = CreateAutostartSection();
                    configurationPanel.Children.Add(autostartCard);
                    
                    // Only create wled_data.json manually if it doesn't exist or is empty
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var wledDataPath = Path.Combine(baseDirectory, "darts-wled", "wled_data.json");
                    
                    var needsManualCreation = false;
                    if (!File.Exists(wledDataPath))
                    {
                        needsManualCreation = true;
                        System.Diagnostics.Debug.WriteLine($"[WLED] wled_data.json does not exist, creating manually");
                    }
                    else
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(wledDataPath);
                            if (string.IsNullOrWhiteSpace(content))
                            {
                                needsManualCreation = true;
                                System.Diagnostics.Debug.WriteLine($"[WLED] wled_data.json is empty, creating manually");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[WLED] wled_data.json already exists with {content.Length} chars, keeping existing file");
                            }
                        }
                        catch (Exception ex)
                        {
                            needsManualCreation = true;
                            System.Diagnostics.Debug.WriteLine($"[WLED] Error reading wled_data.json: {ex.Message}, creating manually");
                        }
                    }
                    
                    if (needsManualCreation)
                    {
                        await CreateWledDataFile(ipAddress, result);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[WLED] Configuration panel updated with {configurationPanel.Children.Count} sections");
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
            UpdateConnectionStatus("🔍 Scanning network for WLED devices (looking for 'WLED' title)...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

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

        private void LoadExistingArgumentValues()
        {
            try
            {
                if (wledApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing WLED argument values:");
                
                foreach (var argument in wledApp.Configuration.Arguments)
                {
                    // If argument already has a value, keep it
                    if (!string.IsNullOrEmpty(argument.Value))
                    {
                        System.Diagnostics.Debug.WriteLine($"  {argument.Name}: {argument.Value} (existing)");
                        continue;
                    }
                    
                    // Otherwise try to get default value from config
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
                    // Extract IP from URL format
                    var url = wledIpArg.Value;
                    if (url.StartsWith("http://"))
                    {
                        url = url.Substring(7);
                    }
                    else if (url.StartsWith("https://"))
                    {
                        url = url.Substring(8);
                    }
                    wledIpTextBox.Text = url;
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
                // Ensure HTTP prefix
                var url = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                
                // Test basic connectivity
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

        private async Task CreateWledDataFile(string ipAddress, WledConnectionResult connectionResult)
        {
            try
            {
                // Create WLED data file in the darts-wled extension directory
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var wledAppDirectory = Path.Combine(baseDirectory, "darts-wled");
                
                System.Diagnostics.Debug.WriteLine($"[WLED] Base directory: {baseDirectory}");
                System.Diagnostics.Debug.WriteLine($"[WLED] WLED app directory: {wledAppDirectory}");
                
                if (!Directory.Exists(wledAppDirectory))
                {
                    Directory.CreateDirectory(wledAppDirectory);
                    System.Diagnostics.Debug.WriteLine($"[WLED] Created WLED directory: {wledAppDirectory}");
                }

                var wledData = new
                {
                    device = new
                    {
                        ip = ipAddress,
                        name = connectionResult.Name,
                        version = connectionResult.Version,
                        ledCount = connectionResult.LedCount
                    },
                    connection = new
                    {
                        tested = DateTime.Now,
                        success = true
                    },
                    configuration = new
                    {
                        brightness = 128,
                        effects = true,
                        scoreArea = true
                    }
                };

                var json = JsonConvert.SerializeObject(wledData, Formatting.Indented);
                var wledDataPath = Path.Combine(wledAppDirectory, "wled_data.json");
                await File.WriteAllTextAsync(wledDataPath, json);
                
                System.Diagnostics.Debug.WriteLine($"[WLED] Created wled_data.json at: {wledDataPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Failed to create wled_data.json: {ex.Message}");
            }
        }

        private async Task TryStartWledExtension(string ipAddress)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Attempting to download and start WLED extension for proper configuration");
                
                // Check if WLED app exists and is downloaded
                var wledAppDownloadable = profileManager.AppsDownloadable.FirstOrDefault(a => a.Name == "darts-wled");
                if (wledAppDownloadable == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] WLED app not found in downloadable apps");
                    return;
                }

                // Check if WLED app needs to be downloaded
                var wledExecutablePath = wledAppDownloadable.GetExecutablePath();
                if (string.IsNullOrEmpty(wledExecutablePath) || !File.Exists(wledExecutablePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] WLED app not downloaded, starting download...");
                    UpdateConnectionStatus("📥 Downloading WLED extension...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                    
                    var downloadSuccess = await DownloadWledExtension(wledAppDownloadable);
                    if (!downloadSuccess)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED] Failed to download WLED extension");
                        return;
                    }
                }

                // Prepare arguments for WLED startup
                var wledUrl = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                var runtimeArgs = new Dictionary<string, string>
                {
                    { "WEPS", wledUrl }
                };

                System.Diagnostics.Debug.WriteLine($"[WLED] Starting WLED extension with WEPS={wledUrl}");
                UpdateConnectionStatus("🚀 Starting WLED extension to create config file...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

                // Start WLED app with the IP address
                var started = wledAppDownloadable.Run(runtimeArgs);
                if (started)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension started successfully");
                    
                    // Wait for the extension to initialize and create wled_data.json with timeout
                    UpdateConnectionStatus("⏳ Waiting for WLED extension to create wled_data.json...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                    
                    var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    var wledDataPath = Path.Combine(baseDirectory, "darts-wled", "wled_data.json");
                    
                    // Wait for wled_data.json to be created or timeout after 20 seconds
                    var maxWaitTime = TimeSpan.FromSeconds(20);
                    var startTime = DateTime.Now;
                    var dataFileCreated = false;
                    
                    System.Diagnostics.Debug.WriteLine($"[WLED] Watching for wled_data.json at: {wledDataPath}");
                    
                    while (DateTime.Now - startTime < maxWaitTime)
                    {
                        if (File.Exists(wledDataPath))
                        {
                            // File exists, but check if it has content (not just empty file)
                            try
                            {
                                var content = await File.ReadAllTextAsync(wledDataPath);
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    dataFileCreated = true;
                                    System.Diagnostics.Debug.WriteLine($"[WLED] ✅ wled_data.json created successfully with content ({content.Length} chars)");
                                    break;
                                }
                            }
                            catch
                            {
                                // File might be locked, continue waiting
                            }
                        }
                        
                        // Wait 500ms before checking again
                        await Task.Delay(500);
                    }
                    
                    if (!dataFileCreated)
                    {
                        var elapsed = DateTime.Now - startTime;
                        System.Diagnostics.Debug.WriteLine($"[WLED] ⚠️ Timeout after {elapsed.TotalSeconds:F1}s - wled_data.json not created");
                    }
                    
                    // Stop the extension - either because file was created or timeout reached
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED] Stopping WLED extension after {(DateTime.Now - startTime).TotalSeconds:F1} seconds");
                        wledAppDownloadable.Close();
                        System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension stopped");
                        
                        // Wait a moment for clean shutdown
                        await Task.Delay(1000);
                        
                        // Final check and status update
                        if (dataFileCreated)
                        {
                            UpdateConnectionStatus("✅ WLED extension configured successfully!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[WLED] Creating wled_data.json manually as fallback");
                            UpdateConnectionStatus("⚠️ Timeout reached, creating config manually...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

                            // Create manually as fallback
                            await CreateWledDataFile(ipAddress, new WledConnectionResult
                            {
                                Success = true,
                                Name = "WLED Device",
                                Version = "Unknown",
                                LedCount = 144
                            });
                            
                            UpdateConnectionStatus("✅ WLED configuration created!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED] Error stopping WLED extension: {ex.Message}");
                        
                        // Create data file manually as fallback
                        if (!dataFileCreated)
                        {
                            await CreateWledDataFile(ipAddress, new WledConnectionResult
                            {
                                Success = true,
                                Name = "WLED Device",
                                Version = "Unknown",
                                LedCount = 144
                            });
                        }
                        
                        UpdateConnectionStatus("✅ WLED configuration completed!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] Failed to start WLED extension, creating wled_data.json manually");
                    UpdateConnectionStatus("⚠️ Could not start extension, creating config manually...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                    
                    // Create data file manually if we can't start the extension
                    await CreateWledDataFile(ipAddress, new WledConnectionResult
                    {
                        Success = true,
                        Name = "WLED Device",
                        Version = "Unknown",
                        LedCount = 144
                    });
                    
                    UpdateConnectionStatus("✅ WLED configuration created manually!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Error in TryStartWledExtension: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[WLED] Stack trace: {ex.StackTrace}");
                
                UpdateConnectionStatus("⚠️ Extension error, creating config manually...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                
                // Create data file manually as fallback
                await CreateWledDataFile(ipAddress, new WledConnectionResult
                {
                    Success = true,
                    Name = "WLED Device",
                    Version = "Unknown",
                    LedCount = 144
                });
                
                UpdateConnectionStatus("✅ WLED configuration completed!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
            }
        }

        private async Task<bool> DownloadWledExtension(AppDownloadable wledApp)
        {
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                
                // Subscribe to download events
                EventHandler<AppEventArgs> onDownloadFinished = null;
                EventHandler<AppEventArgs> onDownloadFailed = null;
                
                onDownloadFinished = (sender, args) =>
                {
                    if (args.App == wledApp)
                    {
                        profileManager.AppDownloadFinished -= onDownloadFinished;
                        profileManager.AppDownloadFailed -= onDownloadFailed;
                        System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension download completed successfully");
                        tcs.TrySetResult(true);
                    }
                };
                
                onDownloadFailed = (sender, args) =>
                {
                    if (args.App == wledApp)
                    {
                        profileManager.AppDownloadFinished -= onDownloadFinished;
                        profileManager.AppDownloadFailed -= onDownloadFailed;
                        System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension download failed: {args.Message}");
                        tcs.TrySetResult(false);
                    }
                };
                
                profileManager.AppDownloadFinished += onDownloadFinished;
                profileManager.AppDownloadFailed += onDownloadFailed;
                
                // Start the download using Install method
                var downloadStarted = wledApp.Install();
                if (!downloadStarted)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension download not started (already up to date?)");
                    profileManager.AppDownloadFinished -= onDownloadFinished;
                    profileManager.AppDownloadFailed -= onDownloadFailed;
                    return true; // Already downloaded
                }
                
                // Wait for download to complete (with timeout)
                var downloadTask = tcs.Task;
                var timeoutTask = Task.Delay(60000); // 60 seconds timeout
                
                var completedTask = await Task.WhenAny(downloadTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    System.Diagnostics.Debug.WriteLine($"[WLED] WLED extension download timed out");
                    profileManager.AppDownloadFinished -= onDownloadFinished;
                    profileManager.AppDownloadFailed -= onDownloadFailed;
                    return false;
                }
                
                return await downloadTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Error downloading WLED extension: {ex.Message}");
                return false;
            }
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