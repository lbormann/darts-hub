using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using darts_hub.control;
using darts_hub.model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace darts_hub.UI
{
    /// <summary>
    /// View for managing application-wide settings such as update behavior, UI mode, and quick setups.
    /// </summary>
    public partial class AppSettingsView : UserControl
    {
        private Configurator? configurator;

        public AppSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Binds this view to the configurator and sets up initial state.
        /// </summary>
        public void Initialize(Configurator configurator)
        {
            this.configurator = configurator;

            var skipUpdate = this.FindControl<CheckBox>("SkipUpdateConfirmationCheckBox");
            var betaTester = this.FindControl<CheckBox>("BetaTesterCheckBox");
            var newSettings = this.FindControl<CheckBox>("NewSettingsModeCheckBox");
            var version = this.FindControl<TextBlock>("AppVersionText");
            var splashCountdown = this.FindControl<NumericUpDown>("SplashCountdownNumeric");

            if (skipUpdate != null)
            {
                skipUpdate.IsChecked = configurator.Settings.SkipUpdateConfirmation;
                skipUpdate.Checked += OnSkipUpdateChanged;
                skipUpdate.Unchecked += OnSkipUpdateChanged;
            }

            if (betaTester != null)
            {
                betaTester.IsChecked = configurator.Settings.IsBetaTester;
                betaTester.Checked += OnBetaTesterChanged;
                betaTester.Unchecked += OnBetaTesterChanged;
            }

            if (newSettings != null)
            {
                newSettings.IsChecked = configurator.Settings.NewSettingsMode;
                newSettings.Checked += OnNewSettingsModeChanged;
                newSettings.Unchecked += OnNewSettingsModeChanged;
            }

            if (splashCountdown != null)
            {
                splashCountdown.Value = configurator.Settings.SplashCountdownSeconds;
                splashCountdown.ValueChanged += OnSplashCountdownChanged;
            }

            if (version != null)
            {
                version.Text = Updater.version;
            }

            InitializeMonitorSettings();
            InitializeWledCloseSettings();
        }

        #region Monitor Settings

        private void InitializeMonitorSettings()
        {
            var useMonitorCb = this.FindControl<CheckBox>("UseSpecificMonitorCheckBox");
            var monitorPanel = this.FindControl<StackPanel>("MonitorSelectionPanel");
            var monitorCombo = this.FindControl<ComboBox>("MonitorComboBox");
            var moveButton = this.FindControl<Button>("MoveToMonitorButton");

            if (useMonitorCb == null || monitorPanel == null || monitorCombo == null || moveButton == null)
                return;

            PopulateMonitorDropdown(monitorCombo);

            useMonitorCb.IsChecked = configurator!.Settings.UseSpecificMonitor;
            monitorPanel.IsVisible = configurator.Settings.UseSpecificMonitor;

            useMonitorCb.Checked += (_, _) => OnUseSpecificMonitorChanged(true, monitorPanel, monitorCombo);
            useMonitorCb.Unchecked += (_, _) => OnUseSpecificMonitorChanged(false, monitorPanel, monitorCombo);
            monitorCombo.SelectionChanged += OnMonitorSelectionChanged;
            moveButton.Click += OnMoveToMonitorClick;
        }

        /// <summary>
        /// Populates the monitor dropdown with all available screens.
        /// </summary>
        private void PopulateMonitorDropdown(ComboBox comboBox)
        {
            comboBox.Items.Clear();

            var mainWindow = GetMainWindow();
            if (mainWindow == null)
                return;

            var screens = mainWindow.Screens.All;
            for (int i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                var primary = screen.IsPrimary ? " (Primary)" : "";
                var item = new ComboBoxItem
                {
                    Content = $"Monitor {i + 1}: {screen.Bounds.Width}x{screen.Bounds.Height}{primary}",
                    Tag = i
                };
                comboBox.Items.Add(item);
            }

            var savedIndex = configurator!.Settings.PreferredMonitorIndex;
            if (savedIndex >= 0 && savedIndex < screens.Count)
                comboBox.SelectedIndex = savedIndex;
            else if (screens.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private void OnUseSpecificMonitorChanged(bool enabled, StackPanel panel, ComboBox comboBox)
        {
            if (configurator == null) return;

            panel.IsVisible = enabled;
            configurator.Settings.UseSpecificMonitor = enabled;

            if (enabled)
                PopulateMonitorDropdown(comboBox);

            configurator.SaveSettings();
        }

        private void OnMonitorSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (configurator == null || sender is not ComboBox cb) return;
            if (cb.SelectedItem is ComboBoxItem item && item.Tag is int index)
            {
                configurator.Settings.PreferredMonitorIndex = index;
                configurator.SaveSettings();
            }
        }

        /// <summary>
        /// Moves the main window to the center of the selected monitor for testing.
        /// </summary>
        private void OnMoveToMonitorClick(object? sender, RoutedEventArgs e)
        {
            var mainWindow = GetMainWindow();
            if (mainWindow == null || configurator == null) return;

            var monitorCombo = this.FindControl<ComboBox>("MonitorComboBox");
            if (monitorCombo?.SelectedItem is not ComboBoxItem item || item.Tag is not int index)
                return;

            var screens = mainWindow.Screens.All;
            if (index < 0 || index >= screens.Count)
                return;

            MoveWindowToScreen(mainWindow, screens[index]);
        }

        /// <summary>
        /// Moves a window to the center of the given screen.
        /// </summary>
        internal static void MoveWindowToScreen(Window window, Avalonia.Platform.Screen screen)
        {
            var workArea = screen.WorkingArea;
            var x = workArea.X + (workArea.Width - (int)window.Width) / 2;
            var y = workArea.Y + (workArea.Height - (int)window.Height) / 2;
            window.Position = new PixelPoint(x, y);
        }

        #endregion

        #region WLED Close Settings

        private void InitializeWledCloseSettings()
        {
            var listPanel = this.FindControl<StackPanel>("WledDeviceListPanel");
            var addButton = this.FindControl<Button>("WledAddDeviceButton");
            if (listPanel == null || addButton == null || configurator == null)
                return;

            // Render existing devices
            foreach (var device in configurator.Settings.WledOnCloseDevices)
            {
                listPanel.Children.Add(BuildDeviceRow(device));
            }

            addButton.Click += (_, _) =>
            {
                var device = new WledDeviceConfig();
                configurator.Settings.WledOnCloseDevices.Add(device);
                listPanel.Children.Add(BuildDeviceRow(device));
                SaveWledDevices();
            };
        }

        /// <summary>
        /// Builds a single device row with endpoint input, action dropdown, preset input, and remove button.
        /// </summary>
        private Border BuildDeviceRow(WledDeviceConfig device)
        {
            var rowBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10),
            };

            var outerStack = new StackPanel { Spacing = 8 };

            // Row 1: Endpoint + Remove
            var topRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            var endpointInput = new TextBox
            {
                Watermark = "IP address (e.g. 192.168.1.100)",
                Text = device.Endpoint,
                Width = 260,
                FontSize = 13,
            };
            endpointInput.LostFocus += (_, _) =>
            {
                device.Endpoint = endpointInput.Text?.Trim() ?? string.Empty;
                SaveWledDevices();
            };

            var removeButton = new Button
            {
                Content = "\u2716",
                Padding = new Thickness(6, 4),
                Background = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
            };
            removeButton.Click += (_, _) =>
            {
                configurator!.Settings.WledOnCloseDevices.Remove(device);
                var listPanel = this.FindControl<StackPanel>("WledDeviceListPanel");
                listPanel?.Children.Remove(rowBorder);
                SaveWledDevices();
            };

            topRow.Children.Add(endpointInput);
            topRow.Children.Add(removeButton);

            // Row 2: Action dropdown + Preset
            var bottomRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            var actionCombo = new ComboBox { Width = 180, FontSize = 13 };
            actionCombo.Items.Add(new ComboBoxItem { Content = "Turn off", Tag = WledCloseAction.TurnOff });
            actionCombo.Items.Add(new ComboBoxItem { Content = "Activate preset", Tag = WledCloseAction.ActivatePreset });
            actionCombo.SelectedIndex = device.Action == WledCloseAction.ActivatePreset ? 1 : 0;

            bool hasCachedPresets = device.CachedPresets != null && device.CachedPresets.Count > 0;

            var presetInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 250,
                Value = device.PresetId,
                Width = 90,
                FontSize = 13,
                FormatString = "0",
                IsVisible = device.Action == WledCloseAction.ActivatePreset && !hasCachedPresets,
            };

            var fetchPresetsButton = new Button
            {
                Content = "Fetch presets",
                Padding = new Thickness(8, 4),
                Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(4),
                FontSize = 12,
                IsVisible = device.Action == WledCloseAction.ActivatePreset,
            };

            ComboBox? presetCombo = null;

            // If cached presets exist, build the dropdown immediately
            if (hasCachedPresets)
            {
                presetCombo = BuildPresetComboBox(device, device.CachedPresets);
                presetCombo.IsVisible = device.Action == WledCloseAction.ActivatePreset;
            }

            actionCombo.SelectionChanged += (_, _) =>
            {
                if (actionCombo.SelectedItem is ComboBoxItem sel && sel.Tag is WledCloseAction action)
                {
                    device.Action = action;
                    bool showPreset = action == WledCloseAction.ActivatePreset;
                    presetInput.IsVisible = showPreset && presetCombo == null;
                    fetchPresetsButton.IsVisible = showPreset;
                    if (presetCombo != null) presetCombo.IsVisible = showPreset;
                    SaveWledDevices();
                }
            };

            presetInput.ValueChanged += (_, _) =>
            {
                device.PresetId = (int)(presetInput.Value ?? 1);
                SaveWledDevices();
            };

            fetchPresetsButton.Click += async (_, _) =>
            {
                var ep = device.Endpoint;
                if (string.IsNullOrWhiteSpace(ep)) return;

                fetchPresetsButton.IsEnabled = false;
                fetchPresetsButton.Content = "Loading...";

                var presets = await WledShutdownService.QueryPresetsAsync(ep);

                fetchPresetsButton.IsEnabled = true;
                fetchPresetsButton.Content = "Fetch presets";

                if (presets == null || presets.Count == 0) return;

                // Persist fetched presets
                device.CachedPresets = new Dictionary<int, string>(presets);
                SaveWledDevices();

                // Replace numeric input with a ComboBox of real preset names
                presetInput.IsVisible = false;

                if (presetCombo != null)
                {
                    bottomRow.Children.Remove(presetCombo);
                }

                presetCombo = BuildPresetComboBox(device, presets);

                // Insert before the fetch button
                var fetchIdx = bottomRow.Children.IndexOf(fetchPresetsButton);
                bottomRow.Children.Insert(fetchIdx, presetCombo);
            };

            bottomRow.Children.Add(actionCombo);
            bottomRow.Children.Add(presetInput);
            if (presetCombo != null)
            {
                bottomRow.Children.Add(presetCombo);
            }
            bottomRow.Children.Add(fetchPresetsButton);

            outerStack.Children.Add(topRow);
            outerStack.Children.Add(bottomRow);
            rowBorder.Child = outerStack;

            return rowBorder;
        }

        /// <summary>
        /// Builds a preset selection ComboBox from a preset-id-to-name dictionary.
        /// </summary>
        private ComboBox BuildPresetComboBox(WledDeviceConfig device, Dictionary<int, string> presets)
        {
            var combo = new ComboBox { Width = 200, FontSize = 13 };
            int selectedIdx = 0;
            int idx = 0;
            foreach (var kvp in presets.OrderBy(p => p.Key))
            {
                combo.Items.Add(new ComboBoxItem
                {
                    Content = $"{kvp.Key}: {kvp.Value}",
                    Tag = kvp.Key
                });
                if (kvp.Key == device.PresetId)
                    selectedIdx = idx;
                idx++;
            }
            combo.SelectedIndex = selectedIdx;
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is ComboBoxItem pItem && pItem.Tag is int pid)
                {
                    device.PresetId = pid;
                    SaveWledDevices();
                }
            };
            return combo;
        }

        private void SaveWledDevices()
        {
            configurator?.SaveSettings();
        }

        #endregion

        #region Helpers

        private Window? GetMainWindow()
        {
            if (this.VisualRoot is Window w)
                return w;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }

        #endregion

        #region General Settings Handlers

        private void OnSkipUpdateChanged(object? sender, RoutedEventArgs e)
        {
            if (configurator == null || sender is not CheckBox cb) return;
            configurator.Settings.SkipUpdateConfirmation = cb.IsChecked == true;
            configurator.SaveSettings();
        }

        private void OnBetaTesterChanged(object? sender, RoutedEventArgs e)
        {
            if (configurator == null || sender is not CheckBox cb) return;
            configurator.Settings.IsBetaTester = cb.IsChecked == true;
            Updater.IsBetaTester = cb.IsChecked == true;
            configurator.SaveSettings();
        }

        private void OnNewSettingsModeChanged(object? sender, RoutedEventArgs e)
        {
            if (configurator == null || sender is not CheckBox cb) return;
            configurator.Settings.NewSettingsMode = cb.IsChecked == true;
            configurator.SaveSettings();
        }

        private void OnSplashCountdownChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            if (configurator == null || sender is not NumericUpDown nud) return;
            configurator.Settings.SplashCountdownSeconds = (int)(nud.Value ?? 1);
            configurator.SaveSettings();
        }

        #endregion
    }
}
