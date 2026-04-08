using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Linq;
using darts_hub.control;
using darts_hub.UI;



namespace darts_hub
{
    public partial class App : Application
    {
        
        private void App_Startup(object sender, object e)
        {
            var current_process = Process.GetCurrentProcess();
            var other_process = Process.GetProcessesByName(current_process.ProcessName).FirstOrDefault(p => p.Id != current_process.Id);

            if (other_process != null && other_process.MainWindowHandle != IntPtr.Zero)
            {
                if (IsIconic(other_process.MainWindowHandle))
                {
                    ShowWindow(other_process.MainWindowHandle, SW_RESTORE);
                }
                SetForegroundWindow(other_process.MainWindowHandle);
                //Shutdown();
            }
        }
        
        

        [DllImport("user32")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32")]
        static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
        const int SW_RESTORE = 9;

        [DllImport("user32")]
        static extern bool SetForegroundWindow(IntPtr hWnd);






        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var (countdownSeconds, windowX, windowY) = LoadSplashConfig();

                if (countdownSeconds > 0)
                {
                    var splash = new SplashWindow(countdownSeconds, windowX, windowY);
                    desktop.MainWindow = splash;

                    splash.Closed += (_, _) =>
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                    };
                }
                else
                {
                    desktop.MainWindow = new MainWindow();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static (int countdownSeconds, double windowX, double windowY) LoadSplashConfig()
        {
            int countdown = 1;
            double windowX = double.NaN;
            double windowY = double.NaN;

            try
            {
                var configPath = Path.Combine(Helper.GetAppBasePath(), "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var parsed = Newtonsoft.Json.Linq.JObject.Parse(json);

                    if (parsed.TryGetValue(nameof(AppConfiguration.SplashCountdownSeconds), StringComparison.OrdinalIgnoreCase, out var countdownToken))
                        countdown = (int)countdownToken;

                    if (parsed.TryGetValue(nameof(AppConfiguration.WindowX), StringComparison.OrdinalIgnoreCase, out var xToken))
                        windowX = (double)xToken;

                    if (parsed.TryGetValue(nameof(AppConfiguration.WindowY), StringComparison.OrdinalIgnoreCase, out var yToken))
                        windowY = (double)yToken;
                }
            }
            catch
            {
                // Fall through to defaults
            }

            return (countdown, windowX, windowY);
        }


    }
}
