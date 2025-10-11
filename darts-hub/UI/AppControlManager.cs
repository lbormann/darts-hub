using darts_hub.model;
using MsBox.Avalonia.Enums;
using System;
using System.Threading.Tasks;

namespace darts_hub.UI
{
    /// <summary>
    /// Manages app control actions like start, stop, restart
    /// </summary>
    public class AppControlManager
    {
        private readonly MainWindow mainWindow;

        public AppControlManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public async Task HandleStartStopApp(AppBase app)
        {
            try
            {
                if (app.AppRunningState)
                {
                    app.Close();
                    await Task.Delay(500); // Give it time to close
                }
                else
                {
                    app.Run();
                    await Task.Delay(500); // Give it time to start
                }
                
                // Refresh the settings page and navigation
                var appSettingsRenderer = new AppSettingsRenderer(mainWindow, mainWindow.GetConfigurator());
                await appSettingsRenderer.RenderAppSettings(app);
                
                // Save changes
                mainWindow.Save();
            }
            catch (Exception ex)
            {
                await mainWindow.RenderMessageBox("Error", 
                    $"Failed to {(app.AppRunningState ? "stop" : "start")} {app.CustomName}:\n{ex.Message}", 
                    Icon.Error, ButtonEnum.Ok, null, null, 0);
            }
        }

        public async Task HandleRestartApp(AppBase app)
        {
            if (!app.AppRunningState) return;
            
            try
            {
                mainWindow.SetWait(true, $"Restarting {app.CustomName}...");
                
                if (app.AppRunningState)
                {
                    app.Close();
                    await Task.Delay(2000);
                }
                
                app.Run();
                
                int attempts = 0;
                bool startedSuccessfully = false;
                while (attempts < 10)
                {
                    await Task.Delay(500);
                    attempts++;
                    if (app.AppRunningState)
                    {
                        startedSuccessfully = true;
                        break;
                    }
                }
                
                mainWindow.SetWait(false, "");
                
                if (!startedSuccessfully)
                {
                    await mainWindow.RenderMessageBox("Warning", 
                        $"Restart initiated for {app.CustomName}, but the app may not have started properly. Please check the console for details.", 
                        Icon.Warning, ButtonEnum.Ok, null, null, 0);
                }
                
                var appSettingsRenderer = new AppSettingsRenderer(mainWindow, mainWindow.GetConfigurator());
                await appSettingsRenderer.RenderAppSettings(app);
                mainWindow.Save();
            }
            catch (Exception ex)
            {
                mainWindow.SetWait(false, "");
                await mainWindow.RenderMessageBox("Error", 
                    $"Failed to restart {app.CustomName}:\n{ex.Message}", 
                    Icon.Error, ButtonEnum.Ok, null, null, 0);
                
                var appSettingsRenderer = new AppSettingsRenderer(mainWindow, mainWindow.GetConfigurator());
                await appSettingsRenderer.RenderAppSettings(app);
            }
        }
    }
}