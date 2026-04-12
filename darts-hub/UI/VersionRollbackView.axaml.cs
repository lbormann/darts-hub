using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using darts_hub.control;
using System;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    public partial class VersionRollbackView : UserControl
    {
        private Configurator? _configurator;
        private string? _selectedVersion;

        public VersionRollbackView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Initializes the view with the configurator and sets up event handlers.
        /// </summary>
        public void Initialize(Configurator configurator)
        {
            _configurator = configurator;

            SetCurrentVersionText();
            SetupSkipVersionCheckBox();
            AttachHandlers();

            _ = LoadVersionsAsync();
        }

        private void SetCurrentVersionText()
        {
            if (this.FindControl<TextBlock>("CurrentVersionText") is { } tb)
                tb.Text = $"Current version: {Updater.version}";
        }

        private void SetupSkipVersionCheckBox()
        {
            var checkBox = this.FindControl<CheckBox>("SkipVersionCheckBox");
            var infoText = this.FindControl<TextBlock>("SkipVersionInfo");
            if (checkBox == null || _configurator == null) return;

            var skipped = _configurator.Settings.SkippedVersion;
            bool hasSkipped = !string.IsNullOrEmpty(skipped);

            checkBox.IsChecked = hasSkipped;
            checkBox.Content = hasSkipped
                ? $"Skip update notification up to version {skipped}"
                : "Skip update notification for the next available version";

            if (infoText != null)
            {
                infoText.IsVisible = hasSkipped;
                infoText.Text = hasSkipped
                    ? $"Updates up to version {skipped} will be suppressed. A newer version will show the update dialog again. This resets automatically after a successful update."
                    : string.Empty;
            }
        }

        private void AttachHandlers()
        {
            if (this.FindControl<ComboBox>("VersionComboBox") is { } combo)
                combo.SelectionChanged += OnVersionSelectionChanged;

            if (this.FindControl<Button>("RollbackButton") is { } rollbackButton)
                rollbackButton.Click += OnRollbackClick;

            if (this.FindControl<CheckBox>("SkipVersionCheckBox") is { } checkBox)
            {
                checkBox.Checked += OnSkipVersionChanged;
                checkBox.Unchecked += OnSkipVersionChanged;
            }
        }

        private async Task LoadVersionsAsync()
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            var combo = this.FindControl<ComboBox>("VersionComboBox");
            var rollbackButton = this.FindControl<Button>("RollbackButton");

            if (combo == null) return;

            try
            {
                if (statusText != null)
                    statusText.Text = "Loading available versions...";

                var versions = await Updater.FetchAvailableVersionsAsync();

                if (versions.Count == 0)
                {
                    if (statusText != null)
                        statusText.Text = "No previous versions available for rollback.";
                    return;
                }

                combo.Items.Clear();
                foreach (var v in versions)
                {
                    combo.Items.Add(new ComboBoxItem { Content = v, Tag = v });
                }

                combo.SelectedIndex = 0;

                if (statusText != null)
                    statusText.Text = $"{versions.Count} version(s) available.";
            }
            catch (Exception ex)
            {
                if (statusText != null)
                    statusText.Text = $"Failed to load versions: {ex.Message}";
                if (rollbackButton != null)
                    rollbackButton.IsEnabled = false;
            }
        }

        private void OnVersionSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var combo = this.FindControl<ComboBox>("VersionComboBox");
            var rollbackButton = this.FindControl<Button>("RollbackButton");

            if (combo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _selectedVersion = tag;
                if (rollbackButton != null)
                    rollbackButton.IsEnabled = true;
            }
            else
            {
                _selectedVersion = null;
                if (rollbackButton != null)
                    rollbackButton.IsEnabled = false;
            }
        }

        private void OnSkipVersionChanged(object? sender, RoutedEventArgs e)
        {
            if (_configurator == null) return;

            var checkBox = this.FindControl<CheckBox>("SkipVersionCheckBox");
            var infoText = this.FindControl<TextBlock>("SkipVersionInfo");
            if (checkBox == null) return;

            if (checkBox.IsChecked == true)
            {
                // Determine the next higher version to skip:
                // Use the first item in the dropdown (newest available version that is higher than current)
                var combo = this.FindControl<ComboBox>("VersionComboBox");
                string versionToSkip = string.Empty;

                if (combo?.Items.Count > 0 && combo.Items[0] is ComboBoxItem firstItem && firstItem.Tag is string firstTag)
                {
                    versionToSkip = firstTag;
                }
                else if (!string.IsNullOrEmpty(Updater.LatestFoundVersion))
                {
                    versionToSkip = Updater.LatestFoundVersion;
                }

                _configurator.Settings.SkippedVersion = versionToSkip;
                _configurator.SaveSettings();
                Updater.SkippedVersion = versionToSkip;

                checkBox.Content = !string.IsNullOrEmpty(versionToSkip)
                    ? $"Skip update notification up to version {versionToSkip}"
                    : "Skip update notification for the next available version";

                if (infoText != null && !string.IsNullOrEmpty(versionToSkip))
                {
                    infoText.IsVisible = true;
                    infoText.Text = $"Updates up to version {versionToSkip} will be suppressed. A newer version will show the update dialog again. This resets automatically after a successful update.";
                }
            }
            else
            {
                _configurator.Settings.SkippedVersion = string.Empty;
                _configurator.SaveSettings();
                Updater.SkippedVersion = string.Empty;

                checkBox.Content = "Skip update notification for the next available version";

                if (infoText != null)
                {
                    infoText.IsVisible = false;
                    infoText.Text = string.Empty;
                }
            }
        }

        /// <summary>
        /// Raised when the user confirms a rollback. The parent (MainWindow) subscribes to handle confirmation and download.
        /// </summary>
        public event EventHandler<string>? RollbackRequested;

        private void OnRollbackClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedVersion))
                return;

            RollbackRequested?.Invoke(this, _selectedVersion);
        }
    }
}
