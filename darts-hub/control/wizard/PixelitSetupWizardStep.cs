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
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Specialized wizard step for Pixelit configuration with connection testing and network scanning
    /// </summary>
    public class PixelitSetupWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private AppBase pixelitApp;
        private ReadmeParser readmeParser;
        private Dictionary<string, string> argumentDescriptions;
        private Dictionary<string, Control> argumentControls;
        private WizardArgumentsConfig wizardConfig;
        
        // Connection testing
        private TextBox pixelitIpTextBox;
        private Button testConnectionButton;
        private Button scanNetworkButton;
        private TextBlock connectionStatusText;
        private Border connectionTestPanel;
        private StackPanel configurationPanel;
        private ComboBox discoveredDevicesComboBox;
        private bool isConnected = false;
        private bool isScanning = false;
        private static readonly HttpClient httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };
        private CancellationTokenSource scanCancellationTokenSource;

        public string Title => "Configure Pixelit Integration";
        public string Description => "Set up LED matrix display and animations";
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
            
            // Find the Pixelit app
            pixelitApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains("pixelit"))?.App;
        }

        public async Task<Control> CreateContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 20,
                MaxWidth = 750,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (pixelitApp == null)
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
                var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md";
                argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Pixelit argument descriptions: {ex.Message}");
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
                Text = "⚠️ Pixelit Integration Not Available",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Pixelit integration is not available in the current profile. You can skip this step and add Pixelit support later if needed.",
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
                Text = "📱 Pixelit Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Connect to your Pixelit device automatically via network scan or manually enter the IP address.",
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
                Background = new SolidColorBrush(Color.FromArgb(80, 156, 39, 176)),
                CornerRadius = new Avalonia.CornerRadius(12),
                Padding = new Avalonia.Thickness(25),
                BorderBrush = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                BorderThickness = new Avalonia.Thickness(1)
            };

            var content = new StackPanel { Spacing = 20 };

            // Title
            content.Children.Add(new TextBlock
            {
                Text = "🔗 Connect to Pixelit Device",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Instructions
            content.Children.Add(new TextBlock
            {
                Text = "Use network scan to automatically discover your Pixelit device or enter the IP address manually. The scan looks for devices with 'PixelIt WebUI' title.",
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
                Content = "🔍 Scan Network for Pixelit Devices",
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
                PlaceholderText = "Select discovered Pixelit device...",
                HorizontalAlignment = HorizontalAlignment.Center
            };

            discoveredDevicesComboBox.SelectionChanged += (s, e) =>
            {
                if (discoveredDevicesComboBox.SelectedItem is PixelitDevice device)
                {
                    pixelitIpTextBox.Text = device.IpAddress;
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

            pixelitIpTextBox = new TextBox
            {
                Text = "192.168.1.117",
                Width = 200,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                Padding = new Avalonia.Thickness(12, 8),
                Watermark = "e.g., 192.168.1.117"
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

            inputContainer.Children.Add(pixelitIpTextBox);
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
            if (pixelitApp?.Configuration?.Arguments == null) 
            {
                System.Diagnostics.Debug.WriteLine("No Pixelit arguments found");
                return;
            }

            var extensionConfig = wizardConfig.GetExtensionConfig("darts-pixelit");
            if (extensionConfig?.Sections == null) 
            {
                System.Diagnostics.Debug.WriteLine("No Pixelit extension config found for 'darts-pixelit'");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Creating {extensionConfig.Sections.Count} Pixelit sections");

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
                    var argument = pixelitApp.Configuration.Arguments.FirstOrDefault(a => 
                        a.Name.Equals(argumentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Creating control for Pixelit argument: {argument.Name} = '{argument.Value}'");
                        var argumentControl = await CreateEnhancedArgumentControl(argument);
                        if (argumentControl != null)
                        {
                            argumentsPanel.Children.Add(argumentControl);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    Pixelit argument '{argumentName}' not found in app configuration");
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
            
            System.Diagnostics.Debug.WriteLine($"Total Pixelit configuration sections created: {configurationPanel.Children.Count}");
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

            // Fallback descriptions for Pixelit arguments
            return argument.Name.ToLower() switch
            {
                "peps" or "con" => "IP address and port of your Pixelit controller device",
                "tp" => "Path to the templates directory containing display templates",
                "bri" => "Global brightness level for display effects (1-255)",
                "as" => "Animation shown when the application starts",
                "ide" => "Default animation shown when no game is active",
                "gs" => "Animation shown when a game starts",
                "ms" => "Animation shown when a match starts",
                "hfo" => "Score threshold for high finish animations",
                "hf" => "Special animations for high finishes and checkouts",
                "g" => "Animations played when a game is won",
                "m" => "Animations played when a match is won",
                "b" => "Animations played when a player goes bust",
                "pj" => "Animations played when a player joins the lobby",
                "pl" => "Animations played when a player leaves the lobby",
                _ => $"Pixelit configuration setting: {argument.NameHuman}"
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

            return type switch
            {
                Argument.TypeString or Argument.TypePassword => CreateTextBox(argument),
                Argument.TypeBool => CreateCheckBox(argument),
                Argument.TypeInt => CreateNumericUpDown(argument, false),
                Argument.TypeFloat => CreateNumericUpDown(argument, true),
                Argument.TypePath => CreatePathSelector(argument),
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
                case "hfo":
                    numericUpDown.Minimum = 2;
                    numericUpDown.Maximum = 170;
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

        private Control CreatePathSelector(Argument argument)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

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
                Width = 250
            };

            var browseButton = new Button
            {
                Content = "Browse...",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(126, 31, 141)),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            browseButton.Click += async (s, e) =>
            {
                Window parentWindow = null;
                var topLevel = TopLevel.GetTopLevel(browseButton);
                if (topLevel is Window window)
                {
                    parentWindow = window;
                }

                var dialog = new OpenFolderDialog();
                var result = await dialog.ShowAsync(parentWindow);
                if (!string.IsNullOrEmpty(result))
                {
                    textBox.Text = result;
                    argument.Value = result;
                    argument.IsValueChanged = true;
                }
            };

            panel.Children.Add(textBox);
            panel.Children.Add(browseButton);

            return panel;
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
                case StackPanel panel when panel.Children.OfType<TextBox>().FirstOrDefault() is TextBox pathTextBox:
                    pathTextBox.Text = defaultValue;
                    break;
            }
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
                Content = "Start Pixelit integration automatically with profile",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = false
            };

            // Set current autostart status
            var appState = profile.Apps.Values.FirstOrDefault(a => a.App == pixelitApp);
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
                Text = "When enabled, Pixelit integration will automatically start when you launch your dart profile.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 200, 240)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var ipAddress = pixelitIpTextBox.Text?.Trim();
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
                var result = await TestPixelitConnection(ipAddress);
                if (result.Success)
                {
                    isConnected = true;
                    UpdateConnectionStatus("✅ Connected! Pixelit device ready", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    
                    // Update Pixelit endpoint in configuration (PEPS argument)
                    var pixelitEndpointsArg = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "PEPS");
                    if (pixelitEndpointsArg != null)
                    {
                        var url = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                        pixelitEndpointsArg.Value = url;
                        pixelitEndpointsArg.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] Updated PEPS argument: {url}");
                    }

                    // Update display brightness to a reasonable default if not set
                    var briArg = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "BRI");
                    if (briArg != null && string.IsNullOrEmpty(briArg.Value))
                    {
                        briArg.Value = "128";
                        briArg.IsValueChanged = true;
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] Set BRI argument to default: 128");
                    }

                    LoadExistingArgumentValues();
                    configurationPanel.IsVisible = true;
                    configurationPanel.Children.Clear();
                    await CreateConfigurationSections();
                    
                    var autostartCard = CreateAutostartSection();
                    configurationPanel.Children.Add(autostartCard);
                    
                    System.Diagnostics.Debug.WriteLine($"[Pixelit] Configuration panel updated with {configurationPanel.Children.Count} sections");
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
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Connection error: {ex}");
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
            UpdateConnectionStatus("🔍 Scanning network for Pixelit devices (looking for 'PixelIt WebUI' title)...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));

            try
            {
                scanCancellationTokenSource = new CancellationTokenSource();
                var discoveredDevices = await ScanNetworkForPixelitDevices(scanCancellationTokenSource.Token);

                if (discoveredDevices.Count > 0)
                {
                    UpdateConnectionStatus($"✅ Found {discoveredDevices.Count} Pixelit device(s)!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    
                    discoveredDevicesComboBox.IsVisible = true;
                    discoveredDevicesComboBox.ItemsSource = discoveredDevices;

                    if (discoveredDevices.Count == 1)
                    {
                        discoveredDevicesComboBox.SelectedItem = discoveredDevices[0];
                        pixelitIpTextBox.Text = discoveredDevices[0].IpAddress;
                        discoveredDevicesComboBox.IsVisible = false;
                        UpdateConnectionStatus($"✅ Auto-selected: {discoveredDevices[0].Name} at {discoveredDevices[0].IpAddress}", 
                            new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    }
                }
                else
                {
                    UpdateConnectionStatus("⚠️ No Pixelit devices found in network", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                }
            }
            catch (OperationCanceledException)
            {
                UpdateConnectionStatus("🛑 Network scan cancelled", new SolidColorBrush(Color.FromRgb(128, 128, 128)));
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus($"❌ Scan error: {ex.Message}", new SolidColorBrush(Color.FromRgb(220, 53, 69)));
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Network scan error: {ex}");
            }

            isScanning = false;
            scanNetworkButton.IsEnabled = true;
            testConnectionButton.IsEnabled = true;
            scanNetworkButton.Content = "🔍 Scan Network for Pixelit Devices";
        }

        private async Task<List<PixelitDevice>> ScanNetworkForPixelitDevices(CancellationToken cancellationToken)
        {
            return await NetworkDeviceScanner.ScanForPixelitDevices(cancellationToken);
        }

        private async Task<PixelitDevice?> TestPixelitDevice(string ipAddress, CancellationToken cancellationToken)
        {
            try
            {
                // Test if device responds to ping first (quick check)
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 1000);
                
                if (reply.Status != IPStatus.Success) 
                {
                    System.Diagnostics.Debug.WriteLine($"[Pixelit] {ipAddress} - Ping failed: {reply.Status}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[Pixelit] {ipAddress} - Ping successful, testing HTTP endpoints");

                // Test Pixelit specific endpoints - root page is most likely to have the title
                var testUrls = new[]
                {
                    $"http://{ipAddress}/",          // Root page - most likely to have PixelIt WebUI title
                    $"http://{ipAddress}/config",    // Config page
                    $"http://{ipAddress}/api",       // API endpoint
                    $"http://{ipAddress}/status"     // Status page
                };

                using var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(5) };

                foreach (var testUrl in testUrls)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] Testing URL: {testUrl}");
                        
                        using var response = await client.GetAsync(testUrl, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync(cancellationToken);
                            
                            System.Diagnostics.Debug.WriteLine($"[Pixelit] {testUrl} - Response length: {content.Length} chars");
                            
                            // Check if response contains Pixelit-specific content
                            if (IsPixelitDevice(content, response.Headers))
                            {
                                var deviceName = ExtractDeviceName(content) ?? $"Pixelit-{ipAddress.Split('.').Last()}";
                                
                                System.Diagnostics.Debug.WriteLine($"[Pixelit] ✅ Confirmed Pixelit device: {deviceName} at {ipAddress}");
                                
                                return new PixelitDevice
                                {
                                    IpAddress = ipAddress,
                                    Name = deviceName,
                                    Endpoint = testUrl,
                                    ResponseContent = content
                                };
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[Pixelit] {testUrl} - Response doesn't match Pixelit indicators");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Pixelit] {testUrl} - HTTP {response.StatusCode}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] {testUrl} - Request cancelled");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] {testUrl} - Exception: {ex.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[Pixelit] {ipAddress} - No Pixelit device detected on any endpoint");
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit] {ipAddress} - Device test failed: {ex.Message}");
            }

            return null;
        }

        private bool IsPixelitDevice(string content, System.Net.Http.Headers.HttpResponseHeaders headers)
        {
            // Specific check for PixelIt WebUI title tag
            var hasPixelitWebUI = content.Contains("<title>PixelIt WebUI</title>", StringComparison.OrdinalIgnoreCase);
            
            if (hasPixelitWebUI)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Found PixelIt WebUI title tag - confirmed Pixelit device");
                return true;
            }

            // Fallback checks for other potential indicators (less specific)
            var pixelitIndicators = new[]
            {
                "pixelit", "PixelIt", "PIXELIT"
            };

            var contentLower = content.ToLower();
            var hasPixelitIndicator = pixelitIndicators.Any(indicator => contentLower.Contains(indicator.ToLower()));

            if (hasPixelitIndicator)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Found Pixelit indicator in content");
                return true;
            }

            // Check headers for device identification
            var serverHeader = headers.Server?.ToString()?.ToLower();
            var hasPixelitHeader = serverHeader != null && pixelitIndicators.Any(indicator => 
                serverHeader.Contains(indicator.ToLower()));

            if (hasPixelitHeader)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Found Pixelit indicator in server header");
                return true;
            }

            return false;
        }

        private string? ExtractDeviceName(string content)
        {
            try
            {
                // First check for the specific PixelIt WebUI title
                if (content.Contains("<title>PixelIt WebUI</title>", StringComparison.OrdinalIgnoreCase))
                {
                    return "PixelIt WebUI";
                }

                // Try to extract device name from JSON response
                if (content.TrimStart().StartsWith("{"))
                {
                    dynamic json = JsonConvert.DeserializeObject(content);
                    var jsonName = json?.name ?? json?.device_name ?? json?.hostname ?? json?.title;
                    if (jsonName != null)
                    {
                        return jsonName;
                    }
                }

                // Try to extract from any HTML title
                if (content.Contains("<title>"))
                {
                    var titleStart = content.IndexOf("<title>") + 7;
                    var titleEnd = content.IndexOf("</title>", titleStart);
                    if (titleEnd > titleStart)
                    {
                        var title = content.Substring(titleStart, titleEnd - titleStart).Trim();
                        if (!string.IsNullOrEmpty(title))
                        {
                            return title;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Error extracting device name: {ex.Message}");
            }

            return null;
        }

        private void LoadExistingArgumentValues()
        {
            try
            {
                if (pixelitApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing Pixelit argument values:");
                
                foreach (var argument in pixelitApp.Configuration.Arguments)
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
                System.Diagnostics.Debug.WriteLine($"Error loading existing Pixelit argument values: {ex.Message}");
            }
        }

        private async Task LoadExistingConfiguration()
        {
            if (pixelitApp?.Configuration?.Arguments != null)
            {
                var pixelitIpArg = pixelitApp.Configuration.Arguments.FirstOrDefault(a => a.Name == "PEPS");
                if (pixelitIpArg != null && !string.IsNullOrEmpty(pixelitIpArg.Value))
                {
                    var url = pixelitIpArg.Value;
                    if (url.StartsWith("http://"))
                    {
                        url = url.Substring(7);
                    }
                    else if (url.StartsWith("https://"))
                    {
                        url = url.Substring(8);
                    }
                    pixelitIpTextBox.Text = url;
                }
            }
        }

        private void UpdateConnectionStatus(string message, SolidColorBrush color)
        {
            connectionStatusText.Text = message;
            connectionStatusText.Foreground = color;
        }

        private async Task<PixelitConnectionResult> TestPixelitConnection(string ipAddress)
        {
            try
            {
                var url = ipAddress.StartsWith("http") ? ipAddress : $"http://{ipAddress}";
                
                var testUrls = new[]
                {
                    $"{url}/",       // Root page - most likely to have PixelIt WebUI title
                    $"{url}/api",
                    $"{url}/status"
                };

                foreach (var testUrl in testUrls)
                {
                    try
                    {
                        using var response = await httpClient.GetAsync(testUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            
                            if (IsPixelitDevice(content, response.Headers))
                            {
                                return new PixelitConnectionResult
                                {
                                    Success = true,
                                    DeviceType = "Pixelit",
                                    Endpoint = testUrl
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Pixelit] Test URL {testUrl} failed: {ex.Message}");
                        continue;
                    }
                }

                return new PixelitConnectionResult
                {
                    Success = false,
                    ErrorMessage = "Device not responding or not a Pixelit device with 'PixelIt WebUI' title"
                };
            }
            catch (HttpRequestException ex)
            {
                return new PixelitConnectionResult
                {
                    Success = false,
                    ErrorMessage = $"Network error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new PixelitConnectionResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (pixelitApp == null)
            {
                return WizardValidationResult.Success();
            }

            if (!isConnected)
            {
                return WizardValidationResult.Error("Please test the Pixelit connection before proceeding. A successful connection is required to configure Pixelit integration.");
            }

            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            if (pixelitApp == null) return;

            try
            {
                if (pixelitApp.Configuration?.Arguments != null)
                {
                    foreach (var argument in pixelitApp.Configuration.Arguments)
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
                throw new Exception($"Failed to apply Pixelit configuration: {ex.Message}");
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
            pixelitIpTextBox.Text = "192.168.1.117";
            
            discoveredDevicesComboBox.IsVisible = false;
            discoveredDevicesComboBox.ItemsSource = null;
            
            scanNetworkButton.Content = "🔍 Scan Network for Pixelit Devices";
            scanNetworkButton.IsEnabled = true;
            testConnectionButton.IsEnabled = true;
            
            UpdateConnectionStatus("Use network scan to auto-discover or enter IP address manually", 
                new SolidColorBrush(Color.FromRgb(180, 180, 180)));
        }
    }

    // Helper classes for Pixelit connection
    public class PixelitConnectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string Endpoint { get; set; } = "";
    }
}