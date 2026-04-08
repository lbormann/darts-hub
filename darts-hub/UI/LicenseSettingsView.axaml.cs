using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using darts_hub.control;
using System;
using System.Diagnostics;

namespace darts_hub.UI
{
    /// <summary>
    /// View for managing the license key: input, validation, and requesting a new license.
    /// </summary>
    public partial class LicenseSettingsView : UserControl
    {
        private const string ExperienceCheckUrl = "https://license.darts-hub.i3ull3t.de/experience.html";

        private LicenseManager? licenseManager;

        public LicenseSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Binds this view to a LicenseManager and refreshes the UI.
        /// </summary>
        public void Initialize(LicenseManager manager)
        {
            licenseManager = manager;
            licenseManager.StatusChanged += OnLicenseStatusChanged;

            // Pre-fill the stored license key
            var input = this.FindControl<TextBox>("LicenseKeyInput");
            if (input != null && !string.IsNullOrWhiteSpace(manager.StoredLicenseKey))
            {
                input.Text = manager.StoredLicenseKey;
            }

            RefreshStatusUI();
        }

        private void OnLicenseStatusChanged(object? sender, LicenseStatusChangedEventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(RefreshStatusUI);
        }

        private void RefreshStatusUI()
        {
            if (licenseManager == null) return;

            var indicator = this.FindControl<Border>("StatusIndicator");
            var statusText = this.FindControl<TextBlock>("StatusText");
            var statusDetail = this.FindControl<TextBlock>("StatusDetail");
            var featuresPanel = this.FindControl<Border>("FeaturesPanel");
            var featuresList = this.FindControl<StackPanel>("FeaturesList");

            if (indicator == null || statusText == null) return;

            switch (licenseManager.CurrentStatus)
            {
                case LicenseStatus.Valid:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    statusText.Text = "License is valid";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(40, 167, 69));
                    break;

                case LicenseStatus.Expired:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    statusText.Text = "License expired";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    break;

                case LicenseStatus.Blocked:
                case LicenseStatus.Revoked:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    statusText.Text = licenseManager.CurrentStatus == LicenseStatus.Blocked
                        ? "License blocked"
                        : "License revoked";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    break;

                case LicenseStatus.Invalid:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    statusText.Text = "License is invalid";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69));
                    break;

                case LicenseStatus.Pending:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    statusText.Text = "License pending activation";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                    break;

                case LicenseStatus.ConnectionError:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(255, 149, 0));
                    statusText.Text = "Could not reach license server";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 149, 0));
                    break;

                default:
                    indicator.Background = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    statusText.Text = "No license configured";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                    break;
            }

            // Show detail message if available
            if (statusDetail != null)
            {
                var detail = licenseManager.CurrentMessage;
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    statusDetail.Text = detail;
                    statusDetail.IsVisible = true;
                }
                else
                {
                    statusDetail.IsVisible = false;
                }
            }

            // Show features if valid
            if (featuresPanel != null && featuresList != null)
            {
                var result = licenseManager.LastResult;
                if (result is { Valid: true, Features: not null })
                {
                    featuresList.Children.Clear();
                    foreach (var prop in result.Features.Properties())
                    {
                        var enabled = prop.Value.ToString() == "1";
                        var featureRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
                        featureRow.Children.Add(new TextBlock
                        {
                            Text = enabled ? "\u2705" : "\u274C",
                            Foreground = enabled
                                ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
                                : new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            FontSize = 14,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        });
                        featureRow.Children.Add(new TextBlock
                        {
                            Text = FormatFeatureName(prop.Name),
                            Foreground = Brushes.White,
                            FontSize = 13,
                            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                        });
                        featuresList.Children.Add(featureRow);
                    }
                    featuresPanel.IsVisible = true;
                }
                else
                {
                    featuresPanel.IsVisible = false;
                }
            }
        }

        private async void ValidateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (licenseManager == null) return;

            var input = this.FindControl<TextBox>("LicenseKeyInput");
            var key = input?.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
            {
                var statusText = this.FindControl<TextBlock>("StatusText");
                if (statusText != null)
                {
                    statusText.Text = "Please enter a license key.";
                    statusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
                }
                return;
            }

            var validateBtn = this.FindControl<Button>("ValidateButton");
            if (validateBtn != null) validateBtn.IsEnabled = false;

            try
            {
                await licenseManager.SaveAndValidateAsync(key);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseSettingsView] Validation error: {ex.Message}");
            }
            finally
            {
                if (validateBtn != null) validateBtn.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object? sender, RoutedEventArgs e)
        {
            if (licenseManager == null) return;

            licenseManager.ClearLicense();

            var input = this.FindControl<TextBox>("LicenseKeyInput");
            if (input != null) input.Text = string.Empty;
        }

        private void RequestLicenseButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(ExperienceCheckUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LicenseSettingsView] Failed to open URL: {ex.Message}");
            }
        }

        private static string FormatFeatureName(string key)
        {
            // "expert_statistics" -> "Expert Statistics"
            var parts = key.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }
    }
}
