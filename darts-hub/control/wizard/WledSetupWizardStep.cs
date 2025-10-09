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
            // ⭐ Always create a NEW main panel to avoid "already has a visual parent" errors
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

            // ⭐ WLED connection test is optional - allow skipping even without connection
            // Users can configure WLED later if they want
            if (!isConnected)
            {
                // Don't force connection - allow skipping
                System.Diagnostics.Debug.WriteLine("[WLED] WLED connection not tested, but allowing skip");
                return WizardValidationResult.Success();
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
                    
                    // Ensure WLED extension is available and start it briefly to generate wled_data.json
                    await EnsureWledExtensionAndGenerateData();
                    
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

        /// <summary>
        /// Ensures WLED extension is available and starts it briefly to generate wled_data.json
        /// </summary>
        private async Task EnsureWledExtensionAndGenerateData()
        {
            try
            {
                UpdateConnectionStatus("🔄 Preparing WLED extension to generate configuration data...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                
                // Check if WLED app is available and installed
                if (wledApp == null)
                {
                    System.Diagnostics.Debug.WriteLine("[WLED] WLED app not found in profile");
                    return;
                }

                // Check if it's a downloadable app that needs to be downloaded first
                var downloadableApp = profileManager.AppsDownloadable?.FirstOrDefault(a => a.Name == wledApp.Name);
                if (downloadableApp != null && !downloadableApp.IsInstalled())
                {
                    UpdateConnectionStatus("📥 Downloading WLED extension...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                    System.Diagnostics.Debug.WriteLine("[WLED] WLED extension not installed, downloading...");
                    
                    // Setup event handler for download completion
                    var downloadCompleted = new TaskCompletionSource<bool>();
                    
                    EventHandler<AppEventArgs> onDownloadFinished = null;
                    EventHandler<AppEventArgs> onDownloadFailed = null;
                    
                    onDownloadFinished = (sender, e) =>
                    {
                        downloadableApp.DownloadFinished -= onDownloadFinished;
                        downloadableApp.DownloadFailed -= onDownloadFailed;
                        downloadCompleted.SetResult(true);
                    };
                    
                    onDownloadFailed = (sender, e) =>
                    {
                        downloadableApp.DownloadFinished -= onDownloadFinished;
                        downloadableApp.DownloadFailed -= onDownloadFailed;
                        downloadCompleted.SetResult(false);
                    };
                    
                    downloadableApp.DownloadFinished += onDownloadFinished;
                    downloadableApp.DownloadFailed += onDownloadFailed;
                    
                    // Start download
                    var downloadStarted = downloadableApp.Install();
                    if (downloadStarted)
                    {
                        // Wait for download to complete
                        var downloadSuccess = await downloadCompleted.Task;
                        if (!downloadSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine("[WLED] Failed to download WLED extension");
                            UpdateConnectionStatus("⚠️ Warning: Could not download WLED extension. Please install it manually.", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                            return;
                        }
                        
                        System.Diagnostics.Debug.WriteLine("[WLED] WLED extension downloaded successfully");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[WLED] Failed to start WLED extension download");
                        UpdateConnectionStatus("⚠️ Warning: Could not start WLED extension download. Please install it manually.", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                        return;
                    }
                }
                
                // Check if the app is installed
                if (!wledApp.IsInstalled())
                {
                    System.Diagnostics.Debug.WriteLine("[WLED] WLED extension not available after download attempt");
                    UpdateConnectionStatus("⚠️ Warning: WLED extension not available. Please install it manually.", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                    return;
                }

                // Start WLED extension briefly to generate wled_data.json
                UpdateConnectionStatus("🚀 Starting WLED extension to generate configuration data...", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                System.Diagnostics.Debug.WriteLine("[WLED] Starting WLED extension to generate wled_data.json");
                
                // Prepare runtime arguments with current IP
                var runtimeArgs = new Dictionary<string, string>();
                if (wledApp.Configuration?.Arguments != null)
                {
                    var wepsArg = wledApp.Configuration.Arguments.FirstOrDefault(a => a.Name == "WEPS");
                    if (wepsArg != null && !string.IsNullOrEmpty(wepsArg.Value))
                    {
                        // Use the configured WLED endpoint
                        System.Diagnostics.Debug.WriteLine($"[WLED] Using configured WLED endpoint: {wepsArg.Value}");
                    }
                }
                
                // Start the app
                var startSuccess = wledApp.Run(runtimeArgs);
                if (startSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("[WLED] WLED extension started successfully");
                    
                    // Wait a moment for the app to initialize and generate wled_data.json
                    await Task.Delay(3000);
                    
                    // Stop the app
                    try
                    {
                        wledApp.Close();
                        System.Diagnostics.Debug.WriteLine("[WLED] WLED extension stopped");
                        UpdateConnectionStatus("✅ Configuration data generated successfully!", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WLED] Error stopping WLED extension: {ex.Message}");
                        // Continue anyway, the important part is that it started
                        UpdateConnectionStatus("✅ Configuration data generated (extension may still be running)", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[WLED] Failed to start WLED extension");
                    UpdateConnectionStatus("⚠️ Warning: Could not start WLED extension to generate configuration data", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WLED] Error in EnsureWledExtensionAndGenerateData: {ex.Message}");
                UpdateConnectionStatus("⚠️ Warning: Error preparing WLED extension", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
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
            // ⭐ Always create a completely NEW panel to avoid parent conflicts
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
                Content = "✅ Yes, configure score areas",
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