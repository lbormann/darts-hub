using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using System;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Wizard step for configuring the darts-caller application with enhanced UI
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

        public string Title => "Configure Darts-Caller";
        public string Description => "Set up the core dart recognition system";
        public string IconName => "darts";
        public bool CanSkip => false; // Caller is essential

        public void Initialize(Profile profile, ProfileManager profileManager, Configurator configurator)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.configurator = configurator;
            this.readmeParser = new ReadmeParser();
            this.argumentControls = new Dictionary<string, Control>();
            this.argumentDescriptions = new Dictionary<string, string>();
            this.wizardConfig = WizardArgumentsConfig.Instance;
            
            // Find the darts-caller app
            callerApp = profile.Apps.Values.FirstOrDefault(a => 
                a.App.CustomName.ToLower().Contains("caller"))?.App;
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
                return CreateNoCallerMessage();
            }

            // Load argument descriptions
            await LoadArgumentDescriptions();

            // Header
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Create enhanced configuration sections
            await CreateConfigurationSections(mainPanel);

            // Enable caller for autostart
            var autostartSection = CreateAutostartSection();
            mainPanel.Children.Add(autostartSection);

            return mainPanel;
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
                System.Diagnostics.Debug.WriteLine($"Failed to load caller argument descriptions: {ex.Message}");
                argumentDescriptions = new Dictionary<string, string>();
            }
        }

        private Control CreateNoCallerMessage()
        {
            var panel = new StackPanel
            {
                Spacing = 20,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            panel.Children.Add(new TextBlock
            {
                Text = "⚠️ Darts-Caller Not Found",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "The darts-caller application is required for dart recognition but was not found in the current profile. Please ensure darts-caller is installed and configured in your profile.",
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
                Text = "🎯 Darts-Caller Configuration",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "Configure the core dart recognition system. These settings are essential for proper dart detection and game management.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            return panel;
        }

        private async Task CreateConfigurationSections(StackPanel mainPanel)
        {
            if (callerApp?.Configuration?.Arguments == null) 
            {
                System.Diagnostics.Debug.WriteLine("No caller arguments found");
                return;
            }

            var extensionConfig = wizardConfig.GetExtensionConfig("darts-caller");
            if (extensionConfig?.Sections == null) 
            {
                System.Diagnostics.Debug.WriteLine("No caller extension config found for 'darts-caller'");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Creating {extensionConfig.Sections.Count} caller sections");

            // Load existing argument values
            LoadExistingArgumentValues();

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
                    var argument = callerApp.Configuration.Arguments.FirstOrDefault(a => 
                        a.Name.Equals(argumentName, StringComparison.OrdinalIgnoreCase));
                    
                    if (argument != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"    Creating control for caller argument: {argument.Name} = '{argument.Value}'");
                        var argumentControl = await CreateEnhancedArgumentControl(argument);
                        if (argumentControl != null)
                        {
                            argumentsPanel.Children.Add(argumentControl);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"    Caller argument '{argumentName}' not found in app configuration");
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
                mainPanel.Children.Add(sectionCard);
                
                System.Diagnostics.Debug.WriteLine($"  Added section '{sectionName}' with {argumentsPanel.Children.Count} controls");
            }
            
            System.Diagnostics.Debug.WriteLine($"Total caller configuration sections created: {extensionConfig.Sections.Count}");
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

            // Fallback descriptions for common caller arguments
            return argument.Name.ToLower() switch
            {
                "u" => "Your autodarts.io email address for authentication",
                "p" => "Your autodarts.io password for authentication",
                "b" => "Your unique autodarts board ID from your autodarts profile",
                "m" => "Path to the media/sounds directory for voice announcements",
                "ms" => "Optional path to shared media directory for additional sounds",
                "c" => "Specific caller voice to use (leave empty for random selection)",
                "v" => "Volume level for voice announcements (0.0 = mute, 1.0 = maximum)",
                "r" => "Random caller selection mode (0=disabled, 1=per game, 2=per leg)",
                "rl" => "Language for random caller selection",
                "rg" => "Gender preference for random caller selection",
                "e" => "Call out every dart score (0=disabled, 1=score only, 2=with total, 3=advanced)",
                "ets" => "Include total score when calling every dart",
                "ccp" => "Announce current player before their turn",
                "cba" => "Enable voice announcements for bot/AI actions",
                "pcc" => "Minimum score to announce possible checkout opportunities",
                "pccyo" => "Only announce checkout opportunities for yourself",
                "a" => "Volume level for ambient background sounds",
                "aac" => "Play ambient sounds after voice announcements",
                "dl" => "Auto-download voice packs (0=disabled, 1-100=quality level)",
                "dlla" => "Language for downloaded voice packs",
                "dln" => "Specific caller name for downloads",
                "rovp" => "Remove old voice packs when downloading new ones",
                "bav" => "Volume level for background audio during games",
                "lpb" => "Enable local audio playback instead of streaming",
                "webdh" => "Disable HTTPS for web caller interface",
                "hp" => "Port number for the web interface",
                "deb" => "Enable debug logging for troubleshooting",
                "cc" => "Enable SSL certificate checking",
                "crl" => "Enable real-life caller mode for physical games",
                _ => $"Caller configuration setting: {argument.NameHuman}"
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
                HorizontalAlignment = HorizontalAlignment.Stretch,
                PasswordChar = argument.GetTypeClear() == Argument.TypePassword ? '*' : '\0'
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

            // Set appropriate limits based on argument
            switch (argument.Name.ToLower())
            {
                case "v" or "a" or "bav":
                    numericUpDown.Minimum = 0.0m;
                    numericUpDown.Maximum = 1.0m;
                    numericUpDown.Increment = 0.1m;
                    break;
                case "hp":
                    numericUpDown.Minimum = 1024;
                    numericUpDown.Maximum = 65535;
                    break;
                case "e":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 3;
                    break;
                case "r":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 2;
                    break;
                case "rl":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 6;
                    break;
                case "rg":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 2;
                    break;
                case "ccp":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 2;
                    break;
                case "dl":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 100;
                    break;
                case "dlla":
                    numericUpDown.Minimum = 0;
                    numericUpDown.Maximum = 6;
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

        private Control CreateFileSelector(Argument argument, bool isFolder)
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
                Content = isFolder ? "Browse..." : "Browse...",
                Padding = new Avalonia.Thickness(15, 8),
                Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 102, 180)),
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

                if (isFolder)
                {
                    var dialog = new OpenFolderDialog();
                    var result = await dialog.ShowAsync(parentWindow);
                    if (!string.IsNullOrEmpty(result))
                    {
                        textBox.Text = result;
                        argument.Value = result;
                        argument.IsValueChanged = true;
                    }
                }
                else
                {
                    var dialog = new OpenFileDialog { AllowMultiple = false };
                    var result = await dialog.ShowAsync(parentWindow);
                    if (result != null && result.Length > 0)
                    {
                        textBox.Text = result[0];
                        argument.Value = result[0];
                        argument.IsValueChanged = true;
                    }
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
                case StackPanel panel when panel.Children.OfType<TextBox>().FirstOrDefault() is TextBox fileTextBox:
                    fileTextBox.Text = defaultValue;
                    break;
            }
        }

        private void LoadExistingArgumentValues()
        {
            try
            {
                if (callerApp?.Configuration?.Arguments == null) return;
                
                System.Diagnostics.Debug.WriteLine("Loading existing caller argument values:");
                
                foreach (var argument in callerApp.Configuration.Arguments)
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
                System.Diagnostics.Debug.WriteLine($"Error loading existing caller argument values: {ex.Message}");
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
                Content = "Start darts-caller automatically with profile",
                FontSize = 13,
                Foreground = Brushes.White,
                IsChecked = true,
                IsEnabled = false // Caller is always enabled since it's essential
            };

            content.Children.Add(autostartCheckBox);

            content.Children.Add(new TextBlock
            {
                Text = "Darts-caller will automatically start when you launch your dart profile since it's essential for dart recognition.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 240, 200)),
                TextWrapping = TextWrapping.Wrap
            });

            card.Child = content;
            return card;
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (callerApp == null)
            {
                return WizardValidationResult.Error("Darts-Caller application is required but not found.");
            }

            // Validate required arguments
            if (callerApp.Configuration?.Arguments != null)
            {
                foreach (var argument in callerApp.Configuration.Arguments.Where(a => a.Required))
                {
                    if (string.IsNullOrWhiteSpace(argument.Value))
                    {
                        return WizardValidationResult.Error($"'{argument.NameHuman}' is required but not set.");
                    }
                }

                // Additional validation for specific arguments
                var portArg = callerApp.Configuration.Arguments.FirstOrDefault(a => a.Name == "HP");
                if (portArg != null && !string.IsNullOrEmpty(portArg.Value))
                {
                    if (int.TryParse(portArg.Value, out int port))
                    {
                        if (port < 1024 || port > 65535)
                        {
                            return WizardValidationResult.Error("Host port must be between 1024 and 65535.");
                        }
                    }
                }
            }

            return WizardValidationResult.Success();
        }

        public async Task ApplyConfiguration()
        {
            if (callerApp == null) return;

            try
            {
                // Enable caller for autostart (it's essential)
                var callerAppState = profile.Apps.Values.FirstOrDefault(a => a.App == callerApp);
                if (callerAppState != null)
                {
                    callerAppState.TaggedForStart = true;
                }
                
                // Configuration changes are already applied through the input controls
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply caller configuration: {ex.Message}");
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
            if (callerApp?.Configuration?.Arguments != null)
            {
                foreach (var argument in callerApp.Configuration.Arguments)
                {
                    if (argumentControls.TryGetValue(argument.Name, out var control))
                    {
                        ResetArgumentToDefault(argument, control);
                    }
                }
            }
        }
    }
}