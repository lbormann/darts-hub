using Avalonia.Controls;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using darts_hub.control;

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

            if (version != null)
            {
                version.Text = Updater.version;
            }
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

            }
        }
