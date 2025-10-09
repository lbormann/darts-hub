using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using darts_hub.model;
using darts_hub.control.wizard.pixelit;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Avalonia.Interactivity;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading;

namespace darts_hub.control.wizard
{
    /// <summary>
    /// Specialized wizard step for Pixelit configuration with connection testing and guided setup
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

        // Guided configuration steps
        private StackPanel guidedConfigPanel;
        private PixelitEssentialSettingsStep essentialSettingsStep;
        private PixelitGameAnimationsStep gameAnimationsStep;
        private PixelitPlayerAnimationsStep playerAnimationsStep;

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

            // Initialize guided configuration steps
            InitializeGuidedSteps();
        }

        private void InitializeGuidedSteps()
        {
            if (pixelitApp == null) return;

            essentialSettingsStep = new PixelitEssentialSettingsStep(pixelitApp, wizardConfig, argumentControls);

            gameAnimationsStep = new PixelitGameAnimationsStep(pixelitApp, wizardConfig, argumentControls,
                onGameAnimationsSelected: () => ShowNextStep("PlayerAnimationsCard"),
                onGameAnimationsSkipped: () => ShowNextStep("PlayerAnimationsCard"));

            playerAnimationsStep = new PixelitPlayerAnimationsStep(pixelitApp, wizardConfig, argumentControls,
                onPlayerAnimationsSelected: CompleteGuidedSetup,
                onPlayerAnimationsSkipped: CompleteGuidedSetup);
        }

        public async Task<Control> CreateContent()
        {
            // ⭐ Always create a NEW main panel to avoid "already has a visual parent" errors
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

            // Header - always create new
            var header = CreateHeader();
            mainPanel.Children.Add(header);

            // Connection Test Panel - always create new
            connectionTestPanel = CreateConnectionTestPanel();
            mainPanel.Children.Add(connectionTestPanel);

            // Configuration Panel (initially hidden) - always create new
            configurationPanel = new StackPanel { Spacing = 20, IsVisible = false };
            mainPanel.Children.Add(configurationPanel);

            // Load existing IP if available
            LoadExistingConfiguration();

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
                Text = "First, we'll connect to your Pixelit device, then configure the display settings and animations.",
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

        private void LoadExistingConfiguration()
        {
            if (pixelitApp?.Configuration?.Arguments != null)
            {
                var pixelitIpArg = pixelitApp.Configuration.Arguments.FirstOrDefault(a => a.Name == "PEPS");
                if (pixelitIpArg != null && !string.IsNullOrEmpty(pixelitIpArg.Value))
                {
                    // Argument contains IP without http://, so use it directly
                    var cleanIpAddress = pixelitIpArg.Value;
                    
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
                    
                    pixelitIpTextBox.Text = cleanIpAddress;
                }
            }
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
                    
                    // Update Pixelit endpoint in configuration
                    UpdatePixelitConfiguration(ipAddress);
                    
                    // Load existing argument values
                    LoadExistingArgumentValues();
                    
                    // Show configuration panel with guided setup
                    configurationPanel.IsVisible = true;
                    configurationPanel.Children.Clear();
                    await CreateGuidedConfiguration();
                    
                    System.Diagnostics.Debug.WriteLine($"[Pixelit] Guided configuration panel created");
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

        private async Task CreateGuidedConfiguration()
        {
            // ⭐ Always create a completely NEW panel to avoid parent conflicts
            guidedConfigPanel = new StackPanel { Spacing = 20 };

            // Step 1: Essential Settings
            var essentialCard = essentialSettingsStep.CreateEssentialSettingsCard();
            guidedConfigPanel.Children.Add(essentialCard);

            // Step 2: Game animations question
            var gameAnimationsCard = gameAnimationsStep.CreateGameAnimationsQuestionCard();
            guidedConfigPanel.Children.Add(gameAnimationsCard);

            // Step 3: Player animations question (initially hidden)
            var playerAnimationsCard = playerAnimationsStep.CreatePlayerAnimationsQuestionCard();
            playerAnimationsCard.IsVisible = false;
            guidedConfigPanel.Children.Add(playerAnimationsCard);

            // Add autostart section
            var autostartCard = CreateAutostartSection();
            guidedConfigPanel.Children.Add(autostartCard);

            // Add to main configuration panel only if not already added
            if (!configurationPanel.Children.Contains(guidedConfigPanel))
            {
                configurationPanel.Children.Add(guidedConfigPanel);
            }
        }

        private void ShowNextStep(string stepName)
        {
            var stepCard = guidedConfigPanel.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Name == stepName);
            
            // ⭐ Only show if not already visible to prevent multiple triggers
            if (stepCard != null && !stepCard.IsVisible)
            {
                stepCard.IsVisible = true;
            }
        }

        private void CompleteGuidedSetup()
        {
            var completionStep = new PixelitCompletionStep(
                gameAnimationsStep.ShowGameAnimations,
                playerAnimationsStep.ShowPlayerAnimations);

            var completionCard = completionStep.CreateCompletionCard();
            guidedConfigPanel.Children.Add(completionCard);
        }

        private void UpdatePixelitConfiguration(string ipAddress)
        {
            // Update Pixelit endpoint in configuration (PEPS argument)
            var pixelitEndpointsArg = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "PEPS");
            if (pixelitEndpointsArg != null)
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
                
                pixelitEndpointsArg.Value = cleanIpAddress;
                pixelitEndpointsArg.IsValueChanged = true;
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Updated PEPS argument: {cleanIpAddress} (without http://)");
            }

            // Update display brightness to a reasonable default if not set
            var briArg = pixelitApp.Configuration?.Arguments?.FirstOrDefault(a => a.Name == "BRI");
            if (briArg != null && string.IsNullOrEmpty(briArg.Value))
            {
                briArg.Value = "128";
                briArg.IsValueChanged = true;
                System.Diagnostics.Debug.WriteLine($"[Pixelit] Set BRI argument to default: 128");
            }
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

        private void UpdateConnectionStatus(string message, SolidColorBrush color)
        {
            connectionStatusText.Text = message;
            connectionStatusText.Foreground = color;
        }

        public async Task<WizardValidationResult> ValidateStep()
        {
            if (pixelitApp == null)
            {
                return WizardValidationResult.Success();
            }

            // ⭐ Pixelit connection test is optional - allow skipping even without connection
            // Users can configure Pixelit later if they want
            if (!isConnected)
            {
                // Don't force connection - allow skipping
                System.Diagnostics.Debug.WriteLine("[Pixelit] Pixelit connection not tested, but allowing skip");
                return WizardValidationResult.Success();
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