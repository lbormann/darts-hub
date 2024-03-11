using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Linq;



namespace autodarts_desktop
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
        
        
        /*
        private const string UniqueEventName = "{AUTODARTS-DESKTOP-STATE-RUNNING}";

        private void App_Startup(object sender, object e)
        {
            bool isNewInstance = false;

            using (var mutex = new System.Threading.Mutex(true, UniqueEventName, out isNewInstance))
            {
                if (!isNewInstance)
                {
                    //Shutdown();
                    Environment.Exit(0);
                    return;
                }

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
                    Environment.Exit(0);
                }
            }
        }
        */
        


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
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }


    }
}
