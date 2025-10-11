using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using darts_hub.control;
using System;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    public partial class UpdaterTestWindow : Window
    {
        private TextBox logTextBox;
        private Button fullTestButton;
        private Button versionTestButton;
        private Button retryTestButton;
        private Button clearLogButton;
        private ProgressBar testProgressBar;

        public UpdaterTestWindow()
        {
            InitializeComponent();
            InitializeEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            logTextBox = this.FindControl<TextBox>("LogTextBox");
            fullTestButton = this.FindControl<Button>("FullTestButton");
            versionTestButton = this.FindControl<Button>("VersionTestButton");
            retryTestButton = this.FindControl<Button>("RetryTestButton");
            clearLogButton = this.FindControl<Button>("ClearLogButton");
            testProgressBar = this.FindControl<ProgressBar>("TestProgressBar");

            // Set initial state
            if (logTextBox != null)
            {
                logTextBox.Text = "=== UPDATER TEST INTERFACE ===\n" +
                                 "This test interface runs in isolated mode.\n" +
                                 "No real updates will be performed.\n" +
                                 "Click a test button to begin.\n\n";
                logTextBox.IsReadOnly = true;
            }
        }

        private void InitializeEventHandlers()
        {
            // Subscribe to test events
            UpdaterTester.TestStatusChanged += OnTestStatusChanged;
            UpdaterTester.TestCompleted += OnTestCompleted;

            // Button click handlers
            if (fullTestButton != null)
                fullTestButton.Click += async (s, e) => await RunFullTest();
            
            if (versionTestButton != null)
                versionTestButton.Click += async (s, e) => await RunVersionTest();
            
            if (retryTestButton != null)
                retryTestButton.Click += async (s, e) => await RunRetryTest();
            
            if (clearLogButton != null)
                clearLogButton.Click += ClearLog;
        }

        private async Task RunFullTest()
        {
            SetButtonsEnabled(false);
            SetProgress(true);
            AppendLog("🔍 Starting full update test (isolated mode)...\n");
            AppendLog("⚠️ Note: No real updates will be performed!\n\n");
            
            try
            {
                await UpdaterTester.RunFullUpdateTest();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Test error: {ex.Message}\n");
                UpdaterLogger.LogError("Full test execution failed in test window", ex);
            }
            finally
            {
                SetButtonsEnabled(true);
                SetProgress(false);
            }
        }

        private async Task RunVersionTest()
        {
            SetButtonsEnabled(false);
            SetProgress(true);
            AppendLog("📋 Starting version check test (isolated mode)...\n");
            AppendLog("⚠️ Note: No real updates will be performed!\n\n");
            
            try
            {
                await UpdaterTester.TestVersionCheckOnly();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Test error: {ex.Message}\n");
                UpdaterLogger.LogError("Version test execution failed in test window", ex);
            }
            finally
            {
                SetButtonsEnabled(true);
                SetProgress(false);
            }
        }

        private async Task RunRetryTest()
        {
            SetButtonsEnabled(false);
            SetProgress(true);
            AppendLog("🔄 Starting retry mechanism test...\n");
            AppendLog("⚠️ Note: Only simulates network failures for testing purposes!\n\n");
            
            try
            {
                await UpdaterTester.TestRetryMechanismOnly();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Test error: {ex.Message}\n");
                UpdaterLogger.LogError("Retry test execution failed in test window", ex);
            }
            finally
            {
                SetButtonsEnabled(true);
                SetProgress(false);
            }
        }

        private void ClearLog(object sender, RoutedEventArgs e)
        {
            if (logTextBox != null)
            {
                logTextBox.Text = "=== UPDATER TEST INTERFACE ===\n" +
                                 "Test log cleared. Ready for new tests.\n\n";
            }
        }

        private void OnTestStatusChanged(object sender, string status)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] {status}\n");
            });
        }

        private void OnTestCompleted(object sender, string results)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                AppendLog("\n=== 📊 TEST RESULTS ===\n");
                AppendLog(results);
                AppendLog("\n=== ✅ TEST COMPLETED ===\n");
                AppendLog("💡 Tip: Check the log file for detailed information.\n\n");
            });
        }

        private void AppendLog(string text)
        {
            if (logTextBox != null)
            {
                logTextBox.Text += text;
                
                // Auto-scroll to bottom
                if (logTextBox.Text.Length > 0)
                {
                    logTextBox.CaretIndex = logTextBox.Text.Length;
                }
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (fullTestButton != null) fullTestButton.IsEnabled = enabled;
            if (versionTestButton != null) versionTestButton.IsEnabled = enabled;
            if (retryTestButton != null) retryTestButton.IsEnabled = enabled;
        }

        private void SetProgress(bool isRunning)
        {
            if (testProgressBar != null)
            {
                testProgressBar.IsIndeterminate = isRunning;
                testProgressBar.IsVisible = isRunning;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            UpdaterTester.TestStatusChanged -= OnTestStatusChanged;
            UpdaterTester.TestCompleted -= OnTestCompleted;
            
            base.OnClosed(e);
        }
    }
}