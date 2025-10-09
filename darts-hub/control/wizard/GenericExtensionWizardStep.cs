using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Avalonia.Controls.Primitives;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Generic wizard step for configuring any darts extension
    /// </summary>
    public class GenericExtensionWizardStep : IWizardStep
    {
        private Profile profile;
        private ProfileManager profileManager;
        private Configurator configurator;
        private AppBase targetApp;
        private ReadmeParser readmeParser;
        private Dictionary<string, string> argumentDescriptions;
        private Dictionary<string, Control> argumentControls;
        private WizardArgumentsConfig wizardConfig;
        private string extensionName;
        private string readmeUrl;

        public string Title { get; private set; }
        public string Description { get; private set; }
        public string IconName { get; private set; }
        public bool CanSkip => true;

        public GenericExtensionWizardStep(string extensionName, string readmeUrl)
        {
            this.extensionName = extensionName;
            this.readmeUrl = readmeUrl;
            this.argumentControls = new Dictionary<string, Control>();
            this.argumentDescriptions = new Dictionary<string, string>();
            this.readmeParser = new ReadmeParser();
            this.wizardConfig = WizardArgumentsConfig.Instance;
            
            SetupStepProperties();
        }

        private void SetupStepProperties()
        {
            switch (extensionName.ToLower())
            {
                case "wled":
                    Title = "Configure WLED Integration";
                    Description = "Set up LED strip control and visual effects";
                    IconName = "darts";
                    break;
                case "pixelit":
                    Title = "Configure Pixelit Display";
                    Description = "Set up smart display for scores and animations";
                    IconName = "darts";
                    break;
                case "voice":
                    Title = "Configure Voice Announcements";
                    Description = "Set up audio feedback and voice announcements";
                    IconName = "darts";
                    break;
                case "gif":
                    Title = "Configure GIF Display";
                    Description = "Set up animated GIF display during games";
                    IconName = "darts";
                    break;
                case "extern":
                    Title = "Configure External Integration";
                    Description = "Set up external services and API connections";
                    IconName = "darts";
                    break;
                default:
                    Title = $"Configure {extensionName}";
                    Description = $"Set up {extensionName} extension";
                    IconName = "darts";
                    break;
            }
        }

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
            
            // Find the target app
            targetApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains(extensionName.ToLower()))?.App;
                
            // Load existing argument values from the app configuration
            if (targetApp?.Configuration?.Arguments != null)
            {
                LoadExistingArgumentValues();
            }
        }

        private void LoadExistingArgumentValues()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Generic] Loading existing values for {extensionName}:");
                
                foreach (var argument in targetApp.Configuration.Arguments)
                {
                    // If argument already has a value, keep it
                    if (!string.IsNullOrEmpty(argument.Value))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Generic]   {argument.Name}: {argument.Value} (existing)");
                        continue;
                    }
                    
                    // Otherwise try to get default value from config
                    var defaultValue = wizardConfig.GetDefaultValue(argument.Name);
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        argument.Value = defaultValue;
                        System.Diagnostics.Debug.WriteLine($"[Generic]   {argument.Name}: {defaultValue} (default)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Generic]   {argument.Name}: (empty)");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Generic] Error loading existing argument values: {ex.Message}");
            }
        }

        public async Task<Control> CreateContent()
        {
            // ⭐ Always create a NEW main panel to avoid "already has a visual parent" errors
            var mainPanel = new StackPanel
            {
                Spacing = 25,
                MaxWidth = 800,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Prevent multiple calls from creating duplicate content
            if (targetApp == null)
            {
                return CreateNotAvailableMessage();
            }

            // Load argument descriptions
            await LoadArgumentDescriptions();

            // Header - always create new
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Configuration sections - always create new
            await CreateConfigurationSections(mainPanel);

            // Add autostart section - always create new
            var autostartCard = CreateAutostartSection();
            mainPanel.Children.Add(autostartCard);

            return mainPanel;
        }

        private async Task LoadArgumentDescriptions()
        {
            try
            {
                if (!string.IsNullOrEmpty(readmeUrl))
                {
                    argumentDescriptions = await readmeParser.GetArgumentsFromReadme(readmeUrl);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Generic] Failed to load {extensionName} argument descriptions: {ex.Message}");
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

            var extensionIcon = GetExtensionIcon();
            panel.Children.Add(new TextBlock
            {
                Text = $"{extensionIcon} {Title} Not Available",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"{extensionName} extension is not available in the current profile. You can skip this step and add {extensionName} support later if needed.",
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

            var extensionIcon = GetExtensionIcon();
            panel.Children.Add(new TextBlock
            {
                Text = $"{extensionIcon} {Title}",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = Description,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private string GetExtensionIcon()
        {
            return extensionName.ToLower() switch
            {
                "wled" => "💡",
                "pixelit" => "📱",
                "voice" => "🗣️",
                "gif" => "🎬",
                "extern" => "🔗",
                _ => "⚙️"
            };
        }

        private async Task CreateConfigurationSections(StackPanel mainPanel)
        {
            if (targetApp?.Configuration?.Arguments == null) 
            {
                System.Diagnostics.Debug.WriteLine($"[Generic] No arguments found for {extensionName}");
                return;
            }

            var extensionConfig = wizardConfig.GetExtensionConfig($"darts-{extensionName.ToLower()}");
            if (extensionConfig?.Sections == null) 
            {
                System.Diagnostics.Debug.WriteLine($"[Generic] No extension config found for 'darts-{extensionName.ToLower()}'");
                System.Diagnostics.Debug.WriteLine($"[Generic] Available extensions: {string.Join(", ", wizardConfig.Extensions.Keys)}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[Generic] Creating {extensionConfig.Sections.Count} sections for {extensionName}");

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
                    Margin = new Avalonia.Thickness(0, 10)
                };

                var sectionContent = new StackPanel { Spacing = 15 };

                // Section Header with expand/collapse
                var headerPanel = CreateSectionHeader(sectionName, sectionConfig.Expanded);
                sectionContent.Children.Add(headerPanel);

                // Section Arguments (initially visible based on expanded state)
                var argumentsPanel = new StackPanel { Spacing = 15, IsVisible = sectionConfig.Expanded };

                System.Diagnostics.Debug.WriteLine($"[Generic]   Section '{sectionName}' with {sectionConfig.Arguments.Count} arguments");

                foreach (var argumentName in sectionConfig.Arguments)
                {
                    var argument = targetApp.Configuration.Arguments.FirstOrDefault(a => 
                        a.Name.Equals(argumentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Generic]     Creating control for argument: {argument.Name} = '{argument.Value}'");
                        var argumentControl = await CreateEnhancedArgumentControl(argument);
                        if (argumentControl != null)
                        {
                            argumentsPanel.Children.Add(argumentControl);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[Generic]     Argument '{argumentName}' not found in app configuration");
                    }
                }

                // Add toggle functionality
                if (headerPanel.Children[0] is Button toggleButton)
                {
                    toggleButton.Click += (s, e) =>
                    {
                        argumentsPanel.IsVisible = !argumentsPanel.IsVisible;
                        toggleButton.Content = argumentsPanel.IsVisible ? "📂" : "📁";
                    };
                }

                sectionContent.Children.Add(argumentsPanel);
                sectionCard.Child = sectionContent;
                mainPanel.Children.Add(sectionCard);
            }
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
                Content = expanded ? "📂" : "📁",
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

            // Fallback descriptions based on extension type
            return GetFallbackDescription(argument);
        }

        private string GetFallbackDescription(Argument argument)
        {
            var argName = argument.Name.ToLower();
            
            return extensionName.ToLower() switch
            {
                "wled" => argName switch
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
                },
                "pixelit" => argName switch
                {
                    "peps" => "IP address and port of your Pixelit device",
                    "tp" => "Path to folder containing Pixelit animation templates",
                    "bri" => "Display brightness level (1-255)",
                    "as" => "Animation shown when the application starts",
                    "ide" => "Animation shown when idle/waiting",
                    _ => $"Pixelit configuration setting: {argument.NameHuman}"
                },
                "voice" => argName switch
                {
                    "con" => "Connection string to darts-caller service",
                    "mp" => "Path to speech recognition model files",
                    "l" => "Language setting for voice recognition (0=EN, 1=DE, 2=NL)",
                    _ when argName.StartsWith("k") => $"Voice command keywords for: {argument.NameHuman}",
                    _ => $"Voice recognition setting: {argument.NameHuman}"
                },
                "gif" => argName switch
                {
                    "mp" => "Path to folder containing GIF/image files",
                    "con" => "Connection string to darts-caller service",
                    "web" => "Enable web-based GIF display",
                    "webp" => "Port for web-based GIF display",
                    _ => $"GIF display setting: {argument.NameHuman}"
                },
                "extern" => argName switch
                {
                    "connection" => "Connection string to darts-caller service",
                    "browser_path" => "Full path to your web browser executable",
                    "autodarts_user" => "Your Autodarts username/email",
                    "autodarts_password" => "Your Autodarts password",
                    "autodarts_board_id" => "Your Autodarts board ID",
                    "extern_platform" => "External platform to connect to (lidarts, nakka, dartboards)",
                    _ => $"External integration setting: {argument.NameHuman}"
                },
                _ => $"Configuration setting for {extensionName}: {argument.NameHuman}"
            };
        }

        private Control CreateInputControl(Argument argument)
        {
            string type = argument.GetTypeClear();

            // Use enhanced controls for WLED effect parameters
            if (extensionName.ToLower().Contains("wled") && WledSettings.IsEffectParameter(argument))
            {
                return WledSettings.CreateAdvancedEffectParameterControl(argument, 
                    () => { argument.IsValueChanged = true; }, targetApp);
            }

            return type switch
            {
                Argument.TypeString or Argument.TypePassword => CreateTextBox(argument),
                Argument.TypeBool => CreateCheckBox(argument),
                Argument.TypeInt => CreateNumericUpDown(argument, false),
                Argument.TypeFloat => CreateNumericUpDown(argument, true),
                Argument.TypeFile => CreateFileSelector(argument, false),
                Argument.TypePath => CreateFileSelector(argument, true),
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

            if (argument.Type.ToLower().Contains("password"))
            {
                textBox.PasswordChar = '*';
                textBox.RevealPassword = false;
            }

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
                argument.Value = argument.ValueMapping?.ContainsKey("True") == true ? 
                    argument.ValueMapping["True"] : "True";
                argument.IsValueChanged = true;
            };

            checkBox.Unchecked += (s, e) =>
            {
                argument.Value = argument.ValueMapping?.ContainsKey("False") == true ? 
                    argument.ValueMapping["False"] : "False";
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

            // Set appropriate limits FIRST before setting value
            SetNumericLimits(numericUpDown, argument, isFloat);

            // Set value AFTER limits are set
            if (isFloat)
            {
                if (double.TryParse(argument.Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                {
                    numericUpDown.Value = (decimal)doubleVal;
                }
                else
                {
                    numericUpDown.Value = 0;
                }
            }
            else
            {
                if (int.TryParse(argument.Value, out var intVal))
                {
                    numericUpDown.Value = intVal;
                }
                else
                {
                    numericUpDown.Value = 0;
                }
            }

            numericUpDown.ValueChanged += (s, e) =>
            {
                argument.Value = numericUpDown.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
                argument.IsValueChanged = true;
            };

            return numericUpDown;
        }

        private void SetNumericLimits(NumericUpDown control, Argument argument, bool isFloat)
        {
            var argName = argument.Name.ToLower();
            
            switch (argName)
            {
                case "bri" or "brightness":
                    control.Minimum = 1;
                    control.Maximum = 255;
                    break;
                case "hp" or "port" or "webp":
                    control.Minimum = 1024;
                    control.Maximum = 65535;
                    break;
                case "v" or "volume":
                    control.Minimum = 0;
                    control.Maximum = 1;
                    control.Increment = 0.1m;
                    break;
                case "l" when extensionName.ToLower() == "voice":
                    control.Minimum = 0;
                    control.Maximum = 2;
                    break;
                case "hfo":
                    control.Minimum = 2;
                    control.Maximum = 170;
                    break;
                case "du":
                    control.Minimum = 0;
                    control.Maximum = 10;
                    break;
                default:
                    // Use reasonable default ranges instead of extreme values
                    control.Minimum = isFloat ? -999.9m : -999;
                    control.Maximum = isFloat ? 999.9m : 999;
                    break;
            }
        }

        private Control CreateFileSelector(Argument argument, bool isDirectory)
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
                Width = 300
            };

            var browseButton = new Button
            {
                Content = "📁",
                Width = 35,
                Background = new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                CornerRadius = new Avalonia.CornerRadius(4)
            };

            textBox.TextChanged += (s, e) =>
            {
                argument.Value = textBox.Text;
                argument.IsValueChanged = true;
            };

            browseButton.Click += async (s, e) =>
            {
                // TODO: Implement file/directory selection dialog
                // This would require adding file dialog functionality
            };

            panel.Children.Add(textBox);
            panel.Children.Add(browseButton);
            return panel;
        }

        private Control CreateClearButton(Argument argument, Control inputControl)
        {
            var clearButton = new Button
            {
                Content = "✖",
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
            var defaultValue = wizardConfig.GetDefaultValue(argument.Name) ?? "";
            argument.Value = defaultValue;
            argument.IsValueChanged = true;

            switch (inputControl)
            {
                case TextBox textBox:
                    textBox.Text = defaultValue;
                    break;
                case CheckBox checkBox:
                    checkBox.IsChecked = defaultValue.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                                        defaultValue == "1";
                    break;
                case NumericUpDown numericUpDown:
                    if (decimal.TryParse(defaultValue, out var decimalVal))
                        numericUpDown.Value = decimalVal;
                    else
                        numericUpDown.Value = 0;
                    break;
                case StackPanel stackPanel when stackPanel.Children[0] is TextBox fileTextBox:
                    fileTextBox.Text = defaultValue;
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
                Content = $"Start {extensionName} extension automatically with profile",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = false
            };

            // Set current autostart status
            var appState = profile.Apps.Values.FirstOrDefault(a => a.App == targetApp);
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
                Text = $"When enabled, {extensionName} extension will automatically start when you launch your dart profile.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 200)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (targetApp == null)
            {
                return WizardValidationResult.Success(); // Skip if not available
            }

            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            if (targetApp == null) return;

            try
            {
                // Configuration changes are already applied through the input controls
                if (targetApp.Configuration?.Arguments != null)
                {
                    foreach (var argument in targetApp.Configuration.Arguments)
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
                throw new Exception($"Failed to apply {extensionName} configuration: {ex.Message}");
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
                var argument = targetApp?.Configuration?.Arguments?.FirstOrDefault(a => a.Name == kvp.Key);
                if (argument != null)
                {
                    ResetArgumentToDefault(argument, kvp.Value);
                }
            }
        }
    }

    // Specific extension steps using the generic base
    public class WledConfigWizardStep : GenericExtensionWizardStep
    {
        public WledConfigWizardStep() : base("wled", "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md")
        {
        }
    }

    public class PixelitConfigWizardStep : GenericExtensionWizardStep
    {
        public PixelitConfigWizardStep() : base("pixelit", "https://raw.githubusercontent.com/lbormann/darts-pixelit/refs/heads/main/README.md")
        {
        }
    }

    public class VoiceConfigWizardStep : GenericExtensionWizardStep
    {
        public VoiceConfigWizardStep() : base("voice", "https://raw.githubusercontent.com/lbormann/darts-voice/refs/heads/main/README.md")
        {
        }
    }

    public class GifConfigWizardStep : GenericExtensionWizardStep
    {
        public GifConfigWizardStep() : base("gif", "https://raw.githubusercontent.com/lbormann/darts-gif/refs/heads/main/README.md")
        {
        }
    }

    public class ExternConfigWizardStep : GenericExtensionWizardStep
    {
        public ExternConfigWizardStep() : base("extern", "https://raw.githubusercontent.com/lbormann/darts-extern/refs/heads/master/README.md")
        {
        }
    }
}