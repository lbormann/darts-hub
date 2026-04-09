using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using darts_hub.control;
using System;
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
        }

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

        private Window? GetMainWindow()
        {
            if (this.VisualRoot is Window w)
                return w;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;

            return null;
        }

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
    }
}
